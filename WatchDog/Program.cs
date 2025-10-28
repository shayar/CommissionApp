using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Serilog;
using EPC.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection; // NEW for Assembly Resolve
using System.Linq;
using System.Threading.Tasks;
using System;

// -----------------------------------------------------------------
// 🛑 ASSEMBLY RESOLVER FIX (Must run before DI Builder) 🛑
// This handler attempts to locate missing DLLs (like Microsoft.Extensions.DependencyInjection.Abstractions)
// within the current application's directory if the initial search fails.
AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    // Ignore resource assemblies
    if (args.Name.EndsWith(".resources")) return null;

    // Build the full path to the missing assembly in the base directory
    string assemblyName = args.Name.Split(',')[0] + ".dll";
    string path = Path.Combine(AppContext.BaseDirectory, assemblyName);

    if (File.Exists(path))
    {
        Console.WriteLine($"[Watchdog] Resolved missing assembly: {assemblyName}");
        return Assembly.LoadFrom(path);
    }
    return null;
};
// -----------------------------------------------------------------


var builder = Host.CreateApplicationBuilder(args);

// Setup Serilog logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/watchdog-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddLogging(logging => logging.AddSerilog());

// 🛑 FIX 1: Correct the database path. DB is inside the EPC.App folder.
string dbPath = Path.Combine(
    AppContext.BaseDirectory,
    "EPC.App",
    "epc_local.db"
);
string connectionString = $"Data Source={dbPath}";


// DB context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity (for role checking)
builder.Services.AddIdentityCore<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

// Execute Lock Check Logic
bool isLocked;
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    isLocked = await CheckAndSetLockAsync(db, userManager, logger);
}


if (isLocked)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("APPLICATION LOCKED. Access denied due to licensing restrictions.");
    Console.WriteLine("Contact the developer for access renewal.");
    Console.ResetColor();
    Console.WriteLine("Press any key to exit.");
    Console.ReadKey();
    return;
}
else
{
    // 🛑 LAUNCH WEB APPLICATION PROCESS 🛑
    try
    {
        // 🛑 FIX 2: Define paths using AppContext.BaseDirectory for reliable execution context
        string baseDir = AppContext.BaseDirectory;
        string appDir = Path.Combine(baseDir, "EPC.App"); // The folder containing the web app's files
        string appPath = Path.Combine(appDir, "EPC.WEB.exe"); // The actual executable name

        if (!File.Exists(appPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"WARNING: Application executable not found at {appPath}.");
            Console.WriteLine("Please ensure EPC.WEB is published to publish/EPC.App/EPC.WEB.exe.");
            Console.WriteLine("Exiting watchdog.");
            Console.ResetColor();
            return;
        }

        Console.Title = "EPC Application Watchdog (DO NOT CLOSE)";
        Console.WriteLine($"Starting EPC.WEB Application from working directory: {appDir}");

        // Access the static Process variable defined in the partial class
        Program._webProcess = new Process();
        Program._webProcess.StartInfo.FileName = appPath;
        // Set the working directory explicitly to the app's folder
        Program._webProcess.StartInfo.WorkingDirectory = appDir;
        Program._webProcess.StartInfo.UseShellExecute = true;
        Program._webProcess.EnableRaisingEvents = true;

        // Ensure the web process dies if the watchdog dies (via ShutdownWebProcess)
        Program._webProcess.Exited += (sender, eventArgs) => {
            Console.WriteLine("EPC.WEB Application has closed itself. Watchdog exiting.");
            Environment.Exit(0);
        };

        // Set up the handler to catch Ctrl+C/console close events on the Watchdog
        Console.CancelKeyPress += (sender, eventArgs) => {
            Console.WriteLine("Watchdog caught exit signal.");
            Program.ShutdownWebProcess();
            eventArgs.Cancel = true; // Allow graceful exit to continue
        };

        Program._webProcess.Start();
        Console.WriteLine("EPC.WEB is running. Close this window to stop both processes.");

        // Wait indefinitely or until the web process closes itself
        Program._webProcess.WaitForExit();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FATAL: Failed to start the web application process. Error: {ex.Message}");
        Log.Fatal(ex, "Failed to start web application process.");
        Console.ResetColor();
    }
}


