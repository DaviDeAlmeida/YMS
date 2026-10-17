using YMS.Domain.Common;

namespace YMS.Domain.Batches;

public enum BatchType
{
    Categories,
    Products,
    Suppliers,
}

public enum BatchStatus
{
    Received,
    Processing,
    Completed,
}

public enum BatchItemResult
{
    Pending,
    Created,
    Updated,
    Rejected,
}

/// <summary>Several records of one type sent in one request, processed asynchronously with a result per item.</summary>
public sealed class Batch
{
    public const int MaxItems = 500;

    private readonly List<BatchItem> _items = [];

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid OwnerId { get; private set; }
    public BatchType Type { get; private set; }
    public BatchStatus Status { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public IReadOnlyList<BatchItem> Items => _items;

    private Batch() { }

    /// <param name="items">Each item's original payload (JSON) and, when it has one, its <c>externalId</c>, in the order sent.</param>
    public static Batch Receive(Guid ownerId, BatchType type, IReadOnlyList<(string? ExternalId, string Payload)> items, DateTimeOffset now)
    {
        if (items.Count == 0)
            throw new DomainException("items", "batch.items_required", "O Lote precisa de pelo menos um item.");
        if (items.Count > MaxItems)
            throw new DomainException("items", "batch.too_many_items", $"O Lote pode ter no máximo {MaxItems} itens.");

        var batch = new Batch { OwnerId = ownerId, Type = type, Status = BatchStatus.Received, ReceivedAt = now };
        batch._items.AddRange(items.Select((item, position) => new BatchItem(batch.Id, position, item.ExternalId, item.Payload)));
        return batch;
    }

    /// <summary>Starts (or resumes, after an interruption) the processing.</summary>
    public void StartProcessing()
    {
        if (Status == BatchStatus.Received)
            Status = BatchStatus.Processing;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status == BatchStatus.Completed)
            return;
        Status = BatchStatus.Completed;
        CompletedAt = now;
    }
}

public sealed class BatchItem
{
    private readonly List<BatchItemError> _errors = [];

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid BatchId { get; private set; }

    /// <summary>Zero-based index of the item in the <c>items</c> array that was sent.</summary>
    public int Position { get; private set; }

    public string? ExternalId { get; private set; }

    /// <summary>The item exactly as received, so processing can be resumed after an interruption.</summary>
    public string Payload { get; private set; } = null!;

    public BatchItemResult Result { get; private set; } = BatchItemResult.Pending;
    public IReadOnlyList<BatchItemError> Errors => _errors;

    private BatchItem() { }

    internal BatchItem(Guid batchId, int position, string? externalId, string payload)
    {
        BatchId = batchId;
        Position = position;
        // Only a reference for the ERP: an oversized externalId is cut here and rejected by the item's own validation.
        ExternalId = externalId is { Length: > MaxExternalIdLength } ? externalId[..MaxExternalIdLength] : externalId;
        Payload = payload;
    }

    public const int MaxExternalIdLength = 50;

    public bool IsPending => Result == BatchItemResult.Pending;

    public void Succeed(bool created)
    {
        EnsurePending();
        Result = created ? BatchItemResult.Created : BatchItemResult.Updated;
    }

    public void Reject(IEnumerable<BatchItemError> errors)
    {
        EnsurePending();
        Result = BatchItemResult.Rejected;
        _errors.AddRange(errors);
    }

    private void EnsurePending()
    {
        if (!IsPending)
            throw new InvalidOperationException($"Item {Position} of Batch {BatchId} already has a result.");
    }
}

public sealed record BatchItemError(string Field, string Code, string Message);
