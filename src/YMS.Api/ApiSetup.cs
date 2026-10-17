using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using YMS.Api.Auth;
using YMS.Api.Errors;
using YMS.Application.Common;
using YMS.Domain.Common;

namespace YMS.Api;

public static class ApiSetup
{
    public static IServiceCollection AddApi(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var enumsAsCamelCase = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
        // Problem Details are written with the HTTP JSON options, controller bodies with the MVC ones.
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(enumsAsCamelCase));
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(enumsAsCamelCase))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidRequestResponse.Create);
        services.AddProblemDetails();
        services.AddExceptionHandler<RejectionExceptionHandler>();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "YMS — API de integração do Owner",
                Version = "v1",
                Description = "Cadastro de Categorias, Produtos e Fornecedores pelo ERP do Owner, unitário ou em Lote.",
            });
            foreach (var assembly in new[] { typeof(ApiSetup).Assembly, typeof(UpsertOutcome).Assembly, typeof(Entity).Assembly })
                options.IncludeXmlComments(assembly);
        });
        services.AddOwnerAuthentication(environment);

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseSwagger();
        app.UseSwaggerUI(options => options.DocumentTitle = "YMS API");

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<OwnerContextMiddleware>();

        app.MapControllers().RequireAuthorization(OwnerAuthentication.OwnerPolicy);

        return app;
    }
}
