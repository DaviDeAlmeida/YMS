namespace YMS.Application.Common;

/// <summary>
/// The Owner the current request (or batch message) acts on. Resolved once per scope, never from the payload:
/// the API sets it from the authenticated user's claim, the batch consumer from the Batch's own owner.
/// </summary>
public interface IOwnerContext
{
    Guid OwnerId { get; }
}

public sealed class OwnerContext : IOwnerContext
{
    private Guid? _ownerId;

    public Guid OwnerId => _ownerId ?? throw new InvalidOperationException("The current Owner was not resolved for this scope.");

    public void Set(Guid ownerId)
    {
        if (_ownerId is not null && _ownerId != ownerId)
            throw new InvalidOperationException("The current Owner was already resolved for this scope.");
        _ownerId = ownerId;
    }
}
