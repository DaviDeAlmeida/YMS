using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using YMS.Application.Common;

namespace YMS.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the context without starting the API (and its hosted services). Migrations are only
/// generated from the model, so the connection string just has to be well-formed; set <c>ConnectionStrings__Yms</c>
/// to run <c>database update</c> against another server.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<YmsDbContext>
{
    private const string LocalConnectionString = "Host=localhost;Port=5433;Database=yms;Username=yms;Password=yms";

    public YmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{YmsDbContext.ConnectionStringName}")
            ?? LocalConnectionString;
        var options = new DbContextOptionsBuilder<YmsDbContext>();
        YmsDbContext.Configure(options, connectionString);

        return new YmsDbContext(options.Options, new OwnerContext());
    }
}
