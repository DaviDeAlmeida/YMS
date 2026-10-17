namespace YMS.Domain.Common;

/// <summary>Base for every model: internal uuid v7 key and YMS-managed UTC timestamps.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    protected void MarkCreated(DateTimeOffset now)
    {
        CreatedAt = now;
        UpdatedAt = now;
    }

    protected void MarkUpdated(DateTimeOffset now) => UpdatedAt = now;
}
