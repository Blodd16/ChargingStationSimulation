    using System;

    namespace ChargingStationSimulation.Models
    {
        /// <summary>
        /// Vehicle types supported by charging stations
        /// </summary>
        public enum VehicleType
        {
            ElectricCar,
            ElectricTruck,
            ElectricBus
        }

        /// <summary>
        /// Current status of vehicle in the system
        /// </summary>
        public enum VehicleStatus
        {
            Waiting,
            Charging,
            Completed,
            Rejected
        }

        /// <summary>
        /// Represents an electric vehicle with charging properties
        /// Based on real-world EV specifications and charging curves
        /// </summary>
        public class Vehicle
        {
            public string Id { get; set; } = string.Empty;
            public VehicleType Type { get; set; }

            /// <summary>
            /// Current battery level (0-100%)
            /// </summary>
            public double BatteryLevel { get; set; }

            /// <summary>
            /// Target battery level after charging (typically 80-90% for fast charging)
            /// </summary>
            public double TargetBatteryLevel { get; set; }

            /// <summary>
            /// Battery capacity in kWh
            /// </summary>
            public double BatteryCapacityKwh { get; set; }

            /// <summary>
            /// Charging power in kW (considers charging curve)
            /// </summary>
            public double ChargingPowerKw { get; set; }

            public DateTime ArrivalTime { get; set; }
            public DateTime? ChargingStartTime { get; set; }
            public DateTime? ChargingEndTime { get; set; }

            /// <summary>
            /// Expected charging duration in minutes
            /// </summary>
            public double ChargingDurationMinutes { get; set; }

            public int StationId { get; set; }
            public VehicleStatus Status { get; set; } = VehicleStatus.Waiting;

            /// <summary>
            /// Time spent waiting in queue
            /// </summary>
            public TimeSpan WaitingTime =>
                ChargingStartTime?.Subtract(ArrivalTime) ?? TimeSpan.Zero;

            /// <summary>
            /// Time remaining until charging completes
            /// </summary>
            public TimeSpan RemainingChargingTime()
            {
                if (!ChargingEndTime.HasValue || Status != VehicleStatus.Charging)
                    return TimeSpan.Zero;

                var remaining = ChargingEndTime.Value - DateTime.Now;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }

            /// <summary>
            /// Charging progress (0.0 - 1.0)
            /// Uses non-linear charging curve simulation
            /// </summary>
            public double GetChargingProgress()
            {
                if (!ChargingStartTime.HasValue || !ChargingEndTime.HasValue || Status != VehicleStatus.Charging)
                    return 0.0;

                var totalDuration = ChargingEndTime.Value - ChargingStartTime.Value;
                var elapsed = DateTime.Now - ChargingStartTime.Value;

                if (elapsed <= TimeSpan.Zero) return 0.0;
                if (elapsed >= totalDuration) return 1.0;

                var linearProgress = elapsed.TotalMinutes / totalDuration.TotalMinutes;

                // Simulate charging curve (faster initial charge, slower near full)
                return SimulateChargingCurve(linearProgress);
            }

            /// <summary>
            /// Current battery level during charging
            /// </summary>
            public double GetCurrentBatteryLevel()
            {
                if (Status != VehicleStatus.Charging)
                    return BatteryLevel;

                var progress = GetChargingProgress();
                var batteryGain = (TargetBatteryLevel - BatteryLevel) * progress;
                return BatteryLevel + batteryGain;
            }

            /// <summary>
            /// Simulate realistic EV charging curve
            /// Fast charging slows down after 80% to protect battery
            /// </summary>
            private double SimulateChargingCurve(double linearProgress)
            {
                // Charging curve formula: y = x - 0.2*x^3
                // Provides faster initial charging, slower towards the end
                return linearProgress - 0.2 * Math.Pow(linearProgress, 3);
            }

            /// <summary>
            /// Energy delivered so far in kWh
            /// </summary>
            public double EnergyDeliveredKwh()
            {
                var progress = GetChargingProgress();
                var targetEnergy = BatteryCapacityKwh * (TargetBatteryLevel - BatteryLevel) / 100.0;
                return targetEnergy * progress;
            }

            /// <summary>
            /// Get vehicle display name with icon
            /// </summary>
            public string DisplayName => $"{GetVehicleIcon()} {Type} #{Id}";

            /// <summary>
            /// Get icon for vehicle type
            /// </summary>
            public string GetVehicleIcon()
            {
                return Type switch
                {
                    VehicleType.ElectricCar => "🚗",
                    VehicleType.ElectricTruck => "🚚",
                    VehicleType.ElectricBus => "🚌",
                    _ => "🚗"
                };
            }

            /// <summary>
            /// Get color code for vehicle type
            /// </summary>
            public string GetVehicleColor()
            {
                return Type switch
                {
                    VehicleType.ElectricCar => "#4ECDC4",
                    VehicleType.ElectricTruck => "#F39C12",
                    VehicleType.ElectricBus => "#9B59B6",
                    _ => "#95A5A6"
                };
            }

            /// <summary>
            /// Create vehicle with realistic specifications
            /// </summary>
            public static Vehicle CreateVehicle(int id, VehicleType type, Random random)
            {
                var vehicle = new Vehicle
                {
                    Id = $"{id:D4}",
                    Type = type,
                    ArrivalTime = DateTime.Now
                };

                // Set realistic parameters based on vehicle type
                switch (type)
                {
                    case VehicleType.ElectricCar:
                        vehicle.BatteryCapacityKwh = 60 + random.Next(20); // 60-80 kWh
                        vehicle.ChargingPowerKw = 50 + random.Next(100); // 50-150 kW
                        vehicle.BatteryLevel = 10 + random.NextDouble() * 30; // 10-40%
                        vehicle.TargetBatteryLevel = 80 + random.NextDouble() * 10; // 80-90%
                        break;

                    case VehicleType.ElectricTruck:
                        vehicle.BatteryCapacityKwh = 200 + random.Next(100); // 200-300 kWh
                        vehicle.ChargingPowerKw = 150 + random.Next(200); // 150-350 kW
                        vehicle.BatteryLevel = 15 + random.NextDouble() * 25; // 15-40%
                        vehicle.TargetBatteryLevel = 75 + random.NextDouble() * 10; // 75-85%
                        break;

                    case VehicleType.ElectricBus:
                        vehicle.BatteryCapacityKwh = 250 + random.Next(150); // 250-400 kWh
                        vehicle.ChargingPowerKw = 100 + random.Next(250); // 100-350 kW
                        vehicle.BatteryLevel = 20 + random.NextDouble() * 20; // 20-40%
                        vehicle.TargetBatteryLevel = 85 + random.NextDouble() * 10; // 85-95%
                        break;
                }

                // Calculate charging duration based on energy needed and power
                var energyNeeded = vehicle.BatteryCapacityKwh *
                    (vehicle.TargetBatteryLevel - vehicle.BatteryLevel) / 100.0;
                vehicle.ChargingDurationMinutes = (energyNeeded / vehicle.ChargingPowerKw) * 60;

                return vehicle;
            }
        }
    }