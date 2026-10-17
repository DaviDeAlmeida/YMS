using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;

namespace YMS.Application.Categories.Queries.GetCategory;

public sealed record GetCategoryQuery(string ExternalId) : IRequest<CategoryResponse?>;

public sealed class GetCategoryHandler(IYmsDbContext db) : IRequestHandler<GetCategoryQuery, CategoryResponse?>
{
    public async Task<CategoryResponse?> Handle(GetCategoryQuery query, CancellationToken cancellationToken)
    {
        var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.ExternalId == query.ExternalId, cancellationToken);
        return category is null ? null : CategoryResponse.From(category);
    }
}
