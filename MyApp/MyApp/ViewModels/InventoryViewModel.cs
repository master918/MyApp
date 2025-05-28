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
        private readonly GoogleService _googleService = new GoogleService();
        private string _selectedSheet;
        public string SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                if (SetProperty(ref _selectedSheet, value) && _selectedSheet != null)
                {
                    // Загружаем структуру при смене выбранного листа
                    _ = LoadSheetStructureAndDataAsync();
                }
            }
        }

        public Command NextItemCommand { get; }
        public Command OpenCompletedFormsCommand { get; }
        public Command FinishCommand { get; }
        public Command ScanQRCommand { get; }
        public Command LoadSheetNamesCommand { get; }

        private int _currentFormNumber = 1;
        public int CurrentFormNumber
        {
            get => _currentFormNumber;
            set => SetProperty(ref _currentFormNumber, value);
        }

        public ObservableCollection<string> SheetNames { get; } = new ObservableCollection<string>();//Названия помщений
        public ObservableCollection<InventoryField> InventoryFields { get; } = new ObservableCollection<InventoryField>();//Поля форм
        public ObservableCollection<CompletedForm> CompletedForms { get; } = new ObservableCollection<CompletedForm>();
        public ObservableCollection<string> ItemNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> FormTypes { get; } = new ObservableCollection<string>();

        private Dictionary<string, List<InventoryField>> FormFields { get; set; } = new Dictionary<string, List<InventoryField>>();

        private string _selectedFormType;
        public string SelectedFormType
        {
            get => _selectedFormType;
            set
            {
                if (SetProperty(ref _selectedFormType, value))
                {
                    _ = UpdateInventoryFieldsForSelectedForm();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public InventoryViewModel()
        {
            LoadSheetNamesCommand = new Command(async () => await LoadSheetNamesAsync());
            NextItemCommand = new Command(NextItem);
            FinishCommand = new Command(async () => await FinishAsync());
            ScanQRCommand = new Command(async () => MessagingCenter.Send(this, "StartScanner"));
            OpenCompletedFormsCommand = new Command(OpenCompletedForms);
        }             

        private async Task LoadSheetNamesAsync()
        {
            try
            {
                IsLoading = true;

                var sheetTitles = await _googleService.GetSheetTitlesAsync();

                var inventorySheets = sheetTitles
                                        .Where(t => !string.Equals(t, "Authorization", StringComparison.OrdinalIgnoreCase))
                                        .ToList();

                SheetNames.Clear();
                foreach (var title in inventorySheets)
                {
                    SheetNames.Add(title);
                }

                // Принудительно устанавливаем первый доступный лист
                if (inventorySheets.Any())
                {
                    SelectedSheet = inventorySheets[0];
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Не найдены листы для инвентаризации", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось загрузить список помещений: {ex.Message}", "OK");
            }
        }
        private async Task LoadSheetStructureAndDataAsync()
        {
            await LoadStructureAsync();
            await LoadDataAsync();
        }
        public async Task LoadStructureAsync()
        {
            try
            {
                IsLoading = true;

                 var spreadsheetId = Preferences.Get("SpreadsheetId", null);
                var sheetName = SelectedSheet;
                var structureRange = $"{sheetName}!1:5";
                var serviceData = await _googleService.GetRangeValuesAsync(spreadsheetId, structureRange);

                InventoryFields.Clear();
                FormTypes.Clear();
                FormFields.Clear();
                ItemNames.Clear();

                if (serviceData.Count < 5)
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Недостаточно строк в таблице для структуры форм", "OK");
                    return;
                }

                var formHeaderRow = serviceData[0];
                var labelRow1 = serviceData[1];
                var labelRow2 = serviceData[2];
                var accessRow = serviceData[3];
                var columnNumberRow = serviceData[4];


                int col = 0;
                while (col < formHeaderRow.Count)
                {
                    var formTitle = formHeaderRow[col]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(formTitle) || !formTitle.StartsWith("ИНВ", StringComparison.OrdinalIgnoreCase))
                    {
                        col++;
                        continue;
                    }

                    if (!FormTypes.Contains(formTitle))
                        FormTypes.Add(formTitle);

                    if (!FormFields.ContainsKey(formTitle))
                        FormFields[formTitle] = new List<InventoryField>();

                    var fieldsForForm = FormFields[formTitle];
                    string inheritedLabelPart1 = null;

                    while (col < columnNumberRow.Count)
                    {
                        var columnNumberStr = columnNumberRow[col]?.ToString()?.Trim();
                        if (!int.TryParse(columnNumberStr, out int columnNumber))
                            break;

                        if (col >= accessRow.Count)
                        {
                            col++;
                            continue;
                        }

                        var access = accessRow[col]?.ToString()?.Trim().ToUpper();
                        if (access != "NAME" && access != "W" && access != "R")
                        {
                            col++;
                            continue;
                        }

                        var rawLabel1 = col < labelRow1.Count ? labelRow1[col]?.ToString()?.Trim() : null;
                        var rawLabel2 = col < labelRow2.Count ? labelRow2[col]?.ToString()?.Trim() : null;

                        if (!string.IsNullOrWhiteSpace(rawLabel1))
                            inheritedLabelPart1 = rawLabel1;

                        var label = $"{inheritedLabelPart1 ?? ""} {rawLabel2}".Trim();
                        if (string.IsNullOrWhiteSpace(label))
                        {
                            col++;
                            continue;
                        }

                        fieldsForForm.Add(new InventoryField
                        {
                            Label = label,
                            IsNameField = access == "NAME",
                            IsReadOnly = access == "R",
                            IsVisible = access == "NAME",
                            ColumnIndex = columnNumber
                        });

                        col++;
                    }
                }

                if (FormTypes.Count > 0)
                    SelectedFormType = FormTypes[0];

                if (FormFields.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Внимание", "Не удалось обнаружить структуру формы на листе", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка при загрузке структуры формы: {ex.Message}", "OK");
            }
            finally
            {
                var s = await LocalDbService.Database.QueryAsync<InventoryItem>("select * from InventoryItem");
                IsLoading = false;

            }
        }
        public async Task LoadDataAsync()
        {
            try
            {
                // Устанавливаем флаг загрузки для отображения индикатора
                IsLoading = true;

                // Получаем ID таблицы из настроек и название текущего листа
                var spreadsheetId = Preferences.Get("SpreadsheetId", null);
                var sheetName = SelectedSheet;

                // Определяем диапазон данных (начиная с 6 строки до 1000)
                var dataStartRow = 6;
                var dataRange = $"{sheetName}!{dataStartRow}:1000";

                // Загружаем данные из Google Sheets
                var sheetData = await _googleService.GetRangeValuesAsync(spreadsheetId, dataRange);

                // Обрабатываем данные для каждой формы инвентаризации
                foreach (var formType in FormTypes)
                {
                    // Пропускаем, если для формы нет полей
                    if (!FormFields.TryGetValue(formType, out var fields)) continue;

                    // Списки для хранения индексов колонок:
                    var writeColumns = new List<(int ColumnIndex, int ColumnNumber)>(); // Для записи (W)
                    var readColumns = new List<(int ColumnIndex, int ColumnNumber)>();  // Для чтения (R)
                    int? nameColumnIndex = null; // Индекс колонки с названием (NAME)

                    // Определяем индексы колонок для каждого типа поля
                    for (int i = 0; i < fields.Count; i++)
                    {
                        var field = fields[i];
                        if (field.IsNameField)
                            nameColumnIndex = i; // Запоминаем индекс колонки Name
                        else if (field.ColumnIndex.HasValue)
                        {
                            // Добавляем индексы для колонок записи и чтения
                            writeColumns.Add((i, field.ColumnIndex.Value));
                            readColumns.Add((i, field.ColumnIndex.Value));
                        }
                    }

                    var itemsToSave = new List<InventoryItem>(); // Список для сохранения в БД

                    // Обрабатываем каждую строку данных из таблицы
                    foreach (var dataRow in sheetData)
                    {
                        // Пропускаем пустые строки
                        if (dataRow.All(cell => string.IsNullOrWhiteSpace(cell?.ToString())))
                            continue;

                        // Создаем новый элемент инвентаризации
                        var item = new InventoryItem
                        {
                            SheetName = sheetName,
                            FormType = formType,
                            WriteColumnValues = new Dictionary<int, string>(),
                            ReadColumnValues = new Dictionary<int, string>()
                        };

                        // Заполняем название (если есть колонка Name и значение)
                        if (nameColumnIndex.HasValue && nameColumnIndex.Value < dataRow.Count)
                        {
                            var nameValue = dataRow[nameColumnIndex.Value]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(nameValue))
                                item.Name = nameValue;
                        }

                        // Заполняем значения для записи (W)
                        foreach (var (fieldIndex, columnIndex) in writeColumns)
                        {
                            // Преобразуем номер колонки в индекс (нумерация с 1)
                            int realCellIndex = columnIndex - 1;

                            // Проверяем, что индекс в пределах строки
                            if (realCellIndex >= dataRow.Count || columnIndex <= 0)
                                continue;

                            var value = dataRow[realCellIndex]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(value))
                                item.WriteColumnValues[columnIndex] = value;
                        }

                        // Заполняем значения для чтения (R)
                        foreach (var (colIndex, colNumber) in readColumns)
                        {
                            if (colIndex >= dataRow.Count || colNumber <= 0)
                                continue;

                            var value = dataRow[colIndex]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(value))
                                item.ReadColumnValues[colNumber] = value;
                        }

                        itemsToSave.Add(item);
                    }

                    // Сохраняем данные в локальную БД
                    if (itemsToSave.Count > 0)
                    {
                        // Сначала удаляем старые данные для этой формы
                        await LocalDbService.DeleteItemsForFormAsync(sheetName, formType);
                        // Затем сохраняем новые данные
                        await LocalDbService.SaveItemsBatchAsync(itemsToSave);
                    }
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок с показом сообщения пользователю
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка при загрузке данных: {ex.Message}", "OK");
            }
            finally
            {
                var s = await LocalDbService.Database.QueryAsync<InventoryItem>("select * from InventoryItem");
                // В любом случае снимаем флаг загрузки
                IsLoading = false;
            }
        }

        public void UpdateFieldVisibility(string nameValue)
        {
            foreach (var field in InventoryFields)
            {
                field.UpdateVisibility(nameValue);
            }
        }
        private async Task UpdateInventoryFieldsForSelectedForm()
        {
            InventoryFields.Clear();

            if (!string.IsNullOrEmpty(SelectedFormType) && FormFields.TryGetValue(SelectedFormType, out var fields))
            {
                // 1. Получаем поле Name
                var nameField = fields.FirstOrDefault(f => f.IsNameField);

                // 2. Получаем все поля кроме Name
                var otherFields = fields
                    .Where(f => !f.IsNameField)
                    .ToList();

                // 3. Загружаем список значений для автодополнения
                if (nameField != null)
                {
                    var entries = await LocalDbService.GetEntriesAsync(SelectedSheet, SelectedFormType);
                    var values = entries
                        .Select(e => e.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .ToList();

                    nameField.Items = new ObservableCollection<string>(values);

                    // Добавляем обработчик изменения значения Name
                    nameField.OnNameChanged = async (nameValue) =>
                    {
                        if (!string.IsNullOrWhiteSpace(nameValue))
                        {
                            await LoadFieldValuesFromDb(nameValue);
                        }
                        else
                        {
                            // Очищаем значения полей, если Name пустой
                            foreach (var field in InventoryFields.Where(f => !f.IsNameField))
                            {
                                field.Value = string.Empty;
                            }
                        }
                    };
                }

                // 4. Добавляем поле Name
                if (nameField != null)
                    InventoryFields.Add(nameField);

                // 5. Добавляем остальные поля
                foreach (var field in otherFields)
                {
                    field.UpdateVisibility(nameField?.Value);
                    InventoryFields.Add(field);
                }
            }
        }
        private async Task LoadFieldValuesFromDb(string nameValue)
        {
            try
            {
                var entries = await LocalDbService.GetEntriesAsync(SelectedSheet, SelectedFormType);
                var item = entries.FirstOrDefault(e =>
                    e.Name?.Equals(nameValue, StringComparison.OrdinalIgnoreCase) ?? false);

                if (item != null)
                {
                    foreach (var field in InventoryFields.Where(f => !f.IsNameField))
                    {
                        if (field.ColumnIndex is int columnIndex && item.WriteColumnValues.TryGetValue(columnIndex, out var value))
                        {
                            field.Value = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading field values: {ex.Message}");
            }
        }



        private Dictionary<string, string> ParseQrData(string qrText)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(qrText))
                return data;

            var parts = qrText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var keyValue = part.Split(new[] { ':' }, 2);
                if (keyValue.Length == 2)
                {
                    data[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return data;
        }
        public async Task HandleScannedText(string qrText)
        {
            var qrData = ParseQrData(qrText);

            if (qrData.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "QR-код не содержит данных", "OK");
                return;
            }

            int filledCount = 0;
            foreach (var field in InventoryFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Label))
                {
                    foreach (var pair in qrData)
                    {
                        // Сравнение с учетом регистра и возможных вариаций
                        if (field.Label.Trim().Equals(pair.Key.Trim(), StringComparison.OrdinalIgnoreCase) ||
                            field.Label.Trim().IndexOf(pair.Key.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            field.Value = pair.Value;
                            filledCount++;
                            break;
                        }
                    }
                }
            }

            if (filledCount == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Предупреждение", "Не удалось сопоставить поля QR-кода с формой", "OK");
            }
        }

        private async void OpenCompletedForms()
        {
            if (CompletedForms.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Просмотр", "Нет заполненных форм", "OK");
                return;
            }

            var items = CompletedForms.Select(form =>
            {
                var nameField = form.Fields.FirstOrDefault(f =>
                    !string.IsNullOrEmpty(f.Label) &&
                    f.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);

                var name = nameField?.Value ?? "(без наименования)";
                return $"{form.Index}. {name}";
            }).ToList();

            string selected = await Application.Current.MainPage.DisplayActionSheet(
                "Заполненные формы", "Отмена", null, items.ToArray());

            if (!string.IsNullOrWhiteSpace(selected) && selected != "Отмена")
            {
                var selectedForm = CompletedForms.FirstOrDefault(f =>
                {
                    var label = f.Fields.FirstOrDefault(x =>
                        !string.IsNullOrEmpty(x.Label) &&
                        x.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);

                    var name = label?.Value ?? "(без наименования)";
                    return $"{f.Index}. {name}" == selected;
                });

                if (selectedForm != null)
                    LoadCompletedForm(selectedForm);
            }
        }

        private void NextItem()
        {
            var snapshot = InventoryFields.Select(f => new InventoryField
            {
                Label = f.Label,
                Value = f.Value
            }).ToList();

            var existingForm = CompletedForms.FirstOrDefault(f => f.Index == CurrentFormNumber);
            if (existingForm != null)
            {
                // Обновляем существующую форму
                existingForm.Fields = snapshot;
            }
            else
            {
                // Добавляем новую форму
                CompletedForms.Add(new CompletedForm
                {
                    Index = CurrentFormNumber,
                    Fields = snapshot
                });
            }

            // Очистка полей и переход к следующей
            foreach (var field in InventoryFields)
                field.Value = string.Empty;

            CurrentFormNumber++;
        }

        private async Task FinishAsync()
        {
            NextItem(); // Сохраняем текущую форму

            try
            {
                IsLoading = true;
                var spreadsheetId = Preferences.Get("SpreadsheetId", null);
                var sheetName = SelectedSheet;

                var values = new List<IList<object>>();
                foreach (var form in CompletedForms)
                {
                    var row = form.Fields.Select(f => (object)(f.Value ?? "")).ToList();
                    values.Add(row);
                }

                var range = $"{sheetName}!A3"; // начинаем с 3 строки
                var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange { Values = values };

                var json = new GoogleService().GetCredentialsJson();
                var creds = JsonConvert.DeserializeObject<GoogleService.GoogleServiceAccountCreds>(json);

                var credential = new Google.Apis.Auth.OAuth2.ServiceAccountCredential(
                    new Google.Apis.Auth.OAuth2.ServiceAccountCredential.Initializer(creds.client_email)
                    {
                        Scopes = new[] { Google.Apis.Sheets.v4.SheetsService.Scope.Spreadsheets }
                    }.FromPrivateKey(creds.private_key));

                var service = new Google.Apis.Sheets.v4.SheetsService(new Google.Apis.Services.BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MyApp"
                });

                var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
                request.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                await request.ExecuteAsync();

                await Application.Current.MainPage.DisplayAlert("Успешно", "Все данные отправлены", "ОК");
                CompletedForms.Clear();
                _currentFormNumber = 1;
                OnPropertyChanged(nameof(CurrentFormNumber));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "ОК");
            }
            finally
            {
                IsLoading = false;
            }
        }
        public void LoadCompletedForm(CompletedForm form)
        {
            InventoryFields.Clear();
            foreach (var field in form.Fields)
            {
                InventoryFields.Add(new InventoryField
                {
                    Label = field.Label,
                    Value = field.Value
                });
            }

            CurrentFormNumber = form.Index;
        }
    }
}
