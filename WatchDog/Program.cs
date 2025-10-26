using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Serilog;
using EPC.Domain.Entities;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Setup Serilog logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/watchdog-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddLogging(logging => logging.AddSerilog());

// DB context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=EPC.WEB\\epc_local.db"));

// Identity (for role checking)
builder.Services.AddIdentityCore<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    // Find a developer user who has logged in in the last 30 days
    var users = await userManager.Users.ToListAsync();

    foreach (var user in users)
    {
        if (await userManager.IsInRoleAsync(user, "Developer") &&
            (DateTime.UtcNow - (user.LastLogin ?? DateTime.MinValue)).TotalDays <= 30)
        {
            if (File.Exists("app_locked.txt"))
            {
                File.Delete("app_locked.txt");
                logger.LogInformation("App unlocked by developer {Email}", user.Email);
                db.AuditLogs.Add(new Audit
                {
                    Action = $"App unlocked by developer {user.Email}",
                    PerformedBy = user.Email
                });
                await db.SaveChangesAsync();
            }

            return;
        }
    }

    // If no recent developer login
    File.WriteAllText("app_locked.txt", "LOCKED");
    logger.LogWarning("App locked due to no active developer login.");
    db.AuditLogs.Add(new Audit
    {
        Action = "App locked due to no active developer login.",
        PerformedBy = "System"
    });
    await db.SaveChangesAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "Error in watchdog task.");
    db.AuditLogs.Add(new Audit
    {
        Action = $"Watchdog error: {ex.Message}",
        PerformedBy = "System"
    });
    await db.SaveChangesAsync();
}
