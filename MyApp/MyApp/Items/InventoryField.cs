﻿using System;
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
                    FilterSuggestions(_value);
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
                    FilterSuggestions(Value); // обновим подсказки при изменении списка
                }
            }
        }

        // --- Автодополнение ---
        public int SuggestionItemHeight { get; set; } = 40; // Высота одного элемента
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
                .Where(item => !string.IsNullOrEmpty(item)) // Фильтруем пустые значения
                .Where(item => item.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct()
                .Take(5)
                .ToList();

            Suggestions.Clear();
            foreach (var item in filtered)
                Suggestions.Add(item);

            SuggestionsVisible = Suggestions.Any();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public double SuggestionsHeight => SuggestionsVisible && Suggestions != null
                                        ? Suggestions.Count * SuggestionItemHeight
                                        : 0;

        private void Suggestions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SuggestionsHeight));
        }


    }
}
