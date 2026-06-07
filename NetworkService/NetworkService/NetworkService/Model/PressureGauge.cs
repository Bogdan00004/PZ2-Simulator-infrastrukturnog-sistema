using NetworkService.Helpers;

namespace NetworkService.Model
{
    public class PressureGauge : ValidationBase
    {
        // Valid measurement range for T1 (MPa)
        public const double MinValidValue = 5.0;
        public const double MaxValidValue = 16.0;

        private int _id;
        private string _name;
        private PressureGaugeType _type;
        private double? _currentValue;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public PressureGaugeType Type
        {
            get => _type;
            set
            {
                SetProperty(ref _type, value);
                OnPropertyChanged(nameof(TypeName));
            }
        }

        public double? CurrentValue
        {
            get => _currentValue;
            set
            {
                SetProperty(ref _currentValue, value);
                OnPropertyChanged(nameof(IsValueValid));
                OnPropertyChanged(nameof(ValueDisplay));
            }
        }

        // Computed properties
        public string TypeName => _type?.Name ?? "Unknown";

        public bool IsValueValid =>
            _currentValue.HasValue &&
            _currentValue.Value >= MinValidValue &&
            _currentValue.Value <= MaxValidValue;

        public string ValueDisplay =>
            _currentValue.HasValue
                ? $"{_currentValue.Value:F2} MPa"
                : "No reading";

        protected override void ValidateSelf()
        {
            if (_id <= 0)
                ValidationErrors["Id"] = "ID must be a positive integer.";

            if (string.IsNullOrWhiteSpace(_name))
                ValidationErrors["Name"] = "Name is required.";

            if (_type == null)
                ValidationErrors["Type"] = "Type must be selected.";
        }
    }
}