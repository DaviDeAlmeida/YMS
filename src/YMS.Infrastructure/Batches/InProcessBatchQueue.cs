using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YMS.Application.Batches;
using YMS.Application.Batches.Processing;
using YMS.Application.Common;
using YMS.Domain.Batches;

namespace YMS.Infrastructure.Batches;

/// <summary>
/// Temporary in-process transport for Batch messages. RabbitMQ replaces it once Docker is available; the contract
/// (publish the Batch id after it is stored, acknowledge after processing) stays the same.
/// </summary>
public sealed class InProcessBatchQueue : IBatchQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask PublishAsync(Guid batchId, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(batchId, cancellationToken);
}

/// <summary>Consumes Batch messages one at a time, so items run in order and a duplicate message only finds nothing pending.</summary>
public sealed class BatchConsumer(InProcessBatchQueue queue, BatchProcessor processor, ILogger<BatchConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batchId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await processor.ProcessAsync(batchId, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Not acknowledged: the Batch is left unfinished and the republisher sends it again.
                logger.LogError(exception, "Batch {BatchId} could not be processed.", batchId);
            }
        }
    }
}

/// <summary>
/// Stands in for the broker's redelivery of unacknowledged messages: on start-up it republishes every unfinished Batch
/// (resuming one interrupted mid-processing), and then, periodically, every Batch still unfinished
/// <c>Batches:RepublishAfter</c> after it was received (never published, or dropped by a failure). Duplicates are harmless:
/// the single consumer runs them one after another and finds nothing pending.
/// </summary>
public sealed class BatchRepublisher(
    IServiceScopeFactory scopes, IBatchQueue queue, TimeProvider clock, IConfiguration configuration, ILogger<BatchRepublisher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue("Batches:RepublishInterval", TimeSpan.FromSeconds(30));
        var after = configuration.GetValue("Batches:RepublishAfter", TimeSpan.FromMinutes(1));

        await RepublishAsync(b => b.Status != BatchStatus.Completed, stoppingToken);

        using var timer = new PeriodicTimer(interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var cutoff = clock.GetUtcNow() - after;
            await RepublishAsync(b => b.Status != BatchStatus.Completed && b.ReceivedAt < cutoff, stoppingToken);
        }
    }

    private async Task RepublishAsync(System.Linq.Expressions.Expression<Func<Batch, bool>> filter, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IYmsDbContext>();
            var ids = await db.Batches.IgnoreQueryFilters().Where(filter).OrderBy(b => b.ReceivedAt).Select(b => b.Id).ToListAsync(cancellationToken);
            foreach (var id in ids)
                await queue.PublishAsync(id, cancellationToken);
            if (ids.Count > 0)
                logger.LogInformation("Republished {Count} unfinished Batches.", ids.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not republish unfinished Batches.");
        }
    }
}
