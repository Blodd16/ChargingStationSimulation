using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChargingStationSimulation.Models;

namespace ChargingStationSimulation.Services
{
    /// <summary>
    /// Core simulation engine using discrete event simulation
    /// Implements Poisson arrival process for realistic vehicle generation
    /// Optimized for high-speed simulation (1x to 1000x)
    /// </summary>
    public class SimulationEngine
    {
        private readonly Random _random = new();
        private readonly List<ChargingStation> _stations = new();
        private readonly SimulationStatistics _statistics = new();
        private DateTime _currentTime;
        private DateTime _simulationStartTime;
        private DateTime _simulationEndTime;
        private int _vehicleIdCounter = 1;
        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;

        public SimulationStatistics Statistics => _statistics;
        public List<ChargingStation> Stations => _stations.ToList();
        public DateTime CurrentTime => _currentTime;
        public bool IsRunning => _isRunning;

        // Events for UI updates (batched for performance)
        public event Action<List<ChargingStation>>? StationsUpdated;
        public event Action<SimulationStatistics>? StatisticsUpdated;
        public event Action<DateTime>? TimeUpdated;
        public event Action<Vehicle, string>? VehicleEvent;

        /// <summary>
        /// Initialize simulation with parameters
        /// </summary>
        public void Initialize(SimulationParameters parameters)
        {
            _statistics.Reset();
            _stations.Clear();
            _vehicleIdCounter = 1;

            // Create charging stations
            for (int i = 1; i <= parameters.NumberOfStations; i++)
            {
                _stations.Add(new ChargingStation
                {
                    Id = i,
                    Capacity = parameters.SlotsPerStation,
                    MaxQueueSize = parameters.MaxQueueSize
                });
            }

            _simulationStartTime = DateTime.Now;
            _currentTime = _simulationStartTime;
            _simulationEndTime = _simulationStartTime.AddHours(parameters.SimulationDurationHours);
        }

        /// <summary>
        /// Run simulation asynchronously with cancellation support
        /// Time Complexity: O(n * m) where n = time steps, m = stations
        /// </summary>
        public async Task RunSimulation(SimulationParameters parameters)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            var timeStep = TimeSpan.FromMinutes(1); // 1-minute simulation steps
            int updateBatchSize = CalculateBatchSize(parameters.SimulationSpeed);
            int stepCounter = 0;

