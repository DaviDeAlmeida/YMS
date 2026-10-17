using MediatR;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Batches.Commands.SubmitBatch;
using YMS.Application.Common;
using YMS.Domain.Batches;

namespace YMS.Api.Controllers;

/// <summary>Shared plumbing for the v1 controllers: JSON bodies, requests handed straight to MediatR.</summary>
[ApiController]
[Consumes("application/json")]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    /// <summary>201 with <c>Location</c> when the record was created, 200 when an existing one was updated.</summary>
    protected IActionResult Upserted<T>(UpsertResult<T> result, Func<T, string> location) =>
        result.Outcome == UpsertOutcome.Created
            ? Created(location(result.Resource), result.Resource)
            : Ok(result.Resource);

    protected IActionResult FoundOrNotFound<T>(T? resource) where T : class =>
        resource is null ? Problem(statusCode: StatusCodes.Status404NotFound) : Ok(resource);

    protected async Task<IActionResult> SubmitBatch(BatchType type, BatchRequest request, CancellationToken ct)
    {
        var batchId = await Sender.Send(new SubmitBatchCommand(type, request.Items), ct);
        return Accepted(Routes.Batch(batchId), new BatchAccepted(batchId));
    }
}
