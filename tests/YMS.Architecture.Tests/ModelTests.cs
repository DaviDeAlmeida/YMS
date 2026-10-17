using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using YMS.Infrastructure.Persistence;

namespace YMS.Architecture.Tests;

/// <summary>The persistence rules of docs/design/modelagem-dominio.md, checked against the EF model (no database needed).</summary>
public sealed class ModelTests
{
    private static readonly IModel Model =
        new DesignTimeDbContextFactory().CreateDbContext([]).GetService<IDesignTimeModel>().Model;

    [Fact]
    public void Every_Owner_table_has_owner_id()
    {
        var withoutOwner = Model.GetRelationalModel().Tables
            .Where(t => t.Name != "owners" && t.FindColumn("owner_id") is null)
            .Select(t => t.Name);

        Assert.Empty(withoutOwner);
    }

    [Fact]
    public void Every_key_is_a_uuid_Id_generated_by_the_application()
    {
        var wrongKeys = Model.GetEntityTypes()
            // An owned one-to-one type shares its owner's row and key.
            .Where(e => e.FindOwnership() is not { IsUnique: true })
            .Where(e => !IsApplicationGeneratedUuidId(e))
            .Select(e => e.DisplayName());

        Assert.Empty(wrongKeys);
    }

    private static bool IsApplicationGeneratedUuidId(IEntityType entityType)
    {
        if (entityType.FindPrimaryKey()?.Properties is not [{ Name: "Id" } id] || id.ClrType != typeof(Guid))
            return false;

        // Either the domain sets it (Guid.CreateVersion7() on the model) or EF asks the application's generator.
        return id.ValueGenerated == ValueGenerated.Never
            || id.GetValueGeneratorFactory()?.Invoke(id, entityType) is GuidV7ValueGenerator;
    }
}