            try
            {
                while (_currentTime < _simulationEndTime && _isRunning)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Generate new vehicles using Poisson distribution
                    GenerateNewVehicles(parameters);

                    // Process all charging stations
                    ProcessStations();

                    // Update statistics
                    UpdateStatistics();

                    // Batch UI updates for performance at high speeds
                    stepCounter++;
                    if (stepCounter >= updateBatchSize)
                    {
                        StationsUpdated?.Invoke(_stations.ToList());
                        StatisticsUpdated?.Invoke(_statistics);
                        TimeUpdated?.Invoke(_currentTime);
                        stepCounter = 0;
                    }

                    // Advance simulation time
                    _currentTime = _currentTime.Add(timeStep);
                    _statistics.SimulationTime = _currentTime - _simulationStartTime;

                    // Dynamic delay based on simulation speed
                    var delayMs = CalculateDelay(parameters.SimulationSpeed);
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, _cancellationTokenSource.Token);
                    }
                }

                // Final update
                StationsUpdated?.Invoke(_stations.ToList());
                StatisticsUpdated?.Invoke(_statistics);
            }
            catch (OperationCanceledException)
            {
                // Simulation stopped by user
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>
        /// Calculate update batch size based on simulation speed
        /// Higher speeds = less frequent UI updates for better performance
        /// </summary>
        private int CalculateBatchSize(double speed)
        {
            if (speed <= 1) return 1;           // Update every step at 1x
            if (speed <= 10) return 5;          // Update every 5 steps at 10x
            if (speed <= 50) return 10;         // Update every 10 steps at 50x
            return 20;                          // Update every 20 steps at 100x+
        }

        /// <summary>
        /// Calculate delay between simulation steps
        /// Formula: delay = base_delay / speed
        /// </summary>
        private int CalculateDelay(double speed)
        {
            const int baseDelay = 100; // 100ms base delay
            var delay = (int)(baseDelay / speed);
            return Math.Max(1, Math.Min(delay, baseDelay));
        }

        /// <summary>
        /// Generate new vehicles using Poisson arrival process
        /// More realistic than uniform distribution
        /// </summary>
        private void GenerateNewVehicles(SimulationParameters parameters)
        {
            var currentHour = _currentTime.Hour;
            var isRushHour = IsRushHour(currentHour);
            var multiplier = isRushHour ? parameters.RushHourMultiplier : 1.0;

            // Calculate arrival probabilities per minute using Poisson distribution
            var carProbability = (parameters.ArrivalRateCarsPerHour * multiplier) / 60.0;
            var truckProbability = (parameters.ArrivalRateTrucksPerHour * multiplier) / 60.0;
            var busProbability = (parameters.ArrivalRateBusesPerHour * multiplier) / 60.0;

            // Generate vehicles for each type
            TryGenerateVehicle(VehicleType.ElectricCar, carProbability);
            TryGenerateVehicle(VehicleType.ElectricTruck, truckProbability);
            TryGenerateVehicle(VehicleType.ElectricBus, busProbability);
        }

        /// <summary>
        /// Check if current hour is rush hour (peak demand)
        /// Morning: 7-9 AM, Evening: 5-7 PM
        /// </summary>
        private bool IsRushHour(int hour)
        {
            return (hour >= 7 && hour <= 9) || (hour >= 17 && hour <= 19);
        }

        /// <summary>
        /// Attempt to generate vehicle based on probability
        /// Uses Poisson process for realistic stochastic arrivals
        /// </summary>
        private void TryGenerateVehicle(VehicleType type, double probability)
        {
            // Poisson arrival: P(arrival) = 1 - e^(-λ)
            // Approximated as λ for small λ (probability < 0.1)
            if (_random.NextDouble() < probability)
            {
                var vehicle = Vehicle.CreateVehicle(_vehicleIdCounter++, type, _random);
                vehicle.ArrivalTime = _currentTime;
                _statistics.TotalVehiclesGenerated++;

                if (AssignVehicleToStation(vehicle))
                {
                    VehicleEvent?.Invoke(vehicle,
                        $"{vehicle.DisplayName} arrived at Station {vehicle.StationId}");
                }
                else
                {
                    vehicle.Status = VehicleStatus.Rejected;
                    _statistics.TotalVehiclesRejected++;
                    VehicleEvent?.Invoke(vehicle,
                        $"{vehicle.DisplayName} rejected - all stations full");
                }
            }
        }

        /// <summary>
        /// Assign vehicle to best available station
        /// Strategy: Shortest queue with available capacity
        /// Time Complexity: O(n) where n = number of stations
        /// </summary>
        private bool AssignVehicleToStation(Vehicle vehicle)
        {
            // Find station with shortest total load (charging + waiting)
            var bestStation = _stations
                .Where(s => s.CanAcceptVehicle)
                .OrderBy(s => s.ChargingVehicles.Count + s.WaitingQueue.Count)
                .ThenBy(s => s.EstimatedWaitTimeMinutes())
                .ThenBy(s => s.Id)
                .FirstOrDefault();

            if (bestStation != null)
            {
                bestStation.AddVehicle(vehicle);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Process all charging stations and handle completed vehicles
        /// Time Complexity: O(n * m) where n = stations, m = vehicles per station
        /// </summary>
        private void ProcessStations()
        {
            foreach (var station in _stations)
            {
                var completedVehicles = station.ProcessCompletedCharging(_currentTime);

                // Update statistics for completed vehicles
                foreach (var vehicle in completedVehicles)
                {
                    _statistics.AddCompletedVehicle(vehicle);
                    VehicleEvent?.Invoke(vehicle,
                        $"{vehicle.DisplayName} completed charging at Station {station.Id}");
                }
            }
        }

        /// <summary>
        /// Update real-time statistics and metrics
        /// </summary>
        private void UpdateStatistics()
        {
            if (_stations.Count > 0)
            {
                // Calculate average utilization across all stations
                var totalUtilization = _stations.Average(s => s.Utilization);
                _statistics.UpdateUtilization(totalUtilization);

                // Track maximum queue length
                var maxQueue = _stations.Max(s => s.QueueLength);
                _statistics.UpdateMaxQueueLength(maxQueue);

                // Calculate total power output
                var totalPower = _stations.Sum(s => s.CurrentPowerOutput);
                _statistics.UpdatePowerOutput(totalPower);
            }
        }

        /// <summary>
        /// Stop simulation gracefully
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Get station by ID
        /// </summary>
        public ChargingStation? GetStationById(int id)
        {
            return _stations.FirstOrDefault(s => s.Id == id);
        }

        /// <summary>
        /// Get all vehicles currently in the system
        /// </summary>
        public List<Vehicle> GetAllActiveVehicles()
        {
            var activeVehicles = new List<Vehicle>();

            foreach (var station in _stations)
            {
                activeVehicles.AddRange(station.ChargingVehicles);
                activeVehicles.AddRange(station.WaitingQueue);
            }

            return activeVehicles;
        }

        /// <summary>
        /// Get system-wide metrics
        /// </summary>
        public (int TotalCharging, int TotalWaiting, double TotalPower) GetSystemMetrics()
        {
            var totalCharging = _stations.Sum(s => s.ChargingVehicles.Count);
            var totalWaiting = _stations.Sum(s => s.QueueLength);
            var totalPower = _stations.Sum(s => s.CurrentPowerOutput);

            return (totalCharging, totalWaiting, totalPower);
        }
    }
}