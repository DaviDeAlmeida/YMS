using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;
using YMS.Domain.Categories;

namespace YMS.Application.Categories.Commands.UpsertCategory;

public sealed class UpsertCategoryHandler(IYmsDbContext db, IOwnerContext owner, TimeProvider clock)
    : IRequestHandler<UpsertCategoryCommand, UpsertResult<CategoryResponse>>
{
    public async Task<UpsertResult<CategoryResponse>> Handle(UpsertCategoryCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var category = await db.Categories.SingleOrDefaultAsync(c => c.ExternalId == command.ExternalId, cancellationToken);

        UpsertOutcome outcome;
        if (category is null)
        {
            category = Category.Create(owner.OwnerId, command.ExternalId!, command.Description!, now);
            db.Categories.Add(category);
            outcome = UpsertOutcome.Created;
        }
        else
        {
            category.Update(command.Description!, now);
            outcome = UpsertOutcome.Updated;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new UpsertResult<CategoryResponse>(CategoryResponse.From(category), outcome);
    }
}
