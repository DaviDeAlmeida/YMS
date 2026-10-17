using YMS.Domain.Common;

namespace YMS.Application.Common;

public sealed record ErrorEntry(string Code, string Message);

public static class FieldErrors
{
    /// <summary>The <c>errors</c> member shared by Problem Details and Batch items: field → [{ code, message }].</summary>
    public static Dictionary<string, ErrorEntry[]> GroupByField(IEnumerable<FieldError> errors) =>
        errors.GroupBy(e => e.Field)
            .ToDictionary(g => g.Key, g => g.Select(e => new ErrorEntry(e.Code, e.Message)).ToArray());
}

public static class DomainExceptionExtensions
{
    public static FieldError ToFieldError(this DomainException exception) => new(exception.Field, exception.Code, exception.Message);
}
