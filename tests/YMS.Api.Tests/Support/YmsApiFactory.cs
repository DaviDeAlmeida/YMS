using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YMS.Api.Auth;
using YMS.Domain.Owners;
using YMS.Infrastructure.Persistence;

namespace YMS.Api.Tests.Support;

public sealed class YmsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _connectionString;

    public async Task InitializeAsync() => _connectionString = await PostgresServer.NewDatabaseAsync();

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{YmsDbContext.ConnectionStringName}", _connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Creates an Owner (fixture) and returns a client authenticated as that Owner.</summary>
    public async Task<HttpClient> CreateOwnerClientAsync(string name = "Owner")
    {
        var owner = Owner.Create(name, DateTimeOffset.UtcNow);
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<YmsDbContext>();
            db.Owners.Add(owner);
            await db.SaveChangesAsync();
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.OwnerHeader, owner.Id.ToString());
        return client;
    }
}

/// <summary>Test-only scheme: authenticates the request as the Owner named in a test header.</summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string OwnerHeader = "X-Test-Owner";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(OwnerHeader, out var ownerId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim(OwnerClaims.OwnerId, ownerId.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
