using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using MyApp.Services;
using System.Linq;
using System;
using MyApp.Items;
using Xamarin.Essentials;
using System.Collections.Generic;
using Newtonsoft.Json;
using ZXing.Mobile;
using System.Diagnostics;

namespace MyApp.ViewModels
{
    public class InventoryViewModel : BaseViewModel
    {
        // Добавляем недостающие команды
        public Command LoadSheetsCommand { get; }
        public Command LoadFormDataCommand { get; }

        // Добавляем недостающее поле
        private readonly Dictionary<string, List<InventoryField>> _formFields = new Dictionary<string, List<InventoryField>>();

        // Добавляем методы для обработки изменений
        private async void OnSheetChanged()
        {
            if (!string.IsNullOrEmpty(SelectedSheet))
            {
                await LoadFormStructureAsync();
            }
        }
        private async void OnFormTypeChanged()
        {
            if (!string.IsNullOrEmpty(SelectedFormType))
            {
                await LoadFormDataAsync();
                foreach(var indexer in Fields.Where(i => i.IsNameField)) { indexer.Value = null; }
            }
        }

        // Добавляем метод для показа ошибок
        private async Task ShowErrorAsync(string message, Exception ex)
        {
            Debug.WriteLine($"{message}: {ex}");
            await Application.Current.MainPage.DisplayAlert("Ошибка", message, "OK");
        }

        private readonly GoogleService _googleService;
        private readonly InventoryRepository _repository;

        // Основные данные
        public ObservableCollection<string> SheetNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> FormTypes { get; } = new ObservableCollection<string>();
        public ObservableCollection<InventoryField> Fields { get; } = new ObservableCollection<InventoryField>();

        // Выбранные значения
        private string _selectedSheet;
        public string SelectedSheet
        {
            get => _selectedSheet;
            set => SetProperty(ref _selectedSheet, value, onChanged: OnSheetChanged);
        }

        private string _selectedFormType;
        public string SelectedFormType
        {
            get => _selectedFormType;
            set => SetProperty(ref _selectedFormType, value, onChanged: OnFormTypeChanged);
        }

        public InventoryViewModel(GoogleService googleService, InventoryRepository repository)
        {
            _googleService = googleService;
            _repository = repository;

            LoadSheetsCommand = new Command(async () => await LoadSheetsAsync());
            LoadFormDataCommand = new Command(async () => await LoadFormDataAsync());
        }

