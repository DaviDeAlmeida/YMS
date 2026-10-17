using YMS.Domain.Common;

namespace YMS.Domain.Categories;

public sealed class Category : Entity
{
    public Guid OwnerId { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Category() { }

    public static Category Create(Guid ownerId, string externalId, string description, DateTimeOffset now)
    {
        var category = new Category { OwnerId = ownerId, ExternalId = externalId, Description = description };
        category.MarkCreated(now);
        return category;
    }

    public void Update(string description, DateTimeOffset now)
    {
        Description = description;
        MarkUpdated(now);
    }
}
