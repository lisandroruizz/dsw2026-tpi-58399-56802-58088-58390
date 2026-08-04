using Dsw2026Tpi.Data;
using Dsw2026Tpi.Data.Extensions;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Api.Configurations;

public static class PersistenceConfigurationExtensions
{
    public static IServiceCollection AddApplicationPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró DefaultConnection.");

        services.AddDbContext<Dsw2026TpiDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDbContext<AuthenticationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseSeeding((c, t) =>
            {
                c.Seedwork<IdentityRole>("Sources\\roles.json");
            });
        });
        return services;
    }
}
