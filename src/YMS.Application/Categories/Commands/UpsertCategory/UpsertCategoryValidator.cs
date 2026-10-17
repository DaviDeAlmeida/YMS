using FluentValidation;
using YMS.Application.Common;

namespace YMS.Application.Categories.Commands.UpsertCategory;

public sealed class UpsertCategoryValidator : AbstractValidator<UpsertCategoryCommand>
{
    public UpsertCategoryValidator()
    {
        RuleFor(c => c.ExternalId).RequiredText("category.external_id", Limits.ExternalId);
        RuleFor(c => c.Description).RequiredText("category.description", Limits.Description);
    }
}
