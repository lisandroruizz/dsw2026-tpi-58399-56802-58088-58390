using Dsw2026Tpi.Api.Resources;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
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
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });


        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminPolicy, policy =>
                policy.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.PatientPolicy, policy =>
                policy.RequireRole(Roles.Patient));
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
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true
            };

        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<AuthenticationDbContext>()
          .AddSignInManager()
          .AddDefaultTokenProviders();
        return services;
    }

    public static IServiceCollection
        AddAppRateLimiting(this IServiceCollection services,IConfiguration configuration)
    {
        int generalPermit = configuration.GetValue( "RateLimiting:General:PermitLimit", 100);

        int generalWindow =configuration.GetValue("RateLimiting:General:WindowSeconds",  60);

        int adminLoginPermit =configuration.GetValue("RateLimiting:AdminLogin:PermitLimit",5);

        int adminLoginWindow =configuration.GetValue("RateLimiting:AdminLogin:WindowSeconds",60);

        int patientLoginPermit = configuration.GetValue("RateLimiting:PatientLogin:PermitLimit", 10);

        int patientLoginWindow = configuration.GetValue("RateLimiting:PatientLogin:WindowSeconds",60);

        int bookingPermit = configuration.GetValue( "RateLimiting:AppointmentBooking:PermitLimit", 5);

        int bookingWindow = configuration.GetValue( "RateLimiting:AppointmentBooking:WindowSeconds",  60);

        services.AddRateLimiter(
            options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext,string>( context =>
                                RateLimitPartition.GetFixedWindowLimiter( GetUserOrIpKey( context),
                                        _ =>
                                            CreateOptions( generalPermit,generalWindow)));

                options.AddPolicy(
                    RateLimitPolicies.AdminLogin,
                    context => RateLimitPartition.GetFixedWindowLimiter(GetIpKey(context),
                                _ =>
                                    CreateOptions( adminLoginPermit, adminLoginWindow)));

                options.AddPolicy(
                    RateLimitPolicies
                        .PatientLogin,
                    context =>
                        RateLimitPartition
                            .GetFixedWindowLimiter(
                                GetIpKey(context),
                                _ =>
                                    CreateOptions(
                                        patientLoginPermit,
                                        patientLoginWindow)));

                options.AddPolicy(
                    RateLimitPolicies
                        .AppointmentBooking,
                    context =>
                        RateLimitPartition
                            .GetFixedWindowLimiter(
                                GetAuthenticatedUserKey(
                                    context),
                                _ =>
                                    CreateOptions(
                                        bookingPermit,
                                        bookingWindow)));

                options.OnRejected =
                    async (
                        context,
                        cancellationToken) =>
                    {
                        ILogger logger =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    ILoggerFactory>()
                                .CreateLogger(
                                    "RateLimiting");

                        logger.LogWarning(
                            "Solicitud rechazada por rate limiting. Ruta: {Path}, clave: {PartitionKey}",
                            context.HttpContext
                                .Request.Path,
                            GetUserOrIpKey(
                                context.HttpContext));

                        await context
                            .HttpContext
                            .Response
                            .WriteAsJsonAsync(
                                new ErrorResponse(
                                    ErrorCodeNames
                                        .RateLimitExceeded,
                                    "Se excedió la cantidad permitida de solicitudes."),
                                cancellationToken);
                    };
            });

        return services;
    }

    private static
        FixedWindowRateLimiterOptions
        CreateOptions(
            int permitLimit,
            int windowSeconds)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window =
                TimeSpan.FromSeconds(
                    windowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }

    private static string GetIpKey(
        HttpContext context)
    {
        return context.Connection
            .RemoteIpAddress?
            .ToString()
            ?? "unknown-ip";
    }

    private static string
        GetAuthenticatedUserKey(
            HttpContext context)
    {
        return context.User
            .FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? GetIpKey(context);
    }

    private static string GetUserOrIpKey(
        HttpContext context)
    {
        return context.User.Identity?
            .IsAuthenticated == true ? GetAuthenticatedUserKey( context) : GetIpKey(context);
    }
}
