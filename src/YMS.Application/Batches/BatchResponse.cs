using YMS.Application.Common;
using YMS.Domain.Batches;

namespace YMS.Application.Batches;

public sealed record BatchResponse(
    Guid Id,
    BatchType Type,
    BatchStatus Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<BatchItemResponse> Items);

public sealed record BatchItemResponse(
    int Position,
    string? ExternalId,
    BatchItemResult Result,
    IReadOnlyDictionary<string, ErrorEntry[]> Errors);
