using Avalonia;
using System;

namespace ChargingStationSimulation
{
    /// <summary>
    /// Program entry point
    /// Configures and launches the Avalonia application
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Configure Avalonia application builder
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}