using System.Text.Json;
using FluentValidation;
using MediatR;

namespace YMS.Application.Common;

/// <summary>A rejected field: the camelCase path in the contract (e.g. <c>eans[1].value</c>), a stable code and a message in Portuguese.</summary>
public sealed record FieldError(string Field, string Code, string Message);

/// <summary>The request was rejected. The API answers it with 422 Problem Details; a Batch item records the errors.</summary>
public sealed class RequestRejectedException(IReadOnlyList<FieldError> errors) : Exception("The request was rejected.")
{
    public IReadOnlyList<FieldError> Errors { get; } = errors;
}

/// <summary>Runs every FluentValidation validator (sync and async) before the handler.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var errors = new List<FieldError>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            errors.AddRange(result.Errors.Select(e => new FieldError(e.PropertyName, e.ErrorCode, e.ErrorMessage)));
        }

        if (errors.Count > 0)
            throw new RequestRejectedException(errors);

        return await next(cancellationToken);
    }
}

public static class ContractPropertyNames
{
    /// <summary>Makes validators name properties as the contract does (<c>address.zipCode</c>, <c>eans[1].value</c>), in paths and messages.</summary>
    public static void Apply() =>
        ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
            member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);
}
