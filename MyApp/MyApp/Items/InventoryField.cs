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
    using System.Runtime.CompilerServices;

    public class InventoryField : INotifyPropertyChanged
    {
        // Базовые свойства поля
        public int? Id { get; set; }
        public string Label { get; set; }
        public int? ColumnIndex { get; set; }

        // Тип поля (Name/Read/Write)
        public FieldAccessType AccessType { get; set; }

        // Видимость и доступность
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        // Значение поля
        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    if (IsNameField)
                    {
                        OnNameChanged?.Invoke(value);
                    }
                }
            }
        }

        // Автодополнение (только для Name полей)
        public List<string> AllSuggestions { get; set; } = new List<string>();
        public ObservableCollection<string> Suggestions { get; } = new ObservableCollection<string>();
        public bool ShowSuggestions { get; set; }
        public double SuggestionsHeight => ShowSuggestions ? Suggestions.Count * 40 : 0;

        // События
        public event Action<string> OnNameChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        // Вспомогательные свойства
        public bool IsNameField => AccessType == FieldAccessType.Name;
        public bool IsReadOnly => AccessType == FieldAccessType.Read;
        public bool IsWritable => AccessType == FieldAccessType.Write;

        private bool _suggestionsVisible;
        public bool SuggestionsVisible
        {
            get => _suggestionsVisible;
            set => SetProperty(ref _suggestionsVisible, value);
        }

        // Методы
        public void UpdateSuggestions(IEnumerable<string> items, string filter = null)
        {
            Suggestions.Clear();

            var filtered = items.Where(i =>
                string.IsNullOrEmpty(filter) ||
                i?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).Take(5);

            foreach (var item in filtered)
                Suggestions.Add(item);

            ShowSuggestions = Suggestions.Any();
            OnPropertyChanged(nameof(SuggestionsHeight));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void FilterSuggestions(string filter)
        {
            SuggestionsVisible = !string.IsNullOrEmpty(filter);
            UpdateSuggestions(AllSuggestions, filter);
            OnPropertyChanged(nameof(SuggestionsHeight));
        }
    }

    public enum FieldAccessType
    {
        Name,   // Поле с названием (автодополнение)
        Read,   // Только чтение
        Write   // Для редактирования
    }
}
