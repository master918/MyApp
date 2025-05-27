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

        public string SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                if (SetProperty(ref _selectedSheet, value))
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

        public Command LoadSheetNamesCommand { get; }

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
        //public async Task LoadSheetStructureAsync()
        //{
        //    try
        //    {
        //        IsLoading = true;

        //        var service = new GoogleService();
        //        var currentSpreadsheetId = Preferences.Get("SpreadsheetId", null);
        //        var sheetName = SelectedSheet;

        //        var range = $"{sheetName}!A1:Z2"; // Считываем строки с метками и заголовками
        //        var serviceData = await service.GetRangeValuesAsync(currentSpreadsheetId, range);

        //        InventoryFields.Clear();

        //        if (serviceData.Count >= 2)
        //        {
        //            var firstRow = serviceData[0]; // строки с "!" — пропускаемые поля
        //            var secondRow = serviceData[1]; // заголовки полей

        //            int nameFieldIndex = -1;

        //            for (int i = 0; i < secondRow.Count; i++)
        //            {
        //                var skip = i < firstRow.Count && firstRow[i]?.ToString().Trim() == "!";
        //                var title = secondRow[i]?.ToString();

        //                if (!skip && !string.IsNullOrWhiteSpace(title))
        //                {
        //                    var field = new InventoryField { Label = title };

        //                    // Определим, является ли это поле "Наименование"
        //                    if (field.IsNameField)
        //                    {
        //                        nameFieldIndex = i;
        //                    }

        //                    InventoryFields.Add(field);
        //                }
        //            }

        //            // Загружаем значения для поля "Наименование", если индекс найден
        //            if (nameFieldIndex >= 0)
        //            {
        //                var nameRange = $"{sheetName}!{(char)('A' + nameFieldIndex)}3:{(char)('A' + nameFieldIndex)}";
        //                var nameValues = await service.GetRangeValuesAsync(currentSpreadsheetId, nameRange);

        //                var nameField = InventoryFields.FirstOrDefault(f => f.IsNameField);
        //                if (nameField != null)
        //                {
        //                    nameField.Items.Clear();

        //                    foreach (var row in nameValues)
        //                    {
        //                        if (row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]?.ToString()))
        //                            nameField.Items.Add(row[0].ToString());
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка при загрузке структуры формы: {ex.Message}", "OK");
        //    }
        //    finally
        //    {
        //        IsLoading = false;
        //    }
        //}

        public async Task LoadSheetStructureAsync()
        {
            try
            {
                IsLoading = true;

                var spreadsheetId = Preferences.Get("SpreadsheetId", null);
                var sheetName = SelectedSheet;

                //var maxColumns = 36; // A-AJ
                //var maxRows = 1000;   // ограничим для безопасности
                var range = $"{sheetName}!1:5";
                //$"{sheetName}!A1:{(char)('A' + maxColumns - 1)}{maxRows}";

                var serviceData = await _googleService.GetRangeValuesAsync(spreadsheetId, range);

                InventoryFields.Clear();
                ItemNames.Clear();

                int row = 0;
                while (row < serviceData.Count)
                {
                    var currentRow = serviceData[row];
                    if (currentRow.Count == 0)
                    {
                        row++;
                        continue;
                    }

                    // Если есть наименование формы в первой ячейке — это начало формы
                    var formTitle = currentRow[0]?.ToString();
                    if (!string.IsNullOrWhiteSpace(formTitle) &&
                        formTitle.StartsWith("ИНВ", StringComparison.OrdinalIgnoreCase))
                    {
                        row++; // переходим к заголовкам
                        if (row + 2 >= serviceData.Count)
                            break;

                        var headerRow1 = serviceData[row];
                        var headerRow2 = serviceData[row + 1];
                        var accessRow = serviceData[row + 2];

                        var fieldCount = Math.Min(Math.Min(headerRow1.Count, headerRow2.Count), accessRow.Count);
                        var nameFieldIndex = -1;

                        for (int col = 0; col < fieldCount; col++)
                        {
                            var labelPart1 = headerRow1[col]?.ToString()?.Trim();
                            var labelPart2 = headerRow2[col]?.ToString()?.Trim();
                            var access = accessRow[col]?.ToString()?.Trim().ToUpper();

                            var label = $"{labelPart1} {labelPart2}".Trim();

                            if (string.IsNullOrWhiteSpace(label) || access == "!")
                                continue;

                            var field = new InventoryField
                            {
                                Label = label
                            };

                            switch (access)
                            {
                                case "R":
                                    // read-only — можно добавить доп. обработку
                                    break;
                                case "W":
                                    // writable
                                    break;
                                case "NAME":
                                    nameFieldIndex = col;
                                    break;
                            }

                            InventoryFields.Add(field);
                        }

                        // Загрузим список наименований, если есть поле NAME
                        if (nameFieldIndex >= 0)
                        {
                            var nameColumnLetter = ((char)('A' + nameFieldIndex)).ToString();
                            var nameRange = $"{sheetName}!{nameColumnLetter}{row + 4}:{nameColumnLetter}";
                            var nameValues = await _googleService.GetRangeValuesAsync(spreadsheetId, nameRange);

                            var nameField = InventoryFields.FirstOrDefault(f => f.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);
                            if (nameField != null)
                            {
                                foreach (var valueRow in nameValues)
                                {
                                    if (valueRow.Count > 0 && !string.IsNullOrWhiteSpace(valueRow[0]?.ToString()))
                                    {
                                        var name = valueRow[0].ToString();
                                        nameField.Items.Add(name);
                                        ItemNames.Add(name); // если нужно глобально
                                    }
                                }
                            }
                        }

                        row += 3; // пропускаем обработанные строки (заголовки + доступы)
                    }
                    else
                    {
                        row++;
                    }
                }

                if (InventoryFields.Count == 0)
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
