using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class CanvasSlot : BindableBase
    {
        private PressureGauge _entity;
        private bool _isSelectedForConnection;

        public int Index { get; set; }

        public PressureGauge Entity
        {
            get => _entity;
            set
            {
                SetProperty(ref _entity, value);
                OnPropertyChanged(nameof(IsOccupied));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public bool IsOccupied => _entity != null;
        public bool IsEmpty => _entity == null;

        public bool IsSelectedForConnection
        {
            get => _isSelectedForConnection;
            set => SetProperty(ref _isSelectedForConnection, value);
        }
    }
}