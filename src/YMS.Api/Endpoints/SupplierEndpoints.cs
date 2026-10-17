using MediatR;
using YMS.Application.Suppliers;
using YMS.Application.Suppliers.Commands.UpsertSupplier;
using YMS.Application.Suppliers.Queries.GetSupplier;

namespace YMS.Api.Endpoints;

public static class SupplierEndpoints
{
    public static RouteGroupBuilder MapSupplierEndpoints(this RouteGroupBuilder api)
    {
        var suppliers = api.MapGroup("/suppliers").WithTags("Suppliers");

        suppliers.MapPost("", async (UpsertSupplierCommand command, ISender sender, CancellationToken ct) =>
                Upsert.ToResult(await sender.Send(command, ct), c => Routes.Supplier(c.ExternalId)))
            .Produces<SupplierResponse>(StatusCodes.Status201Created)
            .Produces<SupplierResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        suppliers.MapGet("/{externalId}", async (string externalId, ISender sender, CancellationToken ct) =>
                await sender.Send(new GetSupplierQuery(externalId), ct) is { } supplier
                    ? Results.Ok(supplier)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound))
            .Produces<SupplierResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