        private async Task LoadSheetsAsync()
        {
            try
            {
                IsBusy = true;
                var sheets = await _googleService.GetSheetTitlesAsync();

                SheetNames.Clear();
                foreach (var sheet in sheets.Where(s => !s.Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
                {
                    SheetNames.Add(sheet);
                }

                SelectedSheet = SheetNames.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Ошибка загрузки списка помещений", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFormStructureAsync()
        {
            try
            {
                IsBusy = true;
                Fields.Clear();
                FormTypes.Clear();
                _formFields.Clear();

                var structure = await _googleService.GetRangeValuesAsync(
                    Preferences.Get("SpreadsheetId", null),
                    $"{SelectedSheet}!1:5");

                // Определяем максимальное количество ячеек в строках
                int maxColumns = structure.Max(row => row.Count);

                // Дополняем каждую строку пустыми значениями до одинаковой длины
                foreach (var row in structure)
                {
                    while (row.Count < maxColumns)
                        row.Add("");
                }

                // Парсим структуру формы
                var formTitles = structure[0];
                var labels1 = structure[1];
                var labels2 = structure[2];
                var accessTypes = structure[3];
                var columnNumbers = structure[4];

                for (int i = 0; i < formTitles.Count; i++)
                {
                    var formTitle = formTitles[i]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(formTitle)) continue;

                    // Добавляем тип формы если его еще нет
                    if (!FormTypes.Contains(formTitle))
                        FormTypes.Add(formTitle);

                    // Создаем поля для этой формы
                    var fields = new List<InventoryField>();
                    string groupLabel = null;

                    for (int j = i; j < columnNumbers.Count; j++)
                    {
                        if (!int.TryParse(columnNumbers[j]?.ToString(), out int colNumber))
                            break;

                        var access = accessTypes[j]?.ToString()?.Trim().ToUpper();
                        if (access != "NAME" && access != "W" && access != "R")
                            continue;

                        // Определяем метку поля
                        var label1 = labels1[j]?.ToString()?.Trim();
                        var label2 = labels2[j]?.ToString()?.Trim();

                        if (!string.IsNullOrEmpty(label1))
                            groupLabel = label1;

                        var fullLabel = $"{groupLabel} {label2}".Trim();
                        if (string.IsNullOrEmpty(fullLabel))
                            continue;

                        // Создаем поле
                        var field = new InventoryField
                        {
                            Id = j,
                            Label = fullLabel,
                            ColumnIndex = colNumber,
                            AccessType = access == "NAME" ? FieldAccessType.Name :
                                        access == "R" ? FieldAccessType.Read : FieldAccessType.Write,
                            IsVisible = access == "NAME" // Показываем только поле Name изначально
                        };

                        // Подписываемся на изменение поля Name
                        if (access == "NAME")
                        {
                            field.OnNameChanged += OnNameChanged;
                        }

                        fields.Add(field);
                    }

                    _formFields[formTitle] = fields;
                }

                // Загружаем поля для выбранного типа формы
                if (FormTypes.Any() && _formFields.TryGetValue(FormTypes.First(), out var formFields))
                {
                    foreach (var field in formFields)
                    {
                        Fields.Add(field);
                    }
                }

                SelectedFormType = FormTypes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Ошибка загрузки структуры формы", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFormDataAsync()
        {
            try
            {
                IsBusy = true;

                // Загружаем данные из Google Sheets
                var sheetData = await _googleService.GetRangeValuesAsync(
                    Preferences.Get("SpreadsheetId", null),
                    $"{SelectedSheet}!6:1000");

                // Преобразуем в InventoryItem и сохраняем в локальную БД
                var items = ParseSheetData(sheetData);
                await _repository.SaveItemsAsync(SelectedSheet, SelectedFormType, items);

                // Обновляем автодополнение
                await UpdateSuggestionsAsync();

                // Обновляем видимость полей (скрываем все, кроме поля Name)
                UpdateFieldVisibility(null);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Ошибка загрузки данных", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task UpdateSuggestionsAsync()
        {
            var names = await _repository.GetItemNamesAsync(SelectedSheet, SelectedFormType);
            var nameField = Fields.FirstOrDefault(f => f.IsNameField);

            if (nameField != null)
            {
                nameField.AllSuggestions = names.ToList(); // сохраняем полный список
                nameField.UpdateSuggestions(nameField.AllSuggestions); // можно без фильтра
            }
        }

        private async Task LoadFieldValuesAsync(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return;

            var values = await _repository.GetFieldValuesAsync(
                SelectedSheet, SelectedFormType, itemName);

            if (values.Count != 0)
            {
                foreach (var field in Fields)
                {
                    if (field.Id.HasValue &&
                        values.TryGetValue(field.Id.Value, out string value))
                    {
                        field.Value = value;
                    }
                }
            }
            else
            {
                foreach (var field in Fields.Where(s => !s.IsNameField))
                {
                    field.Value = null;
                }
            }
        }

        private List<InventoryItem> ParseSheetData(IList<IList<object>> sheetData)
        {
            var items = new List<InventoryItem>();
            var fields = _formFields[SelectedFormType];

            foreach (var row in sheetData)
            {
                if (row.All(c => string.IsNullOrWhiteSpace(c?.ToString())))
                    continue;

                var item = new InventoryItem
                {
                    SheetName = SelectedSheet,
                    FormType = SelectedFormType,
                    WriteColumnValues = new Dictionary<int, string>()
                };

                foreach (var field in fields)
                {
                    if (!field.Id.HasValue) continue;

                    int colIndex = field.Id.Value;
                    if (colIndex >= row.Count) continue;

                    var value = row[colIndex]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    if (field.IsNameField)
                    {
                        item.Name = value;
                    }
                    else if (field.IsWritable)
                    {
                        item.WriteColumnValues[field.Id.Value] = value;
                    }
                    else if (field.IsReadOnly)
                    {
                        item.ReadColumnValues[field.Id.Value] = value;
                    }
                }

                if (!string.IsNullOrEmpty(item.Name))
                    items.Add(item);
            }

            return items;
        }

        public void UpdateFieldVisibility(string name)
        {
            var showFields = !string.IsNullOrEmpty(name);
            foreach (var field in Fields.Where(f => !f.IsNameField))
            {
                field.IsVisible = showFields;
            }
        }

        private async void OnNameChanged(string name)
        {
            await LoadFieldValuesAsync(name);

            // Обновляем видимость полей
            bool showFields = !string.IsNullOrEmpty(name);
            foreach (var field in Fields.Where(f => !f.IsNameField))
            {
                field.IsVisible = showFields;
            }
        }
    }
}
