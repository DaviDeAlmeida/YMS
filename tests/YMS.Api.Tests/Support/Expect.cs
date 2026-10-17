using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace YMS.Api.Tests.Support;

public static class Expect
{
    /// <summary>Asserts a 422 Problem Details response and returns its body.</summary>
    public static async Task<JsonElement> RejectedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(422, problem.GetProperty("status").GetInt32());
        return problem;
    }

    /// <summary>Asserts that <paramref name="errorsOwner"/> (a Problem Details or a Batch item) has <paramref name="code"/> on <paramref name="field"/>, with a message.</summary>
    public static JsonElement Error(JsonElement errorsOwner, string field, string code)
    {
        var errors = errorsOwner.GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out var fieldErrors), $"No errors for '{field}'. Errors: {errors}");
        var match = fieldErrors.EnumerateArray().FirstOrDefault(e => e.GetProperty("code").GetString() == code);
        Assert.True(match.ValueKind == JsonValueKind.Object, $"No '{code}' on '{field}'. Errors: {errors}");
        Assert.False(string.IsNullOrWhiteSpace(match.GetProperty("message").GetString()));
        return match;
    }
}
