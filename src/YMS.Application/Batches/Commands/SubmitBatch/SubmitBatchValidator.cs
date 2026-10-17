using FluentValidation;
using YMS.Domain.Batches;

namespace YMS.Application.Batches.Commands.SubmitBatch;

public sealed class SubmitBatchValidator : AbstractValidator<SubmitBatchCommand>
{
    public SubmitBatchValidator()
    {
        RuleFor(b => b.Items)
            .Must(items => items is { Count: > 0 })
            .WithErrorCode("batch.items_required")
            .WithMessage("O Lote precisa de pelo menos um item.");

        RuleFor(b => b.Items)
            .Must(items => items is null || items.Count <= Batch.MaxItems)
            .WithErrorCode("batch.too_many_items")
            .WithMessage($"O Lote pode ter no máximo {Batch.MaxItems} itens.");
    }
}
