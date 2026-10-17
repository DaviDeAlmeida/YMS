using MediatR;
using YMS.Application.Common;

namespace YMS.Application.Products.Commands.UpsertProduct;

public sealed record UpsertProductCommand(
    string? ExternalId,
    string? Description,
    string? CategoryExternalId,
    IReadOnlyList<EanInput?>? Eans)
    : IRequest<UpsertResult<ProductResponse>>;

public sealed record EanInput(string? Value, bool? IsPrimary);
