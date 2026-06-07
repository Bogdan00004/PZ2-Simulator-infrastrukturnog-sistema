using NetworkService.ViewModel;

namespace NetworkService.Helpers
{
    public abstract class ValidationBase : BindableBase
    {
        public ValidationErrors ValidationErrors { get; private set; } = new ValidationErrors();

        public bool IsValid { get; private set; }

        protected abstract void ValidateSelf();

        public void Validate()
        {
            ValidationErrors.Clear();
            ValidateSelf();
            IsValid = ValidationErrors.IsValid;
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(ValidationErrors));
        }
    }
}