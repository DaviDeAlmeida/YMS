using FluentValidation;

namespace YMS.Application.Common;

/// <summary>Shared rules that attach the stable error code and the Portuguese message used across the contract.</summary>
public static class ValidationRules
{
    public static IRuleBuilderOptions<T, string?> Required<T>(this IRuleBuilder<T, string?> rule, string code) =>
        rule.Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(code)
            .WithMessage("O campo '{PropertyPath}' é obrigatório.");

    public static IRuleBuilderOptions<T, string?> NoLongerThan<T>(this IRuleBuilder<T, string?> rule, int maxLength, string code) =>
        rule.Must(value => value is null || value.Length <= maxLength)
            .WithErrorCode(code)
            .WithMessage($"O campo '{{PropertyPath}}' deve ter no máximo {maxLength} caracteres.");

    /// <summary>Required and at most <paramref name="maxLength"/> characters, with codes <c>{prefix}_required</c> and <c>{prefix}_too_long</c>.</summary>
    public static void RequiredText<T>(this IRuleBuilderInitial<T, string?> rule, string codePrefix, int maxLength) =>
        rule.Cascade(CascadeMode.Stop)
            .Required($"{codePrefix}_required")
            .NoLongerThan(maxLength, $"{codePrefix}_too_long");

    /// <summary>Optional, at most <paramref name="maxLength"/> characters, with code <c>{prefix}_too_long</c>.</summary>
    public static void OptionalText<T>(this IRuleBuilderInitial<T, string?> rule, string codePrefix, int maxLength) =>
        rule.NoLongerThan(maxLength, $"{codePrefix}_too_long");
}
