using YMS.Domain.Products;

namespace YMS.Application.Products;

public sealed record ProductResponse(
    string ExternalId,
    string Description,
    string CategoryExternalId,
    IReadOnlyList<EanResponse> Eans,
    string UnitOfMeasure,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProductResponse From(Product product) => new(
        product.ExternalId,
        product.Description,
        product.Category.ExternalId,
        product.Eans.Select(e => new EanResponse(e.Value, e.IsPrimary)).ToList(),
        product.UnitOfMeasure,
        product.CreatedAt,
        product.UpdatedAt);
}

public sealed record EanResponse(string Value, bool IsPrimary);
