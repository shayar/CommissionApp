using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WatchDog
{
    public class WatchDogRunner
    {
        // Import necessary functions for Windows process management
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        public static void Main(string[] args)
        {
            Console.Title = "EPC Application Watchdog (DO NOT CLOSE)";

            // 1. Execute the core locking logic (The logic in Program.cs runs when this assembly is executed)
            // Assuming this Runner file is compiled into the same assembly as Program.cs, or Program.cs is executed first.
            // For a separate runner, Program.cs is typically renamed to WatchdogLogic.cs and called here.
            // Since you provided Program.cs as the logic, we assume that logic runs automatically.

            // 2. Start the EPC.WEB application (assumed path after publishing)
            string appPath = Path.Combine(Directory.GetCurrentDirectory(), "publishApp\\EPC.App", "EPC.WEB.exe");

            if (!File.Exists(appPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Application executable not found at {appPath}.");
                Console.WriteLine("Ensure you have published EPC.WEB to publishApp/EPC.APP.exe.");
                Console.ResetColor();
                return;
            }

            Process webProcess = new Process();
            webProcess.StartInfo.FileName = appPath;
            webProcess.StartInfo.UseShellExecute = true; // Use the shell to launch the app
            webProcess.EnableRaisingEvents = true;

            webProcess.Exited += (sender, eventArgs) => {
                Console.WriteLine("EPC.WEB Application has closed.");
            };

            Console.WriteLine($"Starting EPC.WEB Application from: {appPath}");
            webProcess.Start();

            // 3. Keep the Runner process alive until Web Process or Runner is closed
            Console.CancelKeyPress += (sender, eventArgs) => {
                Console.WriteLine("Watchdog caught Ctrl+C. Shutting down application...");
                // Signal the application to close when the watchdog is closed
                ShutdownWebProcess(webProcess);
            };

            Console.WriteLine("Watchdog is running. Press Ctrl+C or close this window to stop the application.");

            // Wait indefinitely or until the web process closes
            webProcess.WaitForExit();

            Console.WriteLine("Watchdog shutting down.");
        }

        // Gracefully shuts down the ASP.NET Core Kestrel process
        private static void ShutdownWebProcess(Process process)
        {
            if (process != null && !process.HasExited)
            {
                try
                {
                    // Attempt to send Ctrl+C signal for graceful shutdown (Windows specific)
                    if (AttachConsole(process.Id))
                    {
                        SetConsoleCtrlHandler(IntPtr.Zero, true);
                        GenerateConsoleCtrlEvent(0, process.Id);
                        FreeConsole();

                        // Give it a moment to shut down gracefully
                        if (!process.WaitForExit(5000))
                        {
                            process.Kill();
                        }
                    }
                    else
                    {
                        process.Kill(); // Fallback to immediate kill if signal fails
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during shutdown: {ex.Message}. Forcing kill.");
                    process.Kill();
                }
            }
        }
    }
}
