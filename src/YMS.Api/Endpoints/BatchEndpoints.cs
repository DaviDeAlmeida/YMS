using System.Text.Json;
using MediatR;
using YMS.Application.Batches.Commands.SubmitBatch;
using YMS.Application.Batches.Queries.GetBatch;
using YMS.Domain.Batches;

namespace YMS.Api.Endpoints;

/// <summary><c>{ "items": [...] }</c>, each item in the same format as the unit endpoint.</summary>
public sealed record BatchRequest(IReadOnlyList<JsonElement>? Items);

public sealed record BatchAccepted(Guid BatchId);

public static class BatchEndpoints
{
    public static RouteGroupBuilder MapBatchEndpoints(this RouteGroupBuilder api)
    {
        MapSubmit(api, "/categories/batch", BatchType.Categories, "Categories");
        MapSubmit(api, "/products/batch", BatchType.Products, "Products");
        MapSubmit(api, "/suppliers/batch", BatchType.Suppliers, "Suppliers");

        api.MapGet("/batches/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                await sender.Send(new GetBatchQuery(id), ct) is { } batch
                    ? Results.Ok(batch)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound))
            .WithTags("Batches")
            .Produces<BatchResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static void MapSubmit(RouteGroupBuilder api, string pattern, BatchType type, string tag) =>
        api.MapPost(pattern, async (BatchRequest request, ISender sender, CancellationToken ct) =>
            {
                var batchId = await sender.Send(new SubmitBatchCommand(type, request.Items), ct);
                return Results.Accepted(Routes.Batch(batchId), new BatchAccepted(batchId));
            })
            .WithTags(tag)
            .Produces<BatchAccepted>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
}
