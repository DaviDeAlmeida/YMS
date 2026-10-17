using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;
using YMS.Domain.Products;

namespace YMS.Application.Products.Commands.UpsertProduct;

public sealed class UpsertProductHandler(IYmsDbContext db, IOwnerContext owner, TimeProvider clock)
    : IRequestHandler<UpsertProductCommand, UpsertResult<ProductResponse>>
{
    public async Task<UpsertResult<ProductResponse>> Handle(UpsertProductCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var category = await db.Categories.SingleAsync(c => c.ExternalId == command.CategoryExternalId, cancellationToken);
        var eans = command.Eans!.Select(e => new Ean(e!.Value!, e.IsPrimary ?? false)).ToList();
        var product = await db.Products.SingleOrDefaultAsync(p => p.ExternalId == command.ExternalId, cancellationToken);

        UpsertOutcome outcome;
        if (product is null)
        {
            product = Product.Create(owner.OwnerId, command.ExternalId!, command.Description!, category, eans, now);
            db.Products.Add(product);
            outcome = UpsertOutcome.Created;
        }
        else
        {
            product.Update(command.Description!, category, eans, now);
            outcome = UpsertOutcome.Updated;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new UpsertResult<ProductResponse>(ProductResponse.From(product), outcome);
    }
}
