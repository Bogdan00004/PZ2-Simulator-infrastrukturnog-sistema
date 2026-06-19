using FontAwesome.WPF;
using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;

namespace NetworkService.ViewModel
{
    public class NetworkDisplayViewModel : BindableBase
    {
        // =============================================
        // Collections
        // =============================================
        public ObservableCollection<CanvasSlot> Slots { get; }
        public ObservableCollection<PressureGaugeGroup> TreeViewGroups { get; }
        public ObservableCollection<EntityConnection> Connections { get; }

        // =============================================
        // Connect mode
        // =============================================
        private bool _isConnectModeActive;
        public bool IsConnectModeActive
        {
            get => _isConnectModeActive;
            set
            {
                SetProperty(ref _isConnectModeActive, value);
                OnPropertyChanged(nameof(ConnectModeButtonText));
                if (!value) ClearConnectionSelection();
            }
        }

        public string ConnectModeButtonText =>
            IsConnectModeActive ? "Exit Connect" : "Connect";

        private CanvasSlot _firstSelectedForConnection;

        // =============================================
        // Drag state
        // =============================================
        public bool IsDragging { get; private set; }
        public PressureGauge DraggedEntity { get; private set; }
        public int? DragSourceSlotIndex { get; private set; }

        // =============================================
        // Event for line redraw (code-behind subscribes)
        // =============================================
        public event Action LinesNeedRedraw;

        // =============================================
        // Commands
        // =============================================
        public MyICommand AutoPlaceCommand { get; }
        public MyICommand ToggleConnectModeCommand { get; }
        public MyICommand<CanvasSlot> ClearSlotCommand { get; }

      
        // Constructor
       
        public NetworkDisplayViewModel()
        {
            Slots = new ObservableCollection<CanvasSlot>();
            for (int i = 0; i < 12; i++)
                Slots.Add(new CanvasSlot { Index = i });

            TreeViewGroups = new ObservableCollection<PressureGaugeGroup>();
            Connections = new ObservableCollection<EntityConnection>();
            AutoPlaceCommand = new MyICommand(OnAutoPlace, CanAutoPlace);
            ToggleConnectModeCommand = new MyICommand(() => IsConnectModeActive = !IsConnectModeActive);
            ClearSlotCommand = new MyICommand<CanvasSlot>(OnClearSlot);

            InitializeTreeView();

            MainWindowViewModel.Entities.CollectionChanged += OnEntitiesCollectionChanged;
        }

        // TreeView initialization
        private void InitializeTreeView()
        {
            foreach (var entity in MainWindowViewModel.Entities)
                AddEntityToTreeView(entity);

            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        private void OnEntitiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PressureGauge entity in e.NewItems)
                    AddEntityToTreeView(entity);

