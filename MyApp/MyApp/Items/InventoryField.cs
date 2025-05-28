using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Forms;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace MyApp.Items
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    public class InventoryField : INotifyPropertyChanged
    {
        public Action<string> OnNameChanged;
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
                    FilterSuggestions(_value);

                    // Если это поле NAME, уведомим об изменении
                    if (IsNameField)
                        OnNameChanged?.Invoke(_value);
                }
            }
        }

        public string Label { get; set; }

        private bool _isNameField;
        public bool IsNameField
        {
            get => _isNameField;
            set
            {
                if (_isNameField != value)
                {
                    _isNameField = value;
                    OnPropertyChanged(nameof(IsNameField));
                    OnPropertyChanged(nameof(IsDropdown));
                }
            }
        }

        public bool IsDropdown => IsNameField;

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    OnPropertyChanged(nameof(IsReadOnly));
                }
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }
        public void UpdateVisibility(string nameValue)
        {
            // Поле Name всегда видно, остальные — только если Name не пустой
            if (IsNameField)
            {
                IsVisible = true;
            }
            else
            {
                IsVisible = !string.IsNullOrWhiteSpace(nameValue);
            }
        }

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
                    FilterSuggestions(Value);
                }
            }
        }

        // --- Автодополнение ---
        public int SuggestionItemHeight { get; set; } = 40;

        private bool _suggestionsVisible;
        public bool SuggestionsVisible
        {
            get => _suggestionsVisible;
            set
            {
                if (_suggestionsVisible != value)
                {
                    _suggestionsVisible = value;
                    OnPropertyChanged(nameof(SuggestionsVisible));
                    OnPropertyChanged(nameof(SuggestionsHeight));
                }
            }
        }

        private ObservableCollection<string> _suggestions = new ObservableCollection<string>();
        public ObservableCollection<string> Suggestions
        {
            get => _suggestions;
            set
            {
                if (_suggestions != value)
                {
                    if (_suggestions != null)
                        _suggestions.CollectionChanged -= Suggestions_CollectionChanged;

                    _suggestions = value;
                    OnPropertyChanged(nameof(Suggestions));
                    OnPropertyChanged(nameof(SuggestionsHeight));

                    if (_suggestions != null)
                        _suggestions.CollectionChanged += Suggestions_CollectionChanged;
                }
            }
        }

        public void FilterSuggestions(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || Items == null)
            {
                SuggestionsVisible = false;
                Suggestions.Clear();
                return;
            }

            var filtered = Items
                .Where(item => !string.IsNullOrEmpty(item))
                .Where(item => item.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct()
                .Take(5)
                .ToList();

            Suggestions.Clear();
            foreach (var item in filtered)
                Suggestions.Add(item);

            SuggestionsVisible = Suggestions.Any();
        }

        public double SuggestionsHeight => SuggestionsVisible && Suggestions != null
                                        ? Suggestions.Count * SuggestionItemHeight
                                        : 0;

        private void Suggestions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SuggestionsHeight));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
