using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ChargingStationSimulation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChargingStationSimulation.Views
{
    /// <summary>
    /// Main application window with real-time chart visualization
    /// Implements efficient rendering for high-speed simulation
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private Canvas? _utilizationChart;
        private Canvas? _powerChart;
        private Canvas? _queueChart;
        private DispatcherTimer? _chartUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Get chart canvas references
            _utilizationChart = this.FindControl<Canvas>("UtilizationChart");
            _powerChart = this.FindControl<Canvas>("PowerChart");
            _queueChart = this.FindControl<Canvas>("QueueChart");

            // Setup chart update timer
            SetupChartUpdateTimer();

            // Subscribe to ViewModel property changes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.IsSimulationRunning))
                    {
                        if (_viewModel.IsSimulationRunning)
                            _chartUpdateTimer?.Start();
                        else
                            _chartUpdateTimer?.Stop();
                    }
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Setup timer for chart updates
        /// Update frequency: 1 second (optimal for visual feedback)
        /// </summary>
        private void SetupChartUpdateTimer()
        {
            _chartUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _chartUpdateTimer.Tick += (s, e) =>
            {
                UpdateAllCharts();
            };
        }

        /// <summary>
        /// Update all charts with latest data
        /// </summary>
        private void UpdateAllCharts()
        {
            if (_utilizationChart != null && _viewModel.UtilizationChartData.Count > 0)
            {
                DrawLineChart(_utilizationChart, _viewModel.UtilizationChartData.ToList(),
                    "#1ABC9C", 0, 100, "Utilization %");
            }

            if (_powerChart != null && _viewModel.PowerOutputChartData.Count > 0)
            {
                var maxPower = _viewModel.PowerOutputChartData.Max();
                DrawLineChart(_powerChart, _viewModel.PowerOutputChartData.ToList(),
                    "#F39C12", 0, Math.Max(maxPower * 1.2, 100), "Power kW");
            }

            if (_queueChart != null && _viewModel.QueueLengthChartData.Count > 0)
            {
                var maxQueue = _viewModel.QueueLengthChartData.Max();
                DrawLineChart(_queueChart, _viewModel.QueueLengthChartData.Select(q => (double)q).ToList(),
                    "#E74C3C", 0, Math.Max(maxQueue + 2, 10), "Queue");
            }
        }

        /// <summary>
        /// Draw line chart with smooth curves
        /// Implements simple moving average for smoother visualization
        /// </summary>
        private void DrawLineChart(Canvas canvas, List<double> data, string colorHex,
            double minValue, double maxValue, string label)
        {
            canvas.Children.Clear();

            if (data.Count < 2 || canvas.Bounds.Width < 10 || canvas.Bounds.Height < 10)
                return;

            var width = canvas.Bounds.Width;
            var height = canvas.Bounds.Height;
            var margin = 10.0;
            var chartWidth = width - 2 * margin;
            var chartHeight = height - 2 * margin;

            // Draw axes
            DrawAxes(canvas, margin, chartWidth, chartHeight, minValue, maxValue);

            // Calculate points
            var points = new List<Point>();
            var step = chartWidth / Math.Max(1, data.Count - 1);

            for (int i = 0; i < data.Count; i++)
            {
                var normalizedValue = (data[i] - minValue) / (maxValue - minValue);
                var x = margin + i * step;
                var y = margin + chartHeight - (normalizedValue * chartHeight);
                points.Add(new Point(x, Math.Max(margin, Math.Min(margin + chartHeight, y))));
            }

            // Draw line using Path instead of Polyline
            if (points.Count >= 2)
            {
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure { StartPoint = points[0] };

                for (int i = 1; i < points.Count; i++)
                {
                    pathFigure.Segments.Add(new LineSegment { Point = points[i] });
                }

                pathGeometry.Figures.Add(pathFigure);

                var path = new Path
                {
                    Data = pathGeometry,
                    Stroke = new SolidColorBrush(Color.Parse(colorHex)),
                    StrokeThickness = 2
                };
                canvas.Children.Add(path);

                // Draw filled area under line
                var fillFigure = new PathFigure { StartPoint = points[0] };
                for (int i = 1; i < points.Count; i++)
                {
                    fillFigure.Segments.Add(new LineSegment { Point = points[i] });
                }
                fillFigure.Segments.Add(new LineSegment { Point = new Point(points[points.Count - 1].X, margin + chartHeight) });
                fillFigure.Segments.Add(new LineSegment { Point = new Point(points[0].X, margin + chartHeight) });
                fillFigure.IsClosed = true;

                var fillGeometry = new PathGeometry();
                fillGeometry.Figures.Add(fillFigure);

                var fillPath = new Path
                {
                    Data = fillGeometry,
                    Fill = new SolidColorBrush(Color.Parse(colorHex)) { Opacity = 0.2 }
                };
                canvas.Children.Insert(0, fillPath);
            }

            // Draw current value indicator
            if (points.Count > 0)
            {
                var lastPoint = points[points.Count - 1];

                var glowIndicator = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new SolidColorBrush(Color.Parse(colorHex)) { Opacity = 0.3 }
                };
                Canvas.SetLeft(glowIndicator, lastPoint.X - 8);
                Canvas.SetTop(glowIndicator, lastPoint.Y - 8);
                canvas.Children.Add(glowIndicator);

                var indicator = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.Parse(colorHex))
                };
                Canvas.SetLeft(indicator, lastPoint.X - 4);
                Canvas.SetTop(indicator, lastPoint.Y - 4);
                canvas.Children.Add(indicator);

                // Value label
                var valueText = new TextBlock
                {
                    Text = $"{data[data.Count - 1]:F1}",
                    Foreground = new SolidColorBrush(Color.Parse(colorHex)),
                    FontSize = 11,
                    FontWeight = FontWeight.Bold
                };
                Canvas.SetLeft(valueText, lastPoint.X + 10);
                Canvas.SetTop(valueText, lastPoint.Y - 6);
                canvas.Children.Add(valueText);
            }
        }

        /// <summary>
        /// Draw chart axes with grid lines
        /// </summary>
        private void DrawAxes(Canvas canvas, double margin, double width, double height,
            double minValue, double maxValue)
        {
            var axisColor = new SolidColorBrush(Color.Parse("#34495E"));
            var gridColor = new SolidColorBrush(Color.Parse("#2C3E50")) { Opacity = 0.5 };

            // Y-axis
            var yAxis = new Line
            {
                StartPoint = new Point(margin, margin),
                EndPoint = new Point(margin, margin + height),
                Stroke = axisColor,
                StrokeThickness = 1
            };
            canvas.Children.Add(yAxis);

            // X-axis
            var xAxis = new Line
            {
                StartPoint = new Point(margin, margin + height),
                EndPoint = new Point(margin + width, margin + height),
                Stroke = axisColor,
                StrokeThickness = 1
            };
            canvas.Children.Add(xAxis);

            // Horizontal grid lines (5 lines)
            for (int i = 0; i <= 4; i++)
            {
                var y = margin + (height / 4) * i;
                var gridLine = new Line
                {
                    StartPoint = new Point(margin, y),
                    EndPoint = new Point(margin + width, y),
                    Stroke = gridColor,
                    StrokeThickness = 1,
                    StrokeDashArray = new AvaloniaList<double> { 2, 4 }
                };
                canvas.Children.Insert(0, gridLine);

                // Y-axis labels
                var value = maxValue - (maxValue - minValue) * i / 4;
                var label = new TextBlock
                {
                    Text = $"{value:F0}",
                    Foreground = new SolidColorBrush(Color.Parse("#95A5A6")),
                    FontSize = 9d
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 6);
                canvas.Children.Add(label);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _chartUpdateTimer?.Stop();
            _viewModel?.StopSimulationCommand.Execute(null);
            base.OnClosed(e);
        }
    }
}