using System;
using System.ComponentModel;

namespace MyApp.Models
{
    public class LogPass
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class InventoryField : INotifyPropertyChanged
    {
        private string _value;

        public string Label { get; set; }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}