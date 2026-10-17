using System.Text.Json;
using MediatR;
using YMS.Domain.Batches;

namespace YMS.Application.Batches.Commands.SubmitBatch;

/// <summary>Stores a Batch with its items as received and queues it; returns the Batch id.</summary>
public sealed record SubmitBatchCommand(BatchType Type, IReadOnlyList<JsonElement>? Items) : IRequest<Guid>;
