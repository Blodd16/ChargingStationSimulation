using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ChargingStationSimulation.Models;
using ChargingStationSimulation.Services;
using Avalonia.Threading;

namespace ChargingStationSimulation.ViewModels
{
    /// <summary>
    /// Relay command implementation for MVVM pattern
    /// Supports both synchronous and asynchronous execution
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Func<Task>? _executeAsync;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter)
        {
            if (_executeAsync != null)
                await _executeAsync();
            else
                _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Main ViewModel for the simulation application
    /// Implements MVVM pattern with INotifyPropertyChanged
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SimulationEngine _simulationEngine;
        private readonly DispatcherTimer _chartUpdateTimer;
        private bool _isSimulationRunning;
        private string _currentTimeDisplay = "Ready to Start";
        private string _simulationStatus = "Stopped";
        private Task? _simulationTask;

        public SimulationParameters Parameters { get; } = new();
        public SimulationStatistics Statistics => _simulationEngine.Statistics;
        public ObservableCollection<ChargingStation> Stations { get; } = new();

        // Chart data for real-time visualization
        public ObservableCollection<double> UtilizationChartData { get; } = new();
        public ObservableCollection<double> PowerOutputChartData { get; } = new();
        public ObservableCollection<int> QueueLengthChartData { get; } = new();

        public bool IsSimulationRunning
        {
            get => _isSimulationRunning;
            private set
            {
                SetProperty(ref _isSimulationRunning, value);
                UpdateCommands();
                SimulationStatus = value ? "Running" : "Stopped";
            }
        }

        public string CurrentTimeDisplay
        {
            get => _currentTimeDisplay;
            private set => SetProperty(ref _currentTimeDisplay, value);
        }

        public string SimulationStatus
        {
            get => _simulationStatus;
            private set => SetProperty(ref _simulationStatus, value);
        }

        // Commands
        public RelayCommand StartSimulationCommand { get; }
        public RelayCommand StopSimulationCommand { get; }
        public RelayCommand ResetSimulationCommand { get; }
        public RelayCommand SetSlowSpeedCommand { get; }
        public RelayCommand SetMediumSpeedCommand { get; }
        public RelayCommand SetFastSpeedCommand { get; }
        public RelayCommand SetUltraFastSpeedCommand { get; }

        public MainViewModel()
        {
            _simulationEngine = new SimulationEngine();

            // Subscribe to simulation events
            _simulationEngine.StationsUpdated += OnStationsUpdated;
            _simulationEngine.StatisticsUpdated += OnStatisticsUpdated;
            _simulationEngine.TimeUpdated += OnTimeUpdated;
            _simulationEngine.VehicleEvent += OnVehicleEvent;

            // Initialize commands
            StartSimulationCommand = new RelayCommand(StartSimulation, () => !IsSimulationRunning);
            StopSimulationCommand = new RelayCommand(StopSimulation, () => IsSimulationRunning);
            ResetSimulationCommand = new RelayCommand(ResetSimulation, () => !IsSimulationRunning);

            SetSlowSpeedCommand = new RelayCommand(() => SetSpeed(1.0));
            SetMediumSpeedCommand = new RelayCommand(() => SetSpeed(10.0));
            SetFastSpeedCommand = new RelayCommand(() => SetSpeed(50.0));
            SetUltraFastSpeedCommand = new RelayCommand(() => SetSpeed(100.0));

            // Setup chart update timer
            _chartUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _chartUpdateTimer.Tick += UpdateChartData;

            // Initialize simulation
            ResetSimulation();
        }

        /// <summary>
        /// Start the simulation asynchronously
        /// </summary>
        private async Task StartSimulation()
        {
            try
            {
                IsSimulationRunning = true;
                CurrentTimeDisplay = "Initializing...";

                _simulationEngine.Initialize(Parameters);
                _chartUpdateTimer.Start();

                _simulationTask = _simulationEngine.RunSimulation(Parameters);
                await _simulationTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation error: {ex.Message}");
                CurrentTimeDisplay = "Error occurred";
            }
            finally
            {
                IsSimulationRunning = false;
                _chartUpdateTimer.Stop();
                CurrentTimeDisplay = "Simulation Complete";
            }
        }

        /// <summary>
        /// Stop the running simulation
        /// </summary>
        private void StopSimulation()
        {
            _simulationEngine.Stop();
            _chartUpdateTimer.Stop();
            IsSimulationRunning = false;
            CurrentTimeDisplay = "Simulation Stopped";
        }

        /// <summary>
        /// Reset simulation to initial state
        /// </summary>
        private void ResetSimulation()
        {
            if (IsSimulationRunning)
            {
                StopSimulation();
            }

            _simulationEngine.Initialize(Parameters);
            UpdateStationsDisplay();
            ClearChartData();
            OnPropertyChanged(nameof(Statistics));
            CurrentTimeDisplay = "Ready to Start";
        }

        /// <summary>
        /// Set simulation speed
        /// </summary>
        private void SetSpeed(double speed)
        {
            Parameters.SimulationSpeed = speed;
            OnPropertyChanged(nameof(IsSlowSpeed));
            OnPropertyChanged(nameof(IsMediumSpeed));
            OnPropertyChanged(nameof(IsFastSpeed));
            OnPropertyChanged(nameof(IsUltraFastSpeed));
        }

        /// <summary>
        /// Update chart data for real-time visualization
        /// </summary>
        private void UpdateChartData(object? sender, EventArgs e)
        {
            if (!IsSimulationRunning) return;

            try
            {
                // Update utilization chart
                var recentUtilization = Statistics.GetRecentUtilization(50);
                if (recentUtilization != null && recentUtilization.Count > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        UtilizationChartData.Clear();
                        foreach (var item in recentUtilization)
                        {
                            UtilizationChartData.Add(item);
                        }
                    });
                }

                // Update power output chart
                var recentPower = Statistics.GetRecentPowerOutput(50);
                if (recentPower != null && recentPower.Count > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        PowerOutputChartData.Clear();
                        foreach (var item in recentPower)
                        {
                            PowerOutputChartData.Add(item);
                        }
                    });
                }

                // Update queue length chart
                var recentQueue = Statistics.GetRecentQueueLength(50);
                if (recentQueue != null && recentQueue.Count > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        QueueLengthChartData.Clear();
                        foreach (var item in recentQueue)
                        {
                            QueueLengthChartData.Add(item);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chart update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all chart data
        /// </summary>
        private void ClearChartData()
        {
            UtilizationChartData.Clear();
            PowerOutputChartData.Clear();
            QueueLengthChartData.Clear();
        }

        private void UpdateCommands()
        {
            StartSimulationCommand.RaiseCanExecuteChanged();
            StopSimulationCommand.RaiseCanExecuteChanged();
            ResetSimulationCommand.RaiseCanExecuteChanged();
        }

        private void OnStationsUpdated(List<ChargingStation> stations)
        {
            Dispatcher.UIThread.Post(() => UpdateStationsDisplay());
        }

        private void OnStatisticsUpdated(SimulationStatistics statistics)
        {
            Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(Statistics));
                OnPropertyChanged(nameof(ActiveVehiclesCount));
                OnPropertyChanged(nameof(SystemPowerOutput));
                OnPropertyChanged(nameof(EfficiencyText));
            });
        }

        private void OnTimeUpdated(DateTime currentTime)
        {
            var elapsed = currentTime - DateTime.Now.Date;
            Dispatcher.UIThread.Post(() =>
                CurrentTimeDisplay = $"Time: {currentTime:HH:mm:ss} | Elapsed: {elapsed:hh\\:mm\\:ss}");
        }

        private void OnVehicleEvent(Vehicle vehicle, string eventDescription)
        {
            // Log vehicle events (can be extended for UI notifications)
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {eventDescription}");
        }

        private void UpdateStationsDisplay()
        {
            Stations.Clear();
            foreach (var station in _simulationEngine.Stations)
            {
                Stations.Add(station);
            }
        }

        // UI helper properties
        public bool IsSlowSpeed => Math.Abs(Parameters.SimulationSpeed - 1.0) < 0.1;
        public bool IsMediumSpeed => Math.Abs(Parameters.SimulationSpeed - 10.0) < 0.1;
        public bool IsFastSpeed => Math.Abs(Parameters.SimulationSpeed - 50.0) < 0.1;
        public bool IsUltraFastSpeed => Math.Abs(Parameters.SimulationSpeed - 100.0) < 0.1;

        public string ActiveVehiclesCount =>
            $"{_simulationEngine.GetAllActiveVehicles().Count}";

        public string SystemPowerOutput =>
            $"{Statistics.CurrentPowerOutputKw:F1} kW";

        public string EfficiencyText =>
            $"{Statistics.ProcessingEfficiency:F1}%";

        public string TotalEnergyDelivered =>
            $"{Statistics.TotalEnergyDeliveredKwh:F1} kWh";

        public string AverageEnergyPerVehicle =>
            $"{Statistics.AverageEnergyPerVehicle:F1} kWh";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }
    }
}