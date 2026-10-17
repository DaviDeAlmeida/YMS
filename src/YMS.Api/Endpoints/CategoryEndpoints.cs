using MediatR;
using YMS.Application.Categories;
using YMS.Application.Categories.Commands.UpsertCategory;
using YMS.Application.Categories.Queries.GetCategory;

namespace YMS.Api.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder api)
    {
        var categories = api.MapGroup("/categories").WithTags("Categories");

        categories.MapPost("", async (UpsertCategoryCommand command, ISender sender, CancellationToken ct) =>
                Upsert.ToResult(await sender.Send(command, ct), c => Routes.Category(c.ExternalId)))
            .Produces<CategoryResponse>(StatusCodes.Status201Created)
            .Produces<CategoryResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        categories.MapGet("/{externalId}", async (string externalId, ISender sender, CancellationToken ct) =>
                await sender.Send(new GetCategoryQuery(externalId), ct) is { } category
                    ? Results.Ok(category)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound))
            .Produces<CategoryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
