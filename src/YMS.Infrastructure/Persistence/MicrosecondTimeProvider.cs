namespace YMS.Infrastructure.Persistence;

/// <summary>
/// System clock truncated to microseconds, the precision of PostgreSQL's <c>timestamptz</c>. Without it a record's
/// <c>createdAt</c> in the 201 response (100 ns ticks) would differ from the one read back from the database.
/// </summary>
public sealed class MicrosecondTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        var now = System.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
