using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YMS.Application.Batches.Processing;
using YMS.Application.Common;

namespace YMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        ContractPropertyNames.Apply();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<OwnerContext>();
        services.AddScoped<IOwnerContext>(sp => sp.GetRequiredService<OwnerContext>());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BatchProcessor>();

        return services;
    }
}
