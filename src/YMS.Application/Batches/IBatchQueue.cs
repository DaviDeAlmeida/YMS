namespace YMS.Application.Batches;

/// <summary>Hands a stored Batch to the background consumer. The message carries only the Batch id.</summary>
public interface IBatchQueue
{
    ValueTask PublishAsync(Guid batchId, CancellationToken cancellationToken);
}
