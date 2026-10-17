using MediatR;
using YMS.Application.Products;
using YMS.Application.Products.Commands.UpsertProduct;
using YMS.Application.Products.Queries.GetProduct;

namespace YMS.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder api)
    {
        var products = api.MapGroup("/products").WithTags("Products");

        products.MapPost("", async (UpsertProductCommand command, ISender sender, CancellationToken ct) =>
                Upsert.ToResult(await sender.Send(command, ct), c => Routes.Product(c.ExternalId)))
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .Produces<ProductResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        products.MapGet("/{externalId}", async (string externalId, ISender sender, CancellationToken ct) =>
                await sender.Send(new GetProductQuery(externalId), ct) is { } product
                    ? Results.Ok(product)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound))
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
