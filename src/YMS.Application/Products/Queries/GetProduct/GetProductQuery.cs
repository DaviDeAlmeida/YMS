using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;

namespace YMS.Application.Products.Queries.GetProduct;

public sealed record GetProductQuery(string ExternalId) : IRequest<ProductResponse?>;

public sealed class GetProductHandler(IYmsDbContext db) : IRequestHandler<GetProductQuery, ProductResponse?>
{
    public async Task<ProductResponse?> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .SingleOrDefaultAsync(p => p.ExternalId == query.ExternalId, cancellationToken);
        return product is null ? null : ProductResponse.From(product);
    }
}
