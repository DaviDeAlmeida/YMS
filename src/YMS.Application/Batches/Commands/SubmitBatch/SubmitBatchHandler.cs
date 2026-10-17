using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using YMS.Application.Common;
using YMS.Domain.Batches;

namespace YMS.Application.Batches.Commands.SubmitBatch;

public sealed class SubmitBatchHandler(
    IYmsDbContext db, IOwnerContext owner, IBatchQueue queue, TimeProvider clock, ILogger<SubmitBatchHandler> logger)
    : IRequestHandler<SubmitBatchCommand, Guid>
{
    public async Task<Guid> Handle(SubmitBatchCommand command, CancellationToken cancellationToken)
    {
        var items = command.Items!.Select(item => (ExternalIdOf(item), item.GetRawText())).ToList();
        var batch = Batch.Receive(owner.OwnerId, command.Type, items, clock.GetUtcNow());
        db.Batches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        // The Batch is already stored: if publishing fails, the republisher picks it up while it stays Received.
        try
        {
            await queue.PublishAsync(batch.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Batch {BatchId} was stored but could not be published; it will be republished.", batch.Id);
        }

        return batch.Id;
    }

    // Only a reference for the ERP: an oversized externalId is cut here and rejected by the item's own validation.
    private static string? ExternalIdOf(JsonElement item) =>
        item.ValueKind == JsonValueKind.Object
        && item.TryGetProperty("externalId", out var externalId)
        && externalId.ValueKind == JsonValueKind.String
        && externalId.GetString() is { } value
            ? value.Length > Limits.ExternalId ? value[..Limits.ExternalId] : value
            : null;
}
