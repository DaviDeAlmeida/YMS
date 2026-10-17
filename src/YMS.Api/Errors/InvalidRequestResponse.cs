using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using YMS.Application.Common;

namespace YMS.Api.Errors;

/// <summary>
/// Turns the body binding failures of <c>[ApiController]</c> into a 400 Problem Details with the same <c>errors</c>
/// format as the 422: field → [{ code, message }], in Portuguese, instead of MVC's English string list.
/// </summary>
public static class InvalidRequestResponse
{
    private const string Body = "body";

    public static IActionResult Create(ActionContext context)
    {
        var problem = Problems.BadRequest(ToFieldErrors(context.ModelState));
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }

    // System.Text.Json reports keys as JSON paths: "$" when the body is not JSON at all, "$.eans[0].isPrimary" when a
    // value cannot be read as its type. Any other key is the action parameter left unbound, i.e. a missing body;
    // MVC adds it alongside the JSON errors too, where it would only repeat them.
    private static List<FieldError> ToFieldErrors(ModelStateDictionary modelState)
    {
        var invalidKeys = modelState.Where(e => e.Value?.Errors.Count > 0).Select(e => e.Key).ToList();
        var jsonKeys = invalidKeys.Where(k => k.StartsWith('$')).ToList();
        if (jsonKeys.Count == 0)
            return [new FieldError(Body, "request.body_required", "O corpo da requisição é obrigatório.")];

        return jsonKeys
            .Select(key => key == "$"
                ? new FieldError(Body, "request.malformed_json", "O corpo da requisição não é um JSON válido.")
                : new FieldError(key[2..], "request.invalid_type", "O valor informado não tem o tipo esperado."))
            .ToList();
    }
}
