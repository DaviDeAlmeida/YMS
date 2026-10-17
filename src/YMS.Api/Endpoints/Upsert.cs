using YMS.Application.Common;

namespace YMS.Api.Endpoints;

public static class Upsert
{
    /// <summary>201 with <c>Location</c> when the record was created, 200 when an existing one was updated.</summary>
    public static IResult ToResult<T>(UpsertResult<T> result, Func<T, string> location) =>
        result.Outcome == UpsertOutcome.Created
            ? Results.Created(location(result.Resource), result.Resource)
            : Results.Ok(result.Resource);
}
