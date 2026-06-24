using System.Collections.Generic;
using System.ComponentModel;

namespace NetworkService.Helpers
{
    public class ValidationErrors : INotifyPropertyChanged
    {
        private readonly Dictionary<string, string> _validationErrors = new Dictionary<string, string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsValid => _validationErrors.Count == 0;

        public string this[string fieldName]
        {
            get
            {
                return _validationErrors.ContainsKey(fieldName) ? _validationErrors[fieldName] : string.Empty;
            }
            set
            {
                if (_validationErrors.ContainsKey(fieldName))
                {
                    if (string.IsNullOrWhiteSpace(value))
                        _validationErrors.Remove(fieldName);
                    else
                        _validationErrors[fieldName] = value;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        _validationErrors.Add(fieldName, value);
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsValid"));
            }
        }

        public void Clear() => _validationErrors.Clear();
    }
}