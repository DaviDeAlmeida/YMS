using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YMS.Application.Batches;
using YMS.Application.Common;
using YMS.Infrastructure.Batches;
using YMS.Infrastructure.Persistence;

namespace YMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Temporary: PostgreSQL (Npgsql + migrations) replaces the in-memory provider once Docker is available.
        var databaseName = configuration["Database:InMemoryName"] ?? "yms";
        services.AddDbContext<YmsDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IYmsDbContext>(sp => sp.GetRequiredService<YmsDbContext>());

        // Temporary: RabbitMQ replaces the in-process queue once Docker is available.
        services.AddSingleton<InProcessBatchQueue>();
        services.AddSingleton<IBatchQueue>(sp => sp.GetRequiredService<InProcessBatchQueue>());
        services.AddHostedService<BatchConsumer>();
        services.AddHostedService<BatchRepublisher>();

        return services;
    }
}
