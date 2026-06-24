using NetworkService.Helpers;
using NetworkService.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace NetworkService.ViewModel
{
    public class NetworkEntitiesViewModel : BindableBase
    {
        private readonly ICollectionView _entitiesView;

        // Search properties
        private bool _searchByName = true;
        private bool _searchByType;
        private string _searchText = string.Empty;

        public bool SearchByName
        {
            get => _searchByName;
            set
            {
                SetProperty(ref _searchByName, value);
                ApplyFilter();
            }
        }

        public bool SearchByType
        {
            get => _searchByType;
            set
            {
                SetProperty(ref _searchByType, value);
                ApplyFilter();
            }
        }

        private bool _isLoadingSavedSearch = false;

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                if (!_isLoadingSavedSearch)
                    SelectedSavedSearch = null;
                _entitiesView.Refresh();
                SaveSearchCommand?.RaiseCanExecuteChanged();
            }
        }

        // Saved searches

        public ObservableCollection<SavedSearch> SavedSearches { get; } = new ObservableCollection<SavedSearch>();

        private SavedSearch _selectedSavedSearch;
        public SavedSearch SelectedSavedSearch
        {
            get => _selectedSavedSearch;
            set
            {
                SetProperty(ref _selectedSavedSearch, value);
                if (value != null) LoadSavedSearch(value);
            }
        }

        // Add entity properties 

        private PressureGaugeType _selectedTypeToAdd;
        public PressureGaugeType SelectedTypeToAdd
        {
            get => _selectedTypeToAdd;
            set
            {
                SetProperty(ref _selectedTypeToAdd, value);
                if (value != null)
                    TypeValidationError = string.Empty;
            }
        }

        private string _typeValidationError = string.Empty;
        public string TypeValidationError
        {
            get => _typeValidationError;
            set
            {
                SetProperty(ref _typeValidationError, value);
                OnPropertyChanged(nameof(HasTypeValidationError));
            }
        }

        public bool HasTypeValidationError => !string.IsNullOrEmpty(TypeValidationError);

        public List<PressureGaugeType> AvailableTypes => PressureGaugeType.PredefinedTypes;

        // Selected entity (for delete)
        private PressureGauge _selectedEntity;
        public PressureGauge SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                SetProperty(ref _selectedEntity, value);
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        // Filtered view
       
        public ICollectionView EntitiesView => _entitiesView;

       
        // Commands
        
        public MyICommand AddCommand { get; }
        public MyICommand DeleteCommand { get; }
        public MyICommand SaveSearchCommand { get; }
        public MyICommand ClearSearchCommand { get; }

        
        // Constructor
        
        public NetworkEntitiesViewModel()
        {
            _entitiesView = CollectionViewSource.GetDefaultView(MainWindowViewModel.Entities);
            _entitiesView.Filter = FilterEntity;

            AddCommand = new MyICommand(OnAdd);
            DeleteCommand = new MyICommand(OnDelete, CanDelete);
            SaveSearchCommand = new MyICommand(OnSaveSearch, CanSaveSearch);
            ClearSearchCommand = new MyICommand(OnClearSearch);
        }

        // Filter logic 
        
        private bool FilterEntity(object obj)
        {
            if (!(obj is PressureGauge entity)) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            string text = SearchText.ToLower();

            if (SearchByName)
                return entity.Name.ToLower().Contains(text);

            if (SearchByType)
                return entity.TypeName.ToLower().Contains(text);

            return true;
        }

        private void ApplyFilter() => _entitiesView.Refresh();

        // Add entity auto-generate ID and Name
        private void OnAdd()
        {
            if (SelectedTypeToAdd == null)
            {
                TypeValidationError = "Please select an entity type before adding.";
                return;
            }

            int newId = MainWindowViewModel.Entities.Any() ? MainWindowViewModel.Entities.Max(e => e.Id) + 1 : 1;
            string newName = $"PG-VALVE-{newId:D3}";

            var newEntity = new PressureGauge
            {
                Id = newId,
                Name = newName,
                Type = SelectedTypeToAdd
            };

            MainWindowViewModel.Entities.Add(newEntity);
            _entitiesView.Refresh();

            ToastNotificationService.ShowSuccess("Entity Added",$"'{newName}' (ID: {newId}) added successfully.");

            MainWindowViewModel.RestartSimulator();
        }
        // Delete entity
        private bool CanDelete() => SelectedEntity != null;

        private void OnDelete()
        {
            if (SelectedEntity == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete '{SelectedEntity.Name}' (ID: {SelectedEntity.Id})?", "Confirm Delete",MessageBoxButton.YesNo,MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            string deletedName = SelectedEntity.Name;
            int deletedId = SelectedEntity.Id;

            MainWindowViewModel.Entities.Remove(SelectedEntity);
            SelectedEntity = null;

            _entitiesView.Refresh();

            ToastNotificationService.ShowSuccess("Entity Deleted", $"'{deletedName}' (ID: {deletedId}) removed successfully.");

            MainWindowViewModel.RestartSimulator();
        }

        // Save search 
        private bool CanSaveSearch() => !string.IsNullOrWhiteSpace(SearchText);

        private void OnSaveSearch()
        {
            var search = new SavedSearch
            {
                SearchBy = SearchByName ? "Name" : "Type",
                SearchText = SearchText
            };

            bool alreadyExists = SavedSearches.Any(s => s.SearchBy == search.SearchBy && s.SearchText == search.SearchText);

            if (!alreadyExists)
            {
                SavedSearches.Add(search);
                ToastNotificationService.ShowSuccess("Search Saved",$"Search '{search}' saved successfully.");
            }
            else
            {
                ToastNotificationService.ShowWarning("Already Saved", "This search is already in the saved list.");
            }
        }

        private void LoadSavedSearch(SavedSearch search)
        {
            _isLoadingSavedSearch = true;
            SearchByName = search.SearchBy == "Name";
            SearchByType = search.SearchBy == "Type";
            SearchText = search.SearchText;
            _isLoadingSavedSearch = false;
        }

        // Clear search

        private void OnClearSearch()
        {
            SearchText = string.Empty;
            SearchByName = true;
            SearchByType = false;
            _entitiesView.Refresh();
        }
    }
}