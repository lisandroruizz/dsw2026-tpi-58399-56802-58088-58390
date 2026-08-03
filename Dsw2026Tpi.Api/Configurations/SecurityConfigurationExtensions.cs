using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

public static class SecurityConfigurationExtensions
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener parámetros para creación del JWT desde appsettings.json
        var jwtConfig = configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
        var issuer = jwtConfig["Issuer"] ?? throw new ArgumentNullException("JWT Issuer");
        var audience = jwtConfig["Audience"] ?? throw new ArgumentNullException("JWT Audience");
        var key = Encoding.UTF8.GetBytes(keyText);

        //Agregar autenticación
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                //Definir parámetros para la generación del token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse(
                            "AUTHENTICATION_FAILED", "Se requiere autenticación para acceder al recurso."));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse(
                            "AUTHORIZATION_FAILED","No posee permisos para realizar la operación."));
                    }
                };

            });

        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener configuración para CORS desde appsettings.json
        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>()?
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .Select(origin => origin.TrimEnd('/'))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();

        //Si no se definió configuración en el archivo, utilizar la que se define
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "http://localhost",
                "https://localhost"
            ];
        }

        //Agregar CORS con la política por defecto a partir de las URLs definidas
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 8,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = false
            };

        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<AuthenticationDbContext>()
          .AddSignInManager()
          .AddDefaultTokenProviders();
        return services;
    }

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddFixedWindowLimiter("login", limiter =>
            {
                limiter.PermitLimit = 5;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
                limiter.AutoReplenishment = true;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ErrorResponse(
                       "RATE_LIMIT_EXCEEDED",
                       "Se excedió la cantidad permitida de solicitudes."),
                    cancellationToken);
            };
        });

        return services;
    }
}
