using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;

namespace YMS.Application.Suppliers.Queries.GetSupplier;

public sealed record GetSupplierQuery(string ExternalId) : IRequest<SupplierResponse?>;

public sealed class GetSupplierHandler(IYmsDbContext db) : IRequestHandler<GetSupplierQuery, SupplierResponse?>
{
    public async Task<SupplierResponse?> Handle(GetSupplierQuery query, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(s => s.ExternalId == query.ExternalId, cancellationToken);
        return supplier is null ? null : SupplierResponse.From(supplier);
    }
}
