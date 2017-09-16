using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace prime_num_searcher_gui
{
    class ValidatableDataBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private Dictionary<string, PropertyChangedEventArgs> propertyChangedEventArgsCache = new Dictionary<string, PropertyChangedEventArgs> { };
        public void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            if (!propertyChangedEventArgsCache.TryGetValue(propertyName, out var e))
            {
                e = propertyChangedEventArgsCache[propertyName] = new PropertyChangedEventArgs(propertyName);
            }
            this.PropertyChanged?.Invoke(this, e);
        }
        #endregion

        #region INotifyDataErrorInfo
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private readonly ActiveValidationMessageManager currentErrors = new ActiveValidationMessageManager { };
        public System.Collections.IEnumerable GetErrors(string propertyName) => currentErrors[propertyName];
        public bool HasErrors
        {
            get => currentErrors.HasErrors;
        }
        private void OnErrorsChanged(string propertyName) => this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        #endregion
        private void ValidateProperty(string propertyName, object value)
        {
            var validationErrors = new List<ValidationResult>();
            if (!Validator.TryValidateProperty(value, new ValidationContext(this) { MemberName = propertyName }, validationErrors))
            {
                this.currentErrors.Add(propertyName, validationErrors.Select(error => error.ErrorMessage));
            }
            else
            {
                this.currentErrors.Remove(propertyName);
            }
        }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        protected bool SetAndValidatePropaty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            var isChanged = SetProperty(ref storage, value, propertyName);
            if (isChanged) this.ValidateProperty(propertyName, value);
            return isChanged;
        }
    }
}
