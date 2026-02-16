using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChargingStationSimulation.Models
{
    /// <summary>
    /// Configuration parameters for the charging station simulation
    /// Implements INotifyPropertyChanged for two-way data binding
    /// </summary>
    public class SimulationParameters : INotifyPropertyChanged
    {
        private int _numberOfStations = 3;
        private int _slotsPerStation = 4;
        private int _simulationDurationHours = 8;
        private double _arrivalRateCarsPerHour = 12;
        private double _arrivalRateTrucksPerHour = 4;
        private double _arrivalRateBusesPerHour = 2;
        private double _rushHourMultiplier = 2.0;
        private int _maxQueueSize = 10;
        private double _simulationSpeed = 1.0;

        /// <summary>
        /// Number of charging stations in the simulation
        /// </summary>
        public int NumberOfStations
        {
            get => _numberOfStations;
            set => SetProperty(ref _numberOfStations, value);
        }

        /// <summary>
        /// Number of charging slots per station
        /// </summary>
        public int SlotsPerStation
        {
            get => _slotsPerStation;
            set => SetProperty(ref _slotsPerStation, value);
        }

        /// <summary>
        /// Total simulation duration in hours
        /// </summary>
        public int SimulationDurationHours
        {
            get => _simulationDurationHours;
            set => SetProperty(ref _simulationDurationHours, value);
        }

        /// <summary>
        /// Average electric cars arriving per hour
        /// </summary>
        public double ArrivalRateCarsPerHour
        {
            get => _arrivalRateCarsPerHour;
            set => SetProperty(ref _arrivalRateCarsPerHour, value);
        }

        /// <summary>
        /// Average electric trucks arriving per hour
        /// </summary>
        public double ArrivalRateTrucksPerHour
        {
            get => _arrivalRateTrucksPerHour;
            set => SetProperty(ref _arrivalRateTrucksPerHour, value);
        }

        /// <summary>
        /// Average electric buses arriving per hour
        /// </summary>
        public double ArrivalRateBusesPerHour
        {
            get => _arrivalRateBusesPerHour;
            set => SetProperty(ref _arrivalRateBusesPerHour, value);
        }

        /// <summary>
        /// Multiplier for arrival rates during rush hours (7-9 AM, 5-7 PM)
        /// </summary>
        public double RushHourMultiplier
        {
            get => _rushHourMultiplier;
            set => SetProperty(ref _rushHourMultiplier, value);
        }

        /// <summary>
        /// Maximum number of vehicles allowed in queue per station
        /// </summary>
        public int MaxQueueSize
        {
            get => _maxQueueSize;
            set => SetProperty(ref _maxQueueSize, value);
        }

        /// <summary>
        /// Simulation speed multiplier (1x = real-time, 100x = fast-forward)
        /// </summary>
        public double SimulationSpeed
        {
            get => _simulationSpeed;
            set => SetProperty(ref _simulationSpeed, value);
        }

        /// <summary>
        /// Total system capacity (stations × slots)
        /// </summary>
        public int TotalSystemCapacity => NumberOfStations * SlotsPerStation;

        /// <summary>
        /// Expected total vehicles per hour (all types)
        /// </summary>
        public double TotalExpectedVehiclesPerHour =>
            ArrivalRateCarsPerHour + ArrivalRateTrucksPerHour + ArrivalRateBusesPerHour;

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