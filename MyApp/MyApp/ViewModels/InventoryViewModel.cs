using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using MyApp.Services;
using System.Linq;
using System;
using MyApp.Models;
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
        public Command FinishCommand { get; }
        public Command ScanQRCommand { get; }
        private List<List<InventoryField>> _completedForms = new List<List<InventoryField>>();

        public ObservableCollection<string> SheetNames { get; } = new ObservableCollection<string>();//Названия помщений
        public ObservableCollection<InventoryField> InventoryFields { get; } = new ObservableCollection<InventoryField>();//Поля форм

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
        }

        public Command LoadSheetNamesCommand { get; }

        private async Task LoadSheetNamesAsync()
        {
            try
            {
                IsLoading = true;

                var sheetTitles = await _googleService.GetSheetTitlesAsync();

                SheetNames.Clear();
                foreach (var title in sheetTitles.Where(t => !string.Equals(t, "Authorization", StringComparison.OrdinalIgnoreCase)))
                {
                    SheetNames.Add(title);
                }

                if (SheetNames.Count > 0)
                {
                    SelectedSheet = SheetNames[0];
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

                var service = new GoogleService();
                var currentSpreadsheetId = Preferences.Get("SpreadsheetId", null);
                var sheetName = SelectedSheet;

                var range = $"{sheetName}!A1:Z2"; // Читаем 1 и 2 строку
                var serviceData = await service.GetRangeValuesAsync(currentSpreadsheetId, range);

                InventoryFields.Clear();

                if (serviceData.Count >= 2)
                {
                    var firstRow = serviceData[0]; // Флаги включения (!)
                    var secondRow = serviceData[1]; // Названия полей

                    for (int i = 0; i < secondRow.Count; i++)
                    {
                        var skip = i < firstRow.Count && firstRow[i]?.ToString().Trim() == "!";
                        var title = secondRow[i]?.ToString();

                        if (!skip && !string.IsNullOrWhiteSpace(title))
                        {
                            InventoryFields.Add(new InventoryField { Label = title });
                        }
                    }
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

        // Метод для сканирования QR кода
        private async Task ScanQrCode()
        {
            try
            {
                // Проверяем разрешение на камеру
                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    if (cameraStatus != PermissionStatus.Granted)
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Для сканирования QR-кодов необходимо разрешение на использование камеры", "OK");
                        return;
                    }
                }

                // Используем текущую страницу как контекст, чтобы избежать навигации
                var scanner = new ZXing.Mobile.MobileBarcodeScanningOptions
                {
                    AutoRotate = true,
                    UseFrontCameraIfAvailable = false,
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
                };

                var page = new ZXing.Net.Mobile.Forms.ZXingScannerPage(scanner)
                {
                    Title = "Сканирование QR-кода"
                };

                // Переход на сканер внутри текущей навигации без закрытия страницы InventoryPage
                await Application.Current.MainPage.Navigation.PushAsync(page);

                page.OnScanResult += (result) =>
                {
                    page.IsScanning = false;

                    // Возвращаемся на предыдущую страницу
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current.MainPage.Navigation.PopAsync();

                        if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                        {
                            var qrData = ParseQrData(result.Text);
                            var name = qrData.TryGetValue("Наименование", out var value) ? value : "";

                            var nameField = InventoryFields.FirstOrDefault(f =>
                                !string.IsNullOrEmpty(f.Label) &&
                                f.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);

                            if (nameField != null)
                            {
                                nameField.Value = name;
                            }
                            else
                            {
                                await Application.Current.MainPage.DisplayAlert("Предупреждение",
                                    "Не найдено поле 'Наименование' в форме", "OK");
                            }
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Не удалось отсканировать QR-код: {ex.Message}", "OK");
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
        private void NextItem()
        {
            // Сохраняем текущую форму
            var snapshot = InventoryFields.Select(f => new InventoryField { Label = f.Label, Value = f.Value }).ToList();
            _completedForms.Add(snapshot);

            // Очищаем текущие значения
            foreach (var field in InventoryFields)
            {
                field.Value = string.Empty;
            }
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
                foreach (var form in _completedForms)
                {
                    var row = form.Select(f => (object)(f.Value ?? "")).ToList();
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
                _completedForms.Clear();
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

        public async Task HandleScannedText(string qrText)
        {
            var qrData = ParseQrData(qrText);
            var name = qrData.TryGetValue("Наименование", out var value) ? value : "";

            var nameField = InventoryFields.FirstOrDefault(f =>
                !string.IsNullOrEmpty(f.Label) &&
                f.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);

            if (nameField != null)
            {
                nameField.Value = name;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Предупреждение",
                    "Не найдено поле 'Наименование' в форме", "OK");
            }
        }
    }
}
