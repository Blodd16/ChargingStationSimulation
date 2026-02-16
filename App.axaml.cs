using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChargingStationSimulation.Views;

namespace ChargingStationSimulation
{
    /// <summary>
    /// Main application class
    /// Entry point for the EV Charging Station Simulation
    /// </summary>
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    Title = "EV Charging Station Simulation - Professional Analytics Suite"
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}