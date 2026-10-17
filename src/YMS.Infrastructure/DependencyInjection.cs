using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YMS.Application.Batches;
using YMS.Application.Common;
using YMS.Infrastructure.Batches;
using YMS.Infrastructure.Persistence;

namespace YMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(YmsDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{YmsDbContext.ConnectionStringName}' is not configured.");
        services.AddDbContext<YmsDbContext>(options => YmsDbContext.Configure(options, connectionString));
        services.AddScoped<IYmsDbContext>(sp => sp.GetRequiredService<YmsDbContext>());
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new MicrosecondTimeProvider()));

        // Temporary: RabbitMQ replaces the in-process queue.
        services.AddSingleton<InProcessBatchQueue>();
        services.AddSingleton<IBatchQueue>(sp => sp.GetRequiredService<InProcessBatchQueue>());
        services.AddHostedService<BatchConsumer>();
        services.AddHostedService<BatchRepublisher>();

        return services;
    }
}
