namespace YMS.Domain.Common;

/// <summary>An aggregate invariant was broken. The API answers it with 422, in the same format as validation errors.</summary>
public sealed class DomainException(string field, string code, string message) : Exception(message)
{
    public string Field { get; } = field;
    public string Code { get; } = code;
}
