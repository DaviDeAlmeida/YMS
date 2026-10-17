using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YMS.Domain.Owners;

namespace YMS.Infrastructure.Persistence;

public static class DevelopmentSeed
{
    /// <summary>Creates the local Owner named in <c>Development:OwnerId</c>, the one the Development scheme signs requests in as.</summary>
    public static async Task SeedDevelopmentOwnerAsync(this IServiceProvider services, IConfiguration configuration)
    {
        if (!Guid.TryParse(configuration["Development:OwnerId"], out var ownerId))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YmsDbContext>();
        if (await db.Owners.AnyAsync(o => o.Id == ownerId))
            return;

        db.Owners.Add(Owner.Seed(ownerId, configuration["Development:OwnerName"] ?? "Owner local", TimeProvider.System.GetUtcNow()));
        await db.SaveChangesAsync();
    }
}
