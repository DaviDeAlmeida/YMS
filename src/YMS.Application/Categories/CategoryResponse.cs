using YMS.Domain.Categories;

namespace YMS.Application.Categories;

public sealed record CategoryResponse(string ExternalId, string Description, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static CategoryResponse From(Category category) =>
        new(category.ExternalId, category.Description, category.CreatedAt, category.UpdatedAt);
}
