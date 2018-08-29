using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PinToTaskbarHelperUI.Annotations;

namespace PinToTaskbarHelperUI.Models
{
    public class MainWindowBindingModel : INotifyPropertyChanged
    {
        // Fields

        private bool _isCreatingShortcut;

        // Properties

        public bool IsCreatingShortcut
        {
            get => _isCreatingShortcut;
            set
            {
                if (value == _isCreatingShortcut) return;
                _isCreatingShortcut = value;
                OnPropertyChanged();
            }
        }

        // Events

        public event PropertyChangedEventHandler PropertyChanged;

        // Methods

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
