using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Views
{
    public partial class NetworkDisplayView : UserControl
    {
        private NetworkDisplayViewModel _viewModel;
        private bool _isDragging;
        private Point _treeDragStartPoint;

        public NetworkDisplayView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as NetworkDisplayViewModel;
            if (_viewModel == null) return;

            _viewModel.LinesNeedRedraw += RedrawLines;
            SizeChanged += (s, args) => RedrawLines();
        }

        // =============================================
        // TreeView drag — PreviewMouseMove approach
        // =============================================
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _treeDragStartPoint = e.GetPosition(null);
        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

            var currentPos = e.GetPosition(null);
            var diff = _treeDragStartPoint - currentPos;

            bool movedEnough =
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

            if (!movedEnough) return;

            if (!(EntitiesTreeView.SelectedItem is PressureGauge entity)) return;

            _isDragging = true;
            _viewModel?.StartDragFromTreeView(entity);
            DragDrop.DoDragDrop(EntitiesTreeView, entity, DragDropEffects.Move);
        }

        private void TreeView_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            // Selection change alone does not start drag anymore
        }

        private void TreeView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _viewModel?.ResetDragState();
        }

        // =============================================
        // Slot drag (slot to slot)
        // =============================================
        private void Slot_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_isDragging) return;
            if (_viewModel?.IsConnectModeActive == true) return;
            if (IsInsideButton(e.OriginalSource as DependencyObject)) return;

            var slot = GetSlotFromSender(sender);
            if (slot == null || slot.IsEmpty) return;

            _isDragging = true;
            _viewModel?.StartDragFromSlot(slot);
            DragDrop.DoDragDrop((DependencyObject)sender, slot, DragDropEffects.Move);
            _viewModel?.ResetDragState();
            _isDragging = false;
        }

        // =============================================
        // Slot click — connect mode
        // =============================================
        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.IsConnectModeActive != true) return;
            if (IsInsideButton(e.OriginalSource as DependencyObject)) return;

            var slot = GetSlotFromSender(sender);
            if (slot?.IsOccupied == true)
            {
                _viewModel.SelectForConnection(slot);
                e.Handled = true;
            }
        }

        // =============================================
        // Slot DragOver / Drop
        // =============================================
        private void Slot_DragOver(object sender, DragEventArgs e)
        {
            var slot = GetSlotFromSender(sender);
            e.Effects = _viewModel?.CanDropOnSlot(slot) == true
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Slot_Drop(object sender, DragEventArgs e)
        {
            var slot = GetSlotFromSender(sender);
            _viewModel?.DropOnSlot(slot);
            _isDragging = false;
            e.Handled = true;
        }

        // =============================================
        // Line drawing
        // =============================================
        private void RedrawLines()
        {
            if (_viewModel == null) return;

            Dispatcher.Invoke(() =>
            {
                LinesCanvas.Children.Clear();

                foreach (var connection in _viewModel.Connections)
                {
                    var centerA = GetSlotCenterPoint(connection.SlotIndexA);
                    var centerB = GetSlotCenterPoint(connection.SlotIndexB);

                    LinesCanvas.Children.Add(new Line
                    {
                        X1 = centerA.X,
                        Y1 = centerA.Y,
                        X2 = centerB.X,
                        Y2 = centerB.Y,
                        Stroke = new SolidColorBrush(Color.FromRgb(74, 127, 165)),
                        StrokeThickness = 1.5,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        IsHitTestVisible = false
                    });
                }
            });
        }

        private Point GetSlotCenterPoint(int slotIndex)
        {
            double cellWidth = CanvasArea.ActualWidth / 4.0;
            double cellHeight = CanvasArea.ActualHeight / 3.0;
            int col = slotIndex % 4;
            int row = slotIndex / 4;

            return new Point(
                col * cellWidth + cellWidth / 2.0,
                row * cellHeight + cellHeight / 2.0
            );
        }

        // =============================================
        // Helpers
        // =============================================
        private CanvasSlot GetSlotFromSender(object sender)
        {
            if (sender is ContentControl cc)
                return cc.Content as CanvasSlot;
            return (sender as FrameworkElement)?.DataContext as CanvasSlot;
        }

        private bool IsInsideButton(DependencyObject element)
        {
            while (element != null)
            {
                if (element is Button) return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }
    }
}