using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ChargingStationSimulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChargingStationSimulation.Views
{
    /// <summary>
    /// Animated charging station with REAL moving vehicles
    /// Features: Cars drive in, park, charge with animations, drive out
    /// </summary>
    public partial class AnimatedStationView : UserControl
    {
        private readonly Dictionary<string, VehicleVisual> _vehicleVisuals = new();
        private ChargingStation? _station;
        private DispatcherTimer? _updateTimer;
        private Canvas? _animationCanvas;
        private Random _random = new Random();

        // UI Controls
        private TextBlock? _stationTitle;
        private TextBlock? _powerDisplay;
        private TextBlock? _utilizationDisplay;
        private TextBlock? _slotsDisplay;
        private TextBlock? _chargingCount;
        private TextBlock? _queueCount;
        private TextBlock? _availableCount;
        private ItemsControl? _chargingPortsControl;
        private StackPanel? _queuePanel;

        // Animation constants
        private const double CANVAS_WIDTH = 350;
        private const double CANVAS_HEIGHT = 250;
        private const double VEHICLE_WIDTH = 60;
        private const double VEHICLE_HEIGHT = 30;
        private const double PORT_SIZE = 30;

        public static readonly StyledProperty<ChargingStation?> StationProperty =
            AvaloniaProperty.Register<AnimatedStationView, ChargingStation?>(nameof(Station));

        public ChargingStation? Station
        {
            get => GetValue(StationProperty);
            set => SetValue(StationProperty, value);
        }

        public AnimatedStationView()
        {
            InitializeComponent();
            this.PropertyChanged += OnPropertyChanged;
            SetupUpdateTimer();
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _stationTitle = this.FindControl<TextBlock>("StationTitle");
            _powerDisplay = this.FindControl<TextBlock>("PowerDisplay");
            _utilizationDisplay = this.FindControl<TextBlock>("UtilizationDisplay");
            _slotsDisplay = this.FindControl<TextBlock>("SlotsDisplay");
            _chargingCount = this.FindControl<TextBlock>("ChargingCount");
            _queueCount = this.FindControl<TextBlock>("QueueCount");
            _availableCount = this.FindControl<TextBlock>("AvailableCount");
            _animationCanvas = this.FindControl<Canvas>("AnimationCanvas");
            _chargingPortsControl = this.FindControl<ItemsControl>("ChargingPortsControl");
            _queuePanel = this.FindControl<StackPanel>("QueuePanel");
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DrawParkingLines();
            DrawChargingPorts();
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Fast updates for smooth animation
            };
            _updateTimer.Tick += (s, e) => UpdateDisplay();
            _updateTimer.Start();
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StationProperty)
            {
                _station = e.NewValue as ChargingStation;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Draw parking lines on canvas
        /// </summary>
        private void DrawParkingLines()
        {
            if (_animationCanvas == null) return;

            // Draw parking spots
            for (int i = 0; i < 4; i++)
            {
                double y = 40 + i * 55;

                // Parking line
                var line = new Rectangle
                {
                    Width = 80,
                    Height = 45,
                    Stroke = new SolidColorBrush(Color.Parse("#FFD93D")),
                    StrokeThickness = 2,
                    StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 },
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(line, 100);
                Canvas.SetTop(line, y);
                _animationCanvas.Children.Add(line);

                // Spot number
                var spotNumber = new TextBlock
                {
                    Text = $"{i + 1}",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#34495E")),
                    Opacity = 0.3
                };
                Canvas.SetLeft(spotNumber, 130);
                Canvas.SetTop(spotNumber, y + 10);
                _animationCanvas.Children.Add(spotNumber);
            }

            // Entry arrow
            DrawArrow(20, CANVAS_HEIGHT / 2, 50, CANVAS_HEIGHT / 2, "#1ABC9C");

            // Exit arrow
            DrawArrow(CANVAS_WIDTH - 50, CANVAS_HEIGHT / 2, CANVAS_WIDTH - 20, CANVAS_HEIGHT / 2, "#E74C3C");
        }

        private void DrawArrow(double x1, double y1, double x2, double y2, string color)
        {
            if (_animationCanvas == null) return;

            var line = new Line
            {
                StartPoint = new Point(x1, y1),
                EndPoint = new Point(x2, y2),
                Stroke = new SolidColorBrush(Color.Parse(color)),
                StrokeThickness = 3
            };
            _animationCanvas.Children.Add(line);

            // Arrow head
            var arrow = new Polygon
            {
                Points = new Avalonia.Collections.AvaloniaList<Point>
                {
                    new Point(x2, y2),
                    new Point(x2 - 8, y2 - 5),
                    new Point(x2 - 8, y2 + 5)
                },
                Fill = new SolidColorBrush(Color.Parse(color))
            };
            _animationCanvas.Children.Add(arrow);
        }

        private void DrawChargingPorts()
        {
            if (_chargingPortsControl == null) return;

            for (int i = 0; i < 4; i++)
            {
                var port = CreateChargingPort(i);
                _chargingPortsControl.Items.Add(port);
            }
        }

        private Border CreateChargingPort(int index)
        {
            var portVisual = new Border
            {
                Width = PORT_SIZE,
                Height = PORT_SIZE,
                CornerRadius = new CornerRadius(15),
                Background = new SolidColorBrush(Color.Parse("#34495E")),
                BorderBrush = new SolidColorBrush(Color.Parse("#1ABC9C")),
                BorderThickness = new Thickness(2),
                Child = new TextBlock
                {
                    Text = "🔌",
                    FontSize = 16,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };

            return portVisual;
        }

        private void UpdateDisplay()
        {
            if (_station == null) return;

            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateHeader();
                    UpdateDisplayScreen();
                    UpdateVehicleAnimations();
                    UpdateStatusIndicators();
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating display: {ex.Message}");
            }
        }

        private void UpdateHeader()
        {
            if (_station == null || _stationTitle == null) return;
            _stationTitle.Text = $"STATION {_station.Id:D2}";
        }

        private void UpdateDisplayScreen()
        {
            if (_station == null) return;

            if (_powerDisplay != null)
                _powerDisplay.Text = $"{_station.CurrentPowerOutput:F1} kW";

            if (_utilizationDisplay != null)
                _utilizationDisplay.Text = $"{_station.Utilization:F0}%";

            if (_slotsDisplay != null)
                _slotsDisplay.Text = $"{_station.ChargingVehicles.Count}/{_station.Capacity}";
        }

        /// <summary>
        /// MAIN ANIMATION LOGIC - Vehicles drive in, park, charge, drive out
        /// </summary>
        private void UpdateVehicleAnimations()
        {
            if (_station == null || _animationCanvas == null) return;

            try
            {
                var currentVehicleIds = _station.ChargingVehicles.Select(v => v.Id).ToHashSet();
                var existingVehicleIds = _vehicleVisuals.Keys.ToHashSet();

                // Remove vehicles that finished charging (drive away animation)
                var vehiclesToRemove = existingVehicleIds.Except(currentVehicleIds).ToList();
                foreach (var id in vehiclesToRemove)
                {
                    if (_vehicleVisuals.TryGetValue(id, out var vehicleVisual))
                    {
                        AnimateVehicleDriveAway(vehicleVisual);
                        _vehicleVisuals.Remove(id);
                    }
                }

                // Add new vehicles (drive in animation)
                var vehiclesToAdd = currentVehicleIds.Except(existingVehicleIds).ToList();
                for (int i = 0; i < vehiclesToAdd.Count && i < _station.ChargingVehicles.Count; i++)
                {
                    var vehicleId = vehiclesToAdd[i];
                    var vehicle = _station.ChargingVehicles.FirstOrDefault(v => v.Id == vehicleId);
                    if (vehicle != null)
                    {
                        int spotIndex = _station.ChargingVehicles.IndexOf(vehicle);
                        if (spotIndex >= 0 && spotIndex < 4)
                        {
                            var vehicleVisual = CreateVehicleVisual(vehicle, spotIndex);
                            _vehicleVisuals[vehicleId] = vehicleVisual;
                            _animationCanvas.Children.Add(vehicleVisual.Border);
                            AnimateVehicleDriveIn(vehicleVisual, spotIndex);
                        }
                    }
                }

                // Update charging progress for existing vehicles
                foreach (var vehicle in _station.ChargingVehicles)
                {
                    if (_vehicleVisuals.TryGetValue(vehicle.Id, out var vehicleVisual))
                    {
                        UpdateChargingProgress(vehicleVisual, vehicle);
                    }
                }

                // Update queue display
                UpdateQueueDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in vehicle animations: {ex.Message}");
            }
        }

        private VehicleVisual CreateVehicleVisual(Vehicle vehicle, int spotIndex)
        {
            var vehicleBody = new Border
            {
                Width = VEHICLE_WIDTH,
                Height = VEHICLE_HEIGHT,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.Parse(vehicle.GetVehicleColor())),
                BorderBrush = new SolidColorBrush(Color.Parse("#2C3E50")),
                BorderThickness = new Thickness(2)
            };

            // Add vehicle details
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };

            var typeIcon = new TextBlock
            {
                Text = vehicle.GetVehicleIcon(),
                FontSize = 18,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            Grid.SetColumn(typeIcon, 0);

            // Battery indicator
            var batteryBar = new Border
            {
                Width = 4,
                Height = VEHICLE_HEIGHT - 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.Parse("#34495E")),
                Margin = new Thickness(2),
                Child = new Border
                {
                    Name = "BatteryLevel",
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(Color.Parse("#1ABC9C")),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                    Height = (VEHICLE_HEIGHT - 4) * (vehicle.BatteryLevel / 100.0)
                }
            };
            Grid.SetColumn(batteryBar, 1);

            grid.Children.Add(typeIcon);
            grid.Children.Add(batteryBar);
            vehicleBody.Child = grid;

            // Start position: off screen left
            Canvas.SetLeft(vehicleBody, -VEHICLE_WIDTH);
            Canvas.SetTop(vehicleBody, 40 + spotIndex * 55 + 7);

            return new VehicleVisual
            {
                Border = vehicleBody,
                BatteryBar = batteryBar,
                SpotIndex = spotIndex,
                Vehicle = vehicle
            };
        }

        /// <summary>
        /// Animate vehicle driving into parking spot
        /// </summary>
        private async void AnimateVehicleDriveIn(VehicleVisual vehicleVisual, int spotIndex)
        {
            var targetX = 100.0;
            var startX = -VEHICLE_WIDTH;
            var duration = TimeSpan.FromSeconds(1.5);
            var frames = 30;
            var delayPerFrame = duration.TotalMilliseconds / frames;

            for (int i = 0; i <= frames; i++)
            {
                var progress = (double)i / frames;
                // Cubic ease in-out
                var easedProgress = progress < 0.5
                    ? 4 * progress * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 3) / 2;

                var currentX = startX + (targetX - startX) * easedProgress;
                Canvas.SetLeft(vehicleVisual.Border, currentX);

                await Task.Delay((int)delayPerFrame);
            }
        }

        /// <summary>
        /// Animate vehicle driving away
        /// </summary>
        private async void AnimateVehicleDriveAway(VehicleVisual vehicleVisual)
        {
            var startX = Canvas.GetLeft(vehicleVisual.Border);
            var targetX = CANVAS_WIDTH + VEHICLE_WIDTH;
            var duration = TimeSpan.FromSeconds(1.5);
            var frames = 30;
            var delayPerFrame = duration.TotalMilliseconds / frames;

            for (int i = 0; i <= frames; i++)
            {
                var progress = (double)i / frames;
                // Cubic ease in-out
                var easedProgress = progress < 0.5
                    ? 4 * progress * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 3) / 2;

                var currentX = startX + (targetX - startX) * easedProgress;
                Canvas.SetLeft(vehicleVisual.Border, currentX);

                await Task.Delay((int)delayPerFrame);
            }

            // Remove from canvas after animation
            _animationCanvas?.Children.Remove(vehicleVisual.Border);
        }

        /// <summary>
        /// Update charging progress - animate battery filling up
        /// </summary>
        private async void UpdateChargingProgress(VehicleVisual vehicleVisual, Vehicle vehicle)
        {
            if (vehicleVisual.BatteryBar?.Child is Border batteryLevel)
            {
                var progress = vehicle.GetCurrentBatteryLevel() / 100.0;
                var targetHeight = (VEHICLE_HEIGHT - 4) * progress;
                var currentHeight = batteryLevel.Height;

                // Smooth transition
                var frames = 10;
                var delayPerFrame = 30; // 300ms total

                for (int i = 0; i <= frames; i++)
                {
                    var frameProgress = (double)i / frames;
                    var newHeight = currentHeight + (targetHeight - currentHeight) * frameProgress;
                    batteryLevel.Height = newHeight;

                    await Task.Delay(delayPerFrame);
                }

                // Change color based on charge level
                var color = progress < 0.3 ? "#E74C3C" : progress < 0.7 ? "#F39C12" : "#1ABC9C";
                batteryLevel.Background = new SolidColorBrush(Color.Parse(color));
            }
        }

        private void UpdateQueueDisplay()
        {
            if (_station == null || _queuePanel == null) return;

            _queuePanel.Children.Clear();

            int queueCount = _station.QueueLength;
            for (int i = 0; i < Math.Min(queueCount, 3); i++)
            {
                var queueVehicle = new Border
                {
                    Width = 40,
                    Height = 20,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(Color.Parse("#F39C12")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#E67E22")),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = "🚗",
                        FontSize = 12,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    }
                };
                _queuePanel.Children.Add(queueVehicle);
            }

            if (queueCount > 3)
            {
                _queuePanel.Children.Add(new TextBlock
                {
                    Text = $"+{queueCount - 3}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#F39C12")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
            }
        }

        private void UpdateStatusIndicators()
        {
            if (_station == null) return;

            if (_chargingCount != null)
                _chargingCount.Text = _station.ChargingVehicles.Count.ToString();

            if (_queueCount != null)
                _queueCount.Text = _station.QueueLength.ToString();

            if (_availableCount != null)
                _availableCount.Text = _station.FreeSlots.ToString();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _updateTimer?.Stop();
        }

        // Helper class to track vehicle visuals
        private class VehicleVisual
        {
            public Border Border { get; set; } = null!;
            public Border? BatteryBar { get; set; }
            public int SpotIndex { get; set; }
            public Vehicle Vehicle { get; set; } = null!;
        }
    }
}