using System;
using System.Collections.Generic;
using System.Linq;

namespace ChargingStationSimulation.Models
{
    /// <summary>
    /// Represents a charging station with capacity and queue management
    /// Implements efficient queue processing with O(1) operations
    /// </summary>
    public class ChargingStation
    {
        public int Id { get; set; }

        /// <summary>
        /// Maximum number of simultaneous charging slots
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Currently charging vehicles
        /// </summary>
        public List<Vehicle> ChargingVehicles { get; set; } = new();

        /// <summary>
        /// Vehicles waiting in queue
        /// </summary>
        public Queue<Vehicle> WaitingQueue { get; set; } = new();

        /// <summary>
        /// Maximum allowed queue size
        /// </summary>
        public int MaxQueueSize { get; set; } = 10;

        // Performance properties
        public bool HasFreeSlot => ChargingVehicles.Count < Capacity;
        public int FreeSlots => Math.Max(0, Capacity - ChargingVehicles.Count);
        public int QueueLength => WaitingQueue.Count;

        /// <summary>
        /// Station utilization percentage (0-100)
        /// </summary>
        public double Utilization => Capacity > 0 ? (double)ChargingVehicles.Count / Capacity * 100 : 0;

        public bool CanAcceptVehicle => WaitingQueue.Count < MaxQueueSize;

        /// <summary>
        /// Total energy being delivered (kW)
        /// </summary>
        public double CurrentPowerOutput => ChargingVehicles.Sum(v => v.ChargingPowerKw);

        /// <summary>
        /// Add vehicle to station (either charge immediately or queue)
        /// Time Complexity: O(1)
        /// </summary>
        public void AddVehicle(Vehicle vehicle)
        {
            vehicle.StationId = Id;

            if (HasFreeSlot)
            {
                StartCharging(vehicle);
            }
            else if (CanAcceptVehicle)
            {
                vehicle.Status = VehicleStatus.Waiting;
                WaitingQueue.Enqueue(vehicle);
            }
            else
            {
                vehicle.Status = VehicleStatus.Rejected;
            }
        }

        /// <summary>
        /// Initiate charging process for vehicle
        /// </summary>
        private void StartCharging(Vehicle vehicle)
        {
            vehicle.ChargingStartTime = DateTime.Now;
            vehicle.Status = VehicleStatus.Charging;
            ChargingVehicles.Add(vehicle);

            // Calculate charging end time based on battery capacity and power
            vehicle.ChargingEndTime = vehicle.ChargingStartTime.Value
                .AddMinutes(vehicle.ChargingDurationMinutes);
        }

        /// <summary>
        /// Process completed charging and start next vehicles in queue
        /// Time Complexity: O(n) where n is number of charging vehicles
        /// </summary>
        public List<Vehicle> ProcessCompletedCharging(DateTime currentTime)
        {
            var completedVehicles = ChargingVehicles
                .Where(v => v.ChargingEndTime <= currentTime)
                .ToList();

            foreach (var vehicle in completedVehicles)
            {
                ChargingVehicles.Remove(vehicle);
                vehicle.Status = VehicleStatus.Completed;
                vehicle.BatteryLevel = vehicle.TargetBatteryLevel;
            }

            // Start charging waiting vehicles
            while (HasFreeSlot && WaitingQueue.Count > 0)
            {
                var nextVehicle = WaitingQueue.Dequeue();
                nextVehicle.ChargingStartTime = currentTime;
                nextVehicle.ChargingEndTime = currentTime.AddMinutes(nextVehicle.ChargingDurationMinutes);
                nextVehicle.Status = VehicleStatus.Charging;
                ChargingVehicles.Add(nextVehicle);
            }

            return completedVehicles;
        }

        /// <summary>
        /// Get vehicle at specific charging slot
        /// </summary>
        public Vehicle? GetVehicleAtSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < ChargingVehicles.Count
                ? ChargingVehicles[slotIndex]
                : null;
        }

        /// <summary>
        /// Calculate average wait time for current queue
        /// </summary>
        public double EstimatedWaitTimeMinutes()
        {
            if (!HasFreeSlot && ChargingVehicles.Count > 0)
            {
                var avgRemainingTime = ChargingVehicles
                    .Average(v => v.RemainingChargingTime().TotalMinutes);
                return avgRemainingTime;
            }
            return 0;
        }
    }
}