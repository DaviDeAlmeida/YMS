using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using YMS.Application.Common;

namespace YMS.Api.Auth;

public static class OwnerAuthentication
{
    public const string OwnerPolicy = "Owner";

    /// <summary>
    /// The Owner always comes from the <see cref="OwnerClaims.OwnerId"/> claim. Issuing that claim (JWT, IdentityServer)
    /// belongs to the authentication spec; tests register their own scheme, and Development signs every request in
    /// as the Owner configured in <c>Development:OwnerId</c>.
    /// </summary>
    public static IServiceCollection AddOwnerAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.AddAuthentication(DevelopmentOwnerAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevelopmentOwnerAuthenticationHandler>(
                    DevelopmentOwnerAuthenticationHandler.SchemeName, _ => { });
        }
        else
        {
            services.AddAuthentication();
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(OwnerPolicy, policy => policy.RequireAuthenticatedUser().RequireClaim(OwnerClaims.OwnerId));

        return services;
    }
}

/// <summary>Resolves the current Owner once per request, from the authenticated user's claim.</summary>
public sealed class OwnerContextMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, OwnerContext ownerContext)
    {
        if (Guid.TryParse(context.User.FindFirstValue(OwnerClaims.OwnerId), out var ownerId))
            ownerContext.Set(ownerId);

        return next(context);
    }
}

/// <summary>Local-only scheme: every request is the seeded Owner. Never registered outside Development.</summary>
public sealed class DevelopmentOwnerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentOwner";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ownerId = configuration["Development:OwnerId"];
        if (string.IsNullOrEmpty(ownerId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim(OwnerClaims.OwnerId, ownerId)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
