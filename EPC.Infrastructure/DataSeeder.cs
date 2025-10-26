using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Serilog;

namespace EPC.Infrastructure
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Apply migrations if any are pending
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Log.Information("Applying {Count} pending EF Core migrations...", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                    Log.Information("Migrations applied successfully.");
                }
                else
                {
                    Log.Information("No EF Core migrations were pending.");
                }

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                string[] roles = { "Admin", "Employee", "Developer" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(role));
                        if (result.Succeeded)
                            Log.Information("Created role: {Role}", role);
                        else
                            Log.Warning("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                var adminEmail = "admin@epc.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var newAdmin = new AppUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FullName = "System Admin",
                        StoreId = "SYS"
                    };

                    var createResult = await userManager.CreateAsync(newAdmin, "Admin@1234");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                        Log.Information("Admin user created and assigned to Admin role.");
                    }
                    else
                    {
                        Log.Error("Failed to create admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    Log.Information("Admin user already exists.");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred during data seeding or migration.");
                throw; // Optional: rethrow if critical
            }
        }
    }
}