            if (e.OldItems != null)
                foreach (PressureGauge entity in e.OldItems)
                    RemoveEntityFromEverywhere(entity);

            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        public void AddEntityToTreeView(PressureGauge entity)
        {
            var typeName = entity.TypeName;
            var group = TreeViewGroups.FirstOrDefault(g => g.TypeName == typeName);

            if (group == null)
            {
                group = new PressureGaugeGroup
                {
                    TypeName = typeName,
                    TypeImagePath = entity.Type?.ImagePath ?? "",
                    TypeIcon = entity.Type?.Icon ?? FontAwesomeIcon.Circle,
                    TypeIconBrush = entity.Type?.IconBrush ?? new SolidColorBrush(Colors.Gray)
                };
                TreeViewGroups.Add(group);
            }

            if (!group.Entities.Contains(entity))
                group.Entities.Add(entity);

            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        private void RemoveEntityFromEverywhere(PressureGauge entity)
        {
            // Remove from TreeView
            foreach (var group in TreeViewGroups.ToList())
            {
                group.Entities.Remove(entity);
                if (group.Entities.Count == 0)
                    TreeViewGroups.Remove(group);
            }

            // Remove from canvas slot
            var slot = Slots.FirstOrDefault(s => s.Entity == entity);
            if (slot != null)
            {
                RemoveConnectionsForSlot(slot.Index);
                slot.Entity = null;
                LinesNeedRedraw?.Invoke();
            }

            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        // =============================================
        // Auto-place (CG1)
        // =============================================
        private bool CanAutoPlace() =>
            TreeViewGroups.Any(g => g.Entities.Count > 0) &&
            Slots.Any(s => s.IsEmpty);

        private void OnAutoPlace()
        {
            var freeSlots = Slots.Where(s => s.IsEmpty).ToList();
            var allEntities = TreeViewGroups.SelectMany(g => g.Entities.ToList()).ToList();

            int placed = 0;
            foreach (var entity in allEntities)
            {
                if (placed >= freeSlots.Count) break;
                freeSlots[placed].Entity = entity;
                placed++;
            }

            // Remove placed entities from TreeView
            foreach (var group in TreeViewGroups.ToList())
            {
                foreach (var slot in freeSlots.Take(placed))
                    group.Entities.Remove(slot.Entity);

                if (group.Entities.Count == 0)
                    TreeViewGroups.Remove(group);
            }

            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        // =============================================
        // Clear slot — return entity to TreeView
        // =============================================
        private void OnClearSlot(CanvasSlot slot)
        {
            if (slot?.Entity == null) return;

            var entity = slot.Entity;

            RemoveConnectionsForSlot(slot.Index);
            slot.Entity = null;

            AddEntityToTreeView(entity);
            LinesNeedRedraw?.Invoke();
            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        private void RemoveConnectionsForSlot(int slotIndex)
        {
            var toRemove = Connections
                .Where(c => c.SlotIndexA == slotIndex || c.SlotIndexB == slotIndex)
                .ToList();
            foreach (var c in toRemove)
                Connections.Remove(c);
        }

        // =============================================
        // Connection mode
        // =============================================
        public void SelectForConnection(CanvasSlot slot)
        {
            if (!IsConnectModeActive || slot?.Entity == null) return;

            if (_firstSelectedForConnection == null)
            {
                _firstSelectedForConnection = slot;
                slot.IsSelectedForConnection = true;
            }
            else if (_firstSelectedForConnection == slot)
            {
                slot.IsSelectedForConnection = false;
                _firstSelectedForConnection = null;
            }
            else
            {
                // Check for duplicate
                bool exists = Connections.Any(c =>
                    (c.SlotIndexA == _firstSelectedForConnection.Index && c.SlotIndexB == slot.Index) ||
                    (c.SlotIndexA == slot.Index && c.SlotIndexB == _firstSelectedForConnection.Index));

                if (!exists)
                {
                    Connections.Add(new EntityConnection
                    {
                        SlotIndexA = _firstSelectedForConnection.Index,
                        SlotIndexB = slot.Index
                    });
                }

                _firstSelectedForConnection.IsSelectedForConnection = false;
                _firstSelectedForConnection = null;
                LinesNeedRedraw?.Invoke();
            }
        }

        private void ClearConnectionSelection()
        {
            if (_firstSelectedForConnection == null) return;
            _firstSelectedForConnection.IsSelectedForConnection = false;
            _firstSelectedForConnection = null;
        }

        // =============================================
        // Drag state — called from code-behind
        // =============================================
        public void StartDragFromTreeView(PressureGauge entity)
        {
            IsDragging = true;
            DraggedEntity = entity;
            DragSourceSlotIndex = null;
        }

        public void StartDragFromSlot(CanvasSlot slot)
        {
            if (slot.IsEmpty) return;
            IsDragging = true;
            DraggedEntity = slot.Entity;
            DragSourceSlotIndex = slot.Index;
        }

        public bool CanDropOnSlot(CanvasSlot targetSlot)
        {
            if (!IsDragging || DraggedEntity == null) return false;
            if (targetSlot.IsOccupied) return false;
            if (DragSourceSlotIndex.HasValue &&
                DragSourceSlotIndex.Value == targetSlot.Index) return false;
            return true;
        }

        public void DropOnSlot(CanvasSlot targetSlot)
        {
            if (!CanDropOnSlot(targetSlot)) return;

            if (DragSourceSlotIndex.HasValue)
            {
                // Moving between slots — update connection indices
                int oldIndex = DragSourceSlotIndex.Value;
                int newIndex = targetSlot.Index;

                Slots[oldIndex].Entity = null;

                foreach (var conn in Connections)
                {
                    if (conn.SlotIndexA == oldIndex) conn.SlotIndexA = newIndex;
                    if (conn.SlotIndexB == oldIndex) conn.SlotIndexB = newIndex;
                }
            }
            else
            {
                // From TreeView — remove from groups
                foreach (var group in TreeViewGroups.ToList())
                {
                    group.Entities.Remove(DraggedEntity);
                    if (group.Entities.Count == 0)
                        TreeViewGroups.Remove(group);
                }
            }

            targetSlot.Entity = DraggedEntity;
            ResetDragState();
            LinesNeedRedraw?.Invoke();
            AutoPlaceCommand.RaiseCanExecuteChanged();
        }

        public void ResetDragState()
        {
            IsDragging = false;
            DraggedEntity = null;
            DragSourceSlotIndex = null;
        }
    }
}