using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;

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
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));

                    if (IsNameField)
                    {
                        IsCustomNameEntryVisible = value == "<Создать>";
                    }
                }
            }
        }

        public string Label { get; set; }

        public bool IsNameField => Label?.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool IsDropdown => IsNameField;

        private ObservableCollection<string> _items = new ObservableCollection<string>();
        public ObservableCollection<string> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChanged(nameof(Items));
                }
            }
        }

        private bool _isCustomNameEntryVisible;
        public bool IsCustomNameEntryVisible
        {
            get => _isCustomNameEntryVisible;
            set
            {
                if (_isCustomNameEntryVisible != value)
                {
                    _isCustomNameEntryVisible = value;
                    OnPropertyChanged(nameof(IsCustomNameEntryVisible));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public class CompletedForm
    {
        public int Index { get; set; }
        public List<InventoryField> Fields { get; set; }

        public string DisplayText => $"{Index}. {GetTitle()}";

        private string GetTitle()
        {
            var nameField = Fields?.FirstOrDefault(f =>
                !string.IsNullOrEmpty(f.Label) &&
                f.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);
            return nameField?.Value ?? "(без наименования)";
        }
    }
}