using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Views
{
    public partial class MeasurementGraphView : UserControl
    {
        private MeasurementGraphViewModel _viewModel;

        // drawing constants
        private const double MinRadius = 14;
        private const double MaxRadius = 50;
        private const double TopPadding = 20;
        private const double SidePadding = 50;
        private const double AxisLabelHeight = 36;

        public MeasurementGraphView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MeasurementGraphViewModel;
            if (_viewModel == null) return;

            _viewModel.GraphNeedsRedraw += DrawGraph;

            _viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MeasurementGraphViewModel.SelectedEntity))
                    DrawGraph();
            };

            DrawGraph();
        }

        private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGraph();
        }

        private void DrawGraph()
        {
            if (_viewModel == null) return;

            Dispatcher.Invoke(() =>
            {
                GraphCanvas.Children.Clear();

                var entity = _viewModel.SelectedEntity;
                var history = entity?.History?.ToList();

                double width = GraphCanvas.ActualWidth;
                double height = GraphCanvas.ActualHeight;

                if (history == null || history.Count == 0 || width <= 0 || height <= 0)
                {
                    NoDataPanel.Visibility = Visibility.Visible;
                    return;
                }

                NoDataPanel.Visibility = Visibility.Collapsed;

                // Determine value-to-radius scale based on the data

                double dataMin = history.Min(h => h.Value);
                double dataMax = history.Max(h => h.Value);

                double scaleMin = Math.Min(PressureGauge.MinValidValue, dataMin);
                double scaleMax = Math.Max(PressureGauge.MaxValidValue, dataMax);

                if (Math.Abs(scaleMax - scaleMin) < 0.0001)
                    scaleMax = scaleMin + 1;

                // Layout: points evenly spaced along X axis
                int count = history.Count;
                double usableWidth = Math.Max(width - 2 * SidePadding, 50);
                double step = count > 1 ? usableWidth / (count - 1) : 0;

                // X-axis baseline (for time labels)
                double axisY = height - AxisLabelHeight;

                // dinamic max radius based on available height
                double graphAreaHeight = Math.Max(axisY - TopPadding, 50);
                double dynamicMaxRadius = Math.Min(MaxRadius, graphAreaHeight / 2.0 - 4);
                dynamicMaxRadius = Math.Max(dynamicMaxRadius, MinRadius);

                double centerY = TopPadding + graphAreaHeight / 2.0;

                // Draw X axis line
                GraphCanvas.Children.Add(new Line
                {
                    X1 = SidePadding * 0.4,
                    Y1 = axisY,
                    X2 = width - SidePadding * 0.4,
                    Y2 = axisY,
                    Stroke = new SolidColorBrush(Color.FromRgb(61, 69, 80)),
                    StrokeThickness = 1
                });

                for (int i = 0; i < count; i++)
                {
                    var record = history[i];

                    double centerX = count == 1 ? width / 2.0 : SidePadding + i * step;

                    // Map value -> radius
                    double ratio = (record.Value - scaleMin) / (scaleMax - scaleMin);
                    double radius = MinRadius + ratio * (dynamicMaxRadius - MinRadius);
                    radius = Math.Max(MinRadius, Math.Min(dynamicMaxRadius, radius));

                    var fillColor = record.IsValid
                        ? Color.FromRgb(61, 139, 94)   // UISuccessColor
                        : Color.FromRgb(192, 57, 43);  // UIDangerColor

                    //  Connecting line to previous point 
                    if (i > 0)
                    {
                        double prevX = SidePadding + (i - 1) * step;

                        GraphCanvas.Children.Add(new Line
                        {
                            X1 = prevX,
                            Y1 = centerY,
                            X2 = centerX,
                            Y2 = centerY,
                            Stroke = new SolidColorBrush(Color.FromRgb(74, 127, 165)),
                            StrokeThickness = 1.5,
                            StrokeDashArray = new DoubleCollection { 4, 3 }
                        });
                    }

                    // Tick mark + time label on X axis 
                    GraphCanvas.Children.Add(new Line
                    {
                        X1 = centerX,
                        Y1 = axisY - 4,
                        X2 = centerX,
                        Y2 = axisY + 4,
                        Stroke = new SolidColorBrush(Color.FromRgb(61, 69, 80)),
                        StrokeThickness = 1
                    });

                    var timeLabel = new TextBlock
                    {
                        Text = record.Timestamp.ToString("HH:mm:ss"),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 158, 168))
                    };
                    timeLabel.Measure(new Size(100, 20));
                    Canvas.SetLeft(timeLabel, centerX - timeLabel.DesiredSize.Width / 2.0);
                    Canvas.SetTop(timeLabel, axisY + 8);
                    GraphCanvas.Children.Add(timeLabel);

                    //  Circle marker 
                    var ellipse = new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = new SolidColorBrush(fillColor),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1.5
                    };
                    Canvas.SetLeft(ellipse, centerX - radius);
                    Canvas.SetTop(ellipse, centerY - radius);
                    GraphCanvas.Children.Add(ellipse);

                    // Value label inside circle 
                    var valueLabel = new TextBlock
                    {
                        Text = record.Value.ToString("F1"),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    valueLabel.Measure(new Size(80, 20));
                    Canvas.SetLeft(valueLabel, centerX - valueLabel.DesiredSize.Width / 2.0);
                    Canvas.SetTop(valueLabel, centerY - valueLabel.DesiredSize.Height / 2.0);
                    GraphCanvas.Children.Add(valueLabel);
                }
            });
        }
    }
}