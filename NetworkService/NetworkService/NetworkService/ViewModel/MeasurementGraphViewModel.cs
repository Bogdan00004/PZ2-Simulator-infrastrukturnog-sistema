using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NetworkService.ViewModel
{
    public class MeasurementGraphViewModel : BindableBase
    {
        // Entity list for ComboBox
        public ObservableCollection<PressureGauge> Entities => MainWindowViewModel.Entities;

        // Selected entity
        private PressureGauge _selectedEntity;
        public PressureGauge SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (_selectedEntity != null)
                    _selectedEntity.History.CollectionChanged -= OnHistoryChanged;

                SetProperty(ref _selectedEntity, value);

                if (_selectedEntity != null)
                    _selectedEntity.History.CollectionChanged += OnHistoryChanged;

                GraphNeedsRedraw?.Invoke();
            }
        }

        // Redraw event — code-behind subscribes
        public event Action GraphNeedsRedraw;

        // Constructor
        public MeasurementGraphViewModel()
        {
            Entities.CollectionChanged += OnEntitiesChanged;

            // Select first entity by default, if any
            if (Entities.Count > 0)
                SelectedEntity = Entities[0];
        }

        private void OnEntitiesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If currently selected entity was removed, pick another
            if (SelectedEntity != null && !Entities.Contains(SelectedEntity))
            {
                SelectedEntity = Entities.Count > 0 ? Entities[0] : null;
            }
            // If nothing was selected and entities now exist
            else if (SelectedEntity == null && Entities.Count > 0)
            {
                SelectedEntity = Entities[0];
            }
        }

        private void OnHistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            GraphNeedsRedraw?.Invoke();
        }
    }
}