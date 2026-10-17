using System.Text.Json.Serialization;
using YMS.Api.Auth;
using YMS.Api.Endpoints;
using YMS.Api.Errors;

namespace YMS.Api;

public static class ApiSetup
{
    public static IServiceCollection AddApi(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
        services.AddProblemDetails();
        services.AddExceptionHandler<RejectionExceptionHandler>();
        services.AddOpenApi();
        services.AddOwnerAuthentication(environment);

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<OwnerContextMiddleware>();

        app.MapOpenApi();

        var v1 = app.MapGroup(Routes.V1).RequireAuthorization(OwnerAuthentication.OwnerPolicy);
        v1.MapCategoryEndpoints();
        v1.MapProductEndpoints();
        v1.MapSupplierEndpoints();
        v1.MapBatchEndpoints();

        return app;
    }
}
