using FluentValidation;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;

namespace YMS.Application.Products.Commands.UpsertProduct;

/// <summary>Input format: required fields, sizes and the primary EAN rule.</summary>
public sealed class UpsertProductValidator : AbstractValidator<UpsertProductCommand>
{
    public UpsertProductValidator()
    {
        RuleFor(p => p.ExternalId).RequiredText("product.external_id", Limits.ExternalId);
        RuleFor(p => p.Description).RequiredText("product.description", Limits.Description);
        RuleFor(p => p.CategoryExternalId).RequiredText("product.category_external_id", Limits.ExternalId);

        RuleFor(p => p.Eans)
            .Must(eans => eans is { Count: > 0 })
            .WithErrorCode("product.ean_required")
            .WithMessage("O Produto precisa de pelo menos um EAN.");

        RuleForEach(p => p.Eans)
            .NotNull().WithErrorCode("product.ean_required").WithMessage("O EAN não pode ser nulo.")
            .ChildRules(ean => ean.RuleFor(e => e!.Value).Required("product.ean_value_required"));

        // A lone EAN is the primary one, so the count of primaries only matters with two or more.
        When(p => p.Eans is { Count: > 1 }, () =>
        {
            RuleFor(p => p.Eans)
                .Must(eans => eans!.Any(e => e?.IsPrimary == true))
                .WithErrorCode("product.primary_ean_required")
                .WithMessage("O Produto precisa de um EAN principal (isPrimary = true).");
            RuleFor(p => p.Eans)
                .Must(eans => eans!.Count(e => e?.IsPrimary == true) <= 1)
                .WithErrorCode("product.single_primary_ean")
                .WithMessage("O Produto deve ter apenas um EAN principal (isPrimary = true).");
        });
    }
}

/// <summary>References that need the database: the Category must exist in the current Owner.</summary>
public sealed class UpsertProductReferencesValidator : AbstractValidator<UpsertProductCommand>
{
    public UpsertProductReferencesValidator(IYmsDbContext db)
    {
        When(p => !string.IsNullOrWhiteSpace(p.CategoryExternalId), () =>
            RuleFor(p => p.CategoryExternalId)
                .MustAsync((externalId, ct) => db.Categories.AnyAsync(c => c.ExternalId == externalId, ct))
                .WithErrorCode("category.not_found")
                .WithMessage("A Categoria '{PropertyValue}' não existe. Envie a Categoria antes do Produto."));
    }
}
