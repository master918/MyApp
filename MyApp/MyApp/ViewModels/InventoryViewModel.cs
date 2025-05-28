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

namespace MyApp.ViewModels
{
    public class InventoryViewModel : BaseViewModel
    {
        private readonly GoogleService _googleService = new GoogleService();
        private string _selectedSheet;

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

        public string SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                if (SetProperty(ref _selectedSheet, value)&& _selectedSheet != null)
                {
                    // Загружаем структуру при смене выбранного листа
                    _ = LoadSheetStructureAsync();
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
        public async Task LoadSheetStructureAsync()
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

                    // Новая форма найдена
                    if (!FormTypes.Contains(formTitle))
                        FormTypes.Add(formTitle);

                    if (!FormFields.ContainsKey(formTitle))
                        FormFields[formTitle] = new List<InventoryField>();

                    var fieldsForForm = FormFields[formTitle];
                    int? nameColumnIndex = null;
                    var writeColumns = new List<(int ColumnIndex, int ColumnNumber)>();
                    var readColumns = new List<(int ColumnIndex, int ColumnNumber)>();
                    string inheritedLabelPart1 = null;

                    // Считываем поля формы, пока в 5 строке (columnNumberRow) есть номер столбца
                    while (col < columnNumberRow.Count)
                    {
                        var columnNumberStr = columnNumberRow[col]?.ToString()?.Trim();
                        if (!int.TryParse(columnNumberStr, out int columnNumber))
                            break; // конец текущей формы

                        if (col >= accessRow.Count)
                        {
                            col++;
                            continue;
                        }

                        var access = accessRow[col]?.ToString()?.Trim().ToUpper();
                        bool isValidField = access == "NAME" || access == "W" || access == "R";
                        if (!isValidField)
                        {
                            col++;
                            continue;
                        }

                        var rawLabel1 = labelRow1[col]?.ToString()?.Trim();
                        var rawLabel2 = labelRow2[col]?.ToString()?.Trim();

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
                            IsNameField = access == "NAME"
                        });

                        if (access == "NAME")nameColumnIndex = col;
                        else if (access == "W")writeColumns.Add((col, columnNumber));
                        else if (access == "R")readColumns.Add((col, columnNumber));
                        col++;
                    }

                    // Загружаем данные для формы
                    var dataStartRow = 6;
                    var dataRange = $"{sheetName}!{dataStartRow}:1000";
                    var sheetData = await _googleService.GetRangeValuesAsync(spreadsheetId, dataRange);

                    var itemsToSave = new List<InventoryItem>();

                    foreach (var dataRow in sheetData)
                    {
                        if (dataRow.All(cell => string.IsNullOrWhiteSpace(cell?.ToString())))
                            continue;

                        var item = new InventoryItem
                        {
                            SheetName = sheetName,
                            FormType = formTitle,
                            WriteColumnValues = new Dictionary<int, string>(),
                            ReadColumnValues = new Dictionary<int, string>()
                        };

                        if (nameColumnIndex.HasValue && nameColumnIndex.Value < dataRow.Count)
                        {
                            var nameValue = dataRow[nameColumnIndex.Value]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(nameValue))
                                item.Name = nameValue;
                        }

                        if (nameColumnIndex.HasValue && nameColumnIndex.Value < dataRow.Count)
                        {
                            var nameValue = dataRow[nameColumnIndex.Value]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(nameValue))
                                item.Name = nameValue;
                        }

                        foreach (var (colIndex, colNumber) in writeColumns)
                        {
                            if (colIndex >= dataRow.Count || colNumber <= 0)
                                continue;

                            var value = dataRow[colIndex]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(value))
                                item.WriteColumnValues[colNumber] = value;
                        }
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

                    if (itemsToSave.Count > 0)
                    {
                        await LocalDbService.DeleteItemsForFormAsync(sheetName, formTitle);
                        await LocalDbService.SaveItemsBatchAsync(itemsToSave);
                    }
                    var s = await LocalDbService.Database.QueryAsync<InventoryItem>("select * from InventoryItem");
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
                IsLoading = false;
            }
        }




        private async Task UpdateInventoryFieldsForSelectedForm()
        {
            InventoryFields.Clear();

            if (!string.IsNullOrEmpty(SelectedFormType) && FormFields.TryGetValue(SelectedFormType, out var fields))
            {
                foreach (var field in fields)
                {
                    if (field.IsNameField)
                    {
                        var entries = await LocalDbService.GetEntriesAsync(SelectedSheet);
                        var values = entries.Select(e => e.Name).Distinct().ToList();
                        field.Items = new ObservableCollection<string>(values);

                        foreach (var val in values)
                        {
                            if (!ItemNames.Contains(val))
                                ItemNames.Add(val);
                        }
                    }

                    InventoryFields.Add(field);
                }
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
