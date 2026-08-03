using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Api.Configurations
{
    public static class IdentityInitializationExtensions
    {
        public static async Task InitializeIdentityAsync(this WebApplication app)
        {
            using IServiceScope scope = app.Services.CreateScope();
            RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("IdentityInitialization");

            await EnsureRole(roleManager, Roles.Administrator);
            await EnsureRole(roleManager, Roles.Patient);

            string email = app.Configuration["InitialAdmin:Email"]
                ?? throw new InvalidOperationException("InitialAdmin:Email no configurado.");
            string password = app.Configuration["InitialAdmin:Password"]
                ?? throw new InvalidOperationException("InitialAdmin:Password no configurado.");

            if (password.Length < 8)
            {
                throw new InvalidOperationException("La contraseña del administrador inicial debe tener al menos 8 caracteres.");
            }

            email = email.Trim().ToLowerInvariant();
            ApplicationUser? administrator = await userManager.FindByEmailAsync(email);

            if (administrator is null)
            {
                administrator = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                IdentityResult createResult = await userManager.CreateAsync(administrator, password);
                if (!createResult.Succeeded)
                {
                    string errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"No se pudo crear el administrador inicial: {errors}");
                }

                await userManager.AddToRoleAsync(administrator, Roles.Administrator);
                logger.LogInformation("Se creó el administrador inicial {Email}", email);
                return;
            }

            if (!await userManager.IsInRoleAsync(administrator, Roles.Administrator))
            {
                await userManager.AddToRoleAsync(administrator, Roles.Administrator);
            }

            logger.LogInformation("El administrador inicial ya existe");
        }

        private static async Task EnsureRole(RoleManager<IdentityRole> roleManager, string role)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                IdentityResult result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"No se pudo crear el rol {role}: {errors}");
                }
            }
        }
    }

}
