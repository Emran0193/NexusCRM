using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Infrastructure.Identity;

public static class IdentityDependencyInjection
{
    public static IServiceCollection AddNexusIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthSecurityOptions>(options =>
        {
            configuration.GetSection(AuthSecurityOptions.SectionName).Bind(options);
            var jwtRefresh = configuration.GetValue<int?>("Jwt:RefreshTokenDays");
            if (jwtRefresh is > 0 && options.RefreshTokenDays == 14)
            {
                options.RefreshTokenDays = jwtRefresh.Value;
            }
        });

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IAuthAuditService, AuthAuditService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !isDevelopment;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email
                };

                // SignalR browsers send JWT via query string for WebSockets.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
