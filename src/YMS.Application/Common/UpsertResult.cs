namespace YMS.Application.Common;

public enum UpsertOutcome
{
    Created,
    Updated,
}

public interface IUpsertResult
{
    UpsertOutcome Outcome { get; }
}

/// <summary>Result of creating or updating a record by its <c>externalId</c>.</summary>
public sealed record UpsertResult<T>(T Resource, UpsertOutcome Outcome) : IUpsertResult;