// Encapsulate the database logic into a static method
static async Task<bool> CheckAndSetLockAsync(AppDbContext db, UserManager<AppUser> userManager, ILogger<Program> logger)
{
    // 🛑 NEW: Path for the lock file is relative to the Watchdog's execution path.
    string lockFilePath = Path.Combine(AppContext.BaseDirectory, "app_locked.txt");

    try
    {
        const string DeveloperName = "Shayar";
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // Find any user whose FullName contains "Shayar" OR has the "Developer" role.
        var developerUsers = await userManager.Users
            .Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(DeveloperName.ToLower())) ||
                db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Developer"))
            )
            .ToListAsync();

        bool isAppAccessGranted = false;

        foreach (var user in developerUsers)
        {
            if ((user.LastLogin ?? DateTime.MinValue) >= cutoffDate)
            {
                isAppAccessGranted = true;

                if (File.Exists(lockFilePath))
                {
                    File.Delete(lockFilePath);
                    logger.LogInformation("App UNLOCKED by developer {Email} (Recent login).", user.Email);
                    db.AuditLogs.Add(new Audit
                    {
                        Action = $"App UNLOCKED by developer {user.Email}",
                        PerformedBy = user.Email
                    });
                    await db.SaveChangesAsync();
                }
                return false; // App is not locked
            }
        }

        if (!isAppAccessGranted)
        {
            // If no developer user was found, or no active login in the last 30 days
            File.WriteAllText(lockFilePath, "LOCKED");
            logger.LogWarning("App LOCKED due to no active developer login (Cutoff: {Cutoff}).", cutoffDate);
            db.AuditLogs.Add(new Audit
            {
                Action = "App LOCKED due to no active developer login.",
                PerformedBy = "System Watchdog"
            });
            await db.SaveChangesAsync();
            return true; // App is locked
        }

        // Check if lock file exists even if access was granted (it should have been deleted above)
        return File.Exists(lockFilePath);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "FATAL ERROR in watchdog task.");
        // If database check fails, lock the app as a security measure
        File.WriteAllText(lockFilePath, "LOCKED");

        try
        {
            db.AuditLogs.Add(new Audit
            {
                Action = $"Watchdog critical error: {ex.Message}",
                PerformedBy = "System Watchdog"
            });
            await db.SaveChangesAsync();
        }
        catch { /* Ignore database error if logging fails */ }

        return true; // App is locked due to critical error
    }
}

// -----------------------------------------------------------------
// 🛑 TYPE DECLARATIONS 🛑
// -----------------------------------------------------------------
internal partial class Program
{
    // DllImports allow calling native Windows functions
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    // Define the event types for GenerateConsoleCtrlEvent
    private const int CTRL_C_EVENT = 0;

    // Static Process reference shared between the top-level statements and the methods
    public static Process? _webProcess = null;

    // Method to gracefully shut down the web process when the watchdog exits
    public static void ShutdownWebProcess()
    {
        if (_webProcess != null && !_webProcess.HasExited)
        {
            Console.WriteLine("Shutting down EPC.WEB application gracefully...");
            try
            {
                // Attach to the running process's console session
                if (AttachConsole(_webProcess.Id))
                {
                    SetConsoleCtrlHandler(IntPtr.Zero, true);
                    // Send the Ctrl+C signal to the web app (Kestrel)
                    GenerateConsoleCtrlEvent(CTRL_C_EVENT, _webProcess.Id);
                    FreeConsole();

                    // Give it a moment to shut down gracefully
                    if (!_webProcess.WaitForExit(5000))
                    {
                        _webProcess.Kill();
                        Console.WriteLine("EPC.WEB killed forcefully after timeout.");
                    }
                }
                else
                {
                    _webProcess.Kill(); // Fallback to immediate kill
                    Console.WriteLine("EPC.WEB killed forcefully (AttachConsole failed).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during shutdown: {ex.Message}. Forcing kill.");
                _webProcess.Kill();
            }
        }
    }
}
