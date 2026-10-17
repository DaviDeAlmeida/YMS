using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Common;
using YMS.Domain.Common;

namespace YMS.Api.Errors;

/// <summary>Turns validation and domain rejections into a 422 Problem Details (RFC 9457) with errors per field.</summary>
public sealed class RejectionExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IReadOnlyList<FieldError>? errors = exception switch
        {
            RequestRejectedException rejected => rejected.Errors,
            DomainException domain => [domain.ToFieldError()],
            _ => null,
        };
        if (errors is null)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = Problems.Rejected(errors),
        });
    }
}

public static class Problems
{
    public static ProblemDetails Rejected(IReadOnlyList<FieldError> errors) => new()
    {
        Status = StatusCodes.Status422UnprocessableEntity,
        Title = "A requisição foi rejeitada.",
        Detail = "Um ou mais campos são inválidos.",
        Extensions = { ["errors"] = FieldErrors.GroupByField(errors) },
    };

    public static ProblemDetails BadRequest(IReadOnlyList<FieldError> errors) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Status = StatusCodes.Status400BadRequest,
        Title = "A requisição não pôde ser lida.",
        Detail = "O corpo da requisição está ausente ou malformado.",
        Extensions = { ["errors"] = FieldErrors.GroupByField(errors) },
    };
}
