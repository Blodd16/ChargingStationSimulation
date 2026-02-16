using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChargingStationSimulation.Models
{
    /// <summary>
    /// Comprehensive statistics tracking for the simulation
    /// Includes real-time metrics and historical data
    /// </summary>
    public class SimulationStatistics : INotifyPropertyChanged
    {
        private readonly List<Vehicle> _completedVehicles = new();
        private readonly List<double> _utilizationHistory = new();
        private readonly List<double> _powerOutputHistory = new();
        private readonly List<int> _queueLengthHistory = new();
        private int _totalVehiclesProcessed;
        private int _totalVehiclesGenerated;
        private int _totalVehiclesRejected;
        private double _averageWaitingTime;
        private double _averageUtilization;
        private int _maxQueueLength;
        private TimeSpan _simulationTime;
        private double _totalEnergyDeliveredKwh;
        private double _currentPowerOutputKw;

        public int TotalVehiclesProcessed
        {
            get => _totalVehiclesProcessed;
            private set => SetProperty(ref _totalVehiclesProcessed, value);
        }

        public int TotalVehiclesGenerated
        {
            get => _totalVehiclesGenerated;
            set => SetProperty(ref _totalVehiclesGenerated, value);
        }

        public int TotalVehiclesRejected
        {
            get => _totalVehiclesRejected;
            set => SetProperty(ref _totalVehiclesRejected, value);
        }

        public double AverageWaitingTime
        {
            get => _averageWaitingTime;
            private set => SetProperty(ref _averageWaitingTime, value);
        }

        public double AverageUtilization
        {
            get => _averageUtilization;
            private set => SetProperty(ref _averageUtilization, value);
        }

        public int MaxQueueLength
        {
            get => _maxQueueLength;
            private set => SetProperty(ref _maxQueueLength, value);
        }

        public TimeSpan SimulationTime
        {
            get => _simulationTime;
            set => SetProperty(ref _simulationTime, value);
        }

        /// <summary>
        /// Total energy delivered to all vehicles in kWh
        /// </summary>
        public double TotalEnergyDeliveredKwh
        {
            get => _totalEnergyDeliveredKwh;
            private set => SetProperty(ref _totalEnergyDeliveredKwh, value);
        }

        /// <summary>
        /// Current power output across all stations in kW
        /// </summary>
        public double CurrentPowerOutputKw
        {
            get => _currentPowerOutputKw;
            set => SetProperty(ref _currentPowerOutputKw, value);
        }

        public string SimulationTimeFormatted =>
            $"{_simulationTime.Hours:D2}:{_simulationTime.Minutes:D2}:{_simulationTime.Seconds:D2}";

        /// <summary>
        /// Percentage of vehicles successfully processed vs generated
        /// </summary>
        public double ProcessingEfficiency =>
            _totalVehiclesGenerated > 0 ? (double)_totalVehiclesProcessed / _totalVehiclesGenerated * 100 : 0;

        /// <summary>
        /// Rejection rate percentage
        /// </summary>
        public double RejectionRate =>
            _totalVehiclesGenerated > 0 ? (double)_totalVehiclesRejected / _totalVehiclesGenerated * 100 : 0;

        /// <summary>
        /// Average energy delivered per vehicle in kWh
        /// </summary>
        public double AverageEnergyPerVehicle =>
            _totalVehiclesProcessed > 0 ? _totalEnergyDeliveredKwh / _totalVehiclesProcessed : 0;

        /// <summary>
        /// Peak power output recorded in kW
        /// </summary>
        public double PeakPowerOutputKw { get; private set; }

        public List<Vehicle> CompletedVehicles => _completedVehicles.ToList();
        public List<double> UtilizationHistory => _utilizationHistory.ToList();
        public List<double> PowerOutputHistory => _powerOutputHistory.ToList();
        public List<int> QueueLengthHistory => _queueLengthHistory.ToList();

        /// <summary>
        /// Add completed vehicle and update energy metrics
        /// </summary>
        public void AddCompletedVehicle(Vehicle vehicle)
        {
            _completedVehicles.Add(vehicle);
            TotalVehiclesProcessed = _completedVehicles.Count;

            // Update energy delivered
            var energyDelivered = vehicle.BatteryCapacityKwh *
                (vehicle.TargetBatteryLevel - vehicle.BatteryLevel) / 100.0;
            TotalEnergyDeliveredKwh += energyDelivered;

            CalculateAverageWaitingTime();
            OnPropertyChanged(nameof(ProcessingEfficiency));
            OnPropertyChanged(nameof(AverageEnergyPerVehicle));
        }

        /// <summary>
        /// Update utilization metrics with historical tracking
        /// </summary>
        public void UpdateUtilization(double utilization)
        {
            _utilizationHistory.Add(utilization);

            // Keep last 1000 data points for performance
            if (_utilizationHistory.Count > 1000)
                _utilizationHistory.RemoveAt(0);

            AverageUtilization = _utilizationHistory.Count > 0 ? _utilizationHistory.Average() : 0;
        }

        /// <summary>
        /// Update power output metrics with historical tracking
        /// </summary>
        public void UpdatePowerOutput(double powerKw)
        {
            CurrentPowerOutputKw = powerKw;
            _powerOutputHistory.Add(powerKw);

            // Keep last 1000 data points for performance
            if (_powerOutputHistory.Count > 1000)
                _powerOutputHistory.RemoveAt(0);

            if (powerKw > PeakPowerOutputKw)
            {
                PeakPowerOutputKw = powerKw;
                OnPropertyChanged(nameof(PeakPowerOutputKw));
            }
        }

        /// <summary>
        /// Update queue length with historical tracking
        /// </summary>
        public void UpdateMaxQueueLength(int queueLength)
        {
            _queueLengthHistory.Add(queueLength);

            // Keep last 1000 data points for performance
            if (_queueLengthHistory.Count > 1000)
                _queueLengthHistory.RemoveAt(0);

            if (queueLength > MaxQueueLength)
                MaxQueueLength = queueLength;
        }

        private void CalculateAverageWaitingTime()
        {
            if (_completedVehicles.Count > 0)
            {
                AverageWaitingTime = _completedVehicles
                    .Average(v => v.WaitingTime.TotalMinutes);
            }
        }

        /// <summary>
        /// Get vehicle count by type
        /// </summary>
        public Dictionary<VehicleType, int> GetVehicleTypeStatistics()
        {
            return _completedVehicles
                .GroupBy(v => v.Type)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Get average waiting time by vehicle type
        /// </summary>
        public Dictionary<VehicleType, double> GetAverageWaitingTimeByType()
        {
            return _completedVehicles
                .GroupBy(v => v.Type)
                .ToDictionary(g => g.Key, g => g.Average(v => v.WaitingTime.TotalMinutes));
        }

        /// <summary>
        /// Get average energy delivered by vehicle type
        /// </summary>
        public Dictionary<VehicleType, double> GetAverageEnergyByType()
        {
            return _completedVehicles
                .GroupBy(v => v.Type)
                .ToDictionary(g => g.Key, g => g.Average(v =>
                    v.BatteryCapacityKwh * (v.TargetBatteryLevel - v.BatteryLevel) / 100.0));
        }

        /// <summary>
        /// Calculate throughput (vehicles per hour)
        /// </summary>
        public double GetThroughput()
        {
            if (_simulationTime.TotalHours < 0.1) return 0;
            return _totalVehiclesProcessed / _simulationTime.TotalHours;
        }

        /// <summary>
        /// Get recent utilization trend (last 50 data points)
        /// </summary>
        public List<double> GetRecentUtilization(int count = 50)
        {
            return _utilizationHistory.TakeLast(count).ToList();
        }

        /// <summary>
        /// Get recent power output trend (last 50 data points)
        /// </summary>
        public List<double> GetRecentPowerOutput(int count = 50)
        {
            return _powerOutputHistory.TakeLast(count).ToList();
        }

        /// <summary>
        /// Get recent queue length trend (last 50 data points)
        /// </summary>
        public List<int> GetRecentQueueLength(int count = 50)
        {
            return _queueLengthHistory.TakeLast(count).ToList();
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void Reset()
        {
            _completedVehicles.Clear();
            _utilizationHistory.Clear();
            _powerOutputHistory.Clear();
            _queueLengthHistory.Clear();
            TotalVehiclesProcessed = 0;
            TotalVehiclesGenerated = 0;
            TotalVehiclesRejected = 0;
            AverageWaitingTime = 0;
            AverageUtilization = 0;
            MaxQueueLength = 0;
            SimulationTime = TimeSpan.Zero;
            TotalEnergyDeliveredKwh = 0;
            CurrentPowerOutputKw = 0;
            PeakPowerOutputKw = 0;
        }

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