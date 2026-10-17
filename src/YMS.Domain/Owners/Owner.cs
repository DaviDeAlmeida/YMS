using YMS.Domain.Common;

namespace YMS.Domain.Owners;

public sealed class Owner : Entity
{
    public string Name { get; private set; } = null!;

    private Owner() { }

    public static Owner Create(string name, DateTimeOffset now)
    {
        var owner = new Owner { Name = name };
        owner.MarkCreated(now);
        return owner;
    }

    /// <summary>An Owner with a known id, for the local environment's seed.</summary>
    public static Owner Seed(Guid id, string name, DateTimeOffset now)
    {
        var owner = Create(name, now);
        owner.Id = id;
        return owner;
    }
}
