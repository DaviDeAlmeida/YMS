using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YMS.Application.Categories.Commands.UpsertCategory;
using YMS.Application.Common;
using YMS.Application.Products.Commands.UpsertProduct;
using YMS.Application.Suppliers.Commands.UpsertSupplier;
using YMS.Domain.Batches;
using YMS.Domain.Common;

namespace YMS.Application.Batches.Processing;

/// <summary>
/// Processes the pending items of a Batch in order, each in its own scope and transaction, through the same
/// command (validators and handler) as the unit endpoint. Safe to run again: items with a result are skipped.
/// </summary>
public sealed class BatchProcessor(IServiceScopeFactory scopes, TimeProvider clock, ILogger<BatchProcessor> logger)
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(Guid batchId, CancellationToken cancellationToken)
    {
        if (await StartAsync(batchId, cancellationToken) is not { } batch)
            return;

        foreach (var position in batch.PendingPositions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessItemAsync(batch, position, cancellationToken);
        }

        await InOwnerScopeAsync(batch.OwnerId, async (db, _, ct) =>
        {
            var stored = await db.Batches.SingleAsync(b => b.Id == batchId, ct);
            stored.Complete(clock.GetUtcNow());
            await db.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    private sealed record StartedBatch(Guid Id, Guid OwnerId, BatchType Type, IReadOnlyList<int> PendingPositions);

    private async Task<StartedBatch?> StartAsync(Guid batchId, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IYmsDbContext>();

        // The message carries only the id: the Owner is the one recorded on the Batch itself.
        var batch = await db.Batches.IgnoreQueryFilters()
            .Include(b => b.Items)
            .SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            logger.LogWarning("Batch {BatchId} does not exist; message ignored.", batchId);
            return null;
        }
        if (batch.Status == BatchStatus.Completed)
            return null;

        batch.StartProcessing();
        await db.SaveChangesAsync(cancellationToken);

        var pending = batch.Items.Where(i => i.IsPending).Select(i => i.Position).Order().ToList();
        return new StartedBatch(batch.Id, batch.OwnerId, batch.Type, pending);
    }

    private async Task ProcessItemAsync(StartedBatch batch, int position, CancellationToken cancellationToken)
    {
        IReadOnlyList<FieldError> errors;
        try
        {
            // Writing the record and the item's result commit together, so an interruption never splits them.
            await InOwnerScopeAsync(batch.OwnerId, (db, sender, ct) => db.ExecuteInTransactionAsync(async tct =>
            {
                var item = await PendingItemAsync(db, batch.Id, position, tct);
                if (item is null)
                    return;

                var result = (IUpsertResult)(await sender.Send(ToCommand(batch.Type, item.Payload), tct))!;
                item.Succeed(created: result.Outcome == UpsertOutcome.Created);
                await db.SaveChangesAsync(tct);
            }, ct), cancellationToken);
            return;
        }
        catch (RequestRejectedException rejected)
        {
            errors = rejected.Errors;
        }
        catch (DomainException domain)
        {
            errors = [domain.ToFieldError()];
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Item {Position} of Batch {BatchId} failed unexpectedly.", position, batch.Id);
            errors = [new FieldError("", "batch.item_failed", "Não foi possível processar o item.")];
        }

        // A fresh scope, so nothing the failed attempt left in the change tracker is saved.
        await InOwnerScopeAsync(batch.OwnerId, async (db, _, ct) =>
        {
            var item = await PendingItemAsync(db, batch.Id, position, ct);
            item?.Reject(errors.Select(e => new BatchItemError(e.Field, e.Code, e.Message)));
            await db.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    // Through the Batch, so the item is reached under the current Owner's filter.
    private static Task<BatchItem?> PendingItemAsync(IYmsDbContext db, Guid batchId, int position, CancellationToken ct) =>
        db.Batches.Where(b => b.Id == batchId)
            .SelectMany(b => b.Items)
            .SingleOrDefaultAsync(i => i.Position == position && i.Result == BatchItemResult.Pending, ct);

    private static object ToCommand(BatchType type, string payload)
    {
        object? command;
        try
        {
            command = type switch
            {
                BatchType.Categories => JsonSerializer.Deserialize<UpsertCategoryCommand>(payload, PayloadOptions),
                BatchType.Products => JsonSerializer.Deserialize<UpsertProductCommand>(payload, PayloadOptions),
                BatchType.Suppliers => JsonSerializer.Deserialize<UpsertSupplierCommand>(payload, PayloadOptions),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }
        catch (JsonException)
        {
            command = null;
        }

        return command ?? throw new RequestRejectedException(
            [new FieldError("", "batch.item_invalid", "O item não está no formato esperado para este Lote.")]);
    }

    private async Task InOwnerScopeAsync(Guid ownerId, Func<IYmsDbContext, ISender, CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        scope.ServiceProvider.GetRequiredService<OwnerContext>().Set(ownerId);
        await work(scope.ServiceProvider.GetRequiredService<IYmsDbContext>(), scope.ServiceProvider.GetRequiredService<ISender>(), cancellationToken);
    }
}
