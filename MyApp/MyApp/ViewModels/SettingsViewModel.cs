using MyApp.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly GoogleService _googleService = new GoogleService();

        private string _currentServiceAccount;
        public string CurrentServiceAccount
        {
            get => _currentServiceAccount;
            set => SetProperty(ref _currentServiceAccount, value);
        }

        private string _spreadsheetUrl;
        public string SpreadsheetUrl
        {
            get => _spreadsheetUrl;
            set
            {
                if (SetProperty(ref _spreadsheetUrl, value))
                {
                    Task.Run(async () => await CheckConnectionDelayed());
                }
            }
        }

        private string _connectionStatus;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private bool? _connectionValid;
        public bool? ConnectionValid
        {
            get => _connectionValid;
            set => SetProperty(ref _connectionValid, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenSpreadsheetCommand { get; }
        public ICommand UploadCredentialsCommand { get; }

        public SettingsViewModel()
        {
            CurrentServiceAccount = _googleService.CurrentServiceAccount;
            LoadCurrentSettings();
            ConnectionStatus = "Проверка подключения...";

            SaveCommand = new Command(async () => await OnSave());
            CancelCommand = new Command(async () => await OnCancel());
            OpenSpreadsheetCommand = new Command(async () => await OpenSpreadsheet());
            UploadCredentialsCommand = new Command(async () => UploadAndVerifyCredentials());

            InitializeAsync();
        }

        private async Task CheckConnectionDelayed()
        {
            await Task.Delay(1000);
            await CheckConnection();
        }

        private async void InitializeAsync() => await CheckConnection();

        public async Task CheckConnection()
        {
            if (string.IsNullOrWhiteSpace(SpreadsheetUrl))
            {
                ConnectionValid = null;
                ConnectionStatus = "Укажите ссылку на Google Sheets документ";
                return;
            }

            IsBusy = true;
            ConnectionStatus = "Проверка подключения...";

            try
            {
                var spreadsheetId = ExtractSpreadsheetIdFromUrl(SpreadsheetUrl);
                var isConnected = await _googleService.TestConnectionAsync(spreadsheetId);

                ConnectionValid = isConnected;
                ConnectionStatus = isConnected
                    ? "Подключение активно"
                    : "Не удалось подключиться к таблице";
            }
            catch (Exception ex)
            {
                ConnectionValid = false;
                ConnectionStatus = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UploadAndVerifyCredentials()
        {
            try
            {
                var fileResult = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите credentials.json",
                });

                if (fileResult == null) return;

                if (!fileResult.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Пожалуйста, выберите файл с расширением .json", "OK");
                    return;
                }

                IsBusy = true;
                ConnectionStatus = "Загрузка и проверка credentials...";

                string json;
                using (var stream = await fileResult.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    json = await reader.ReadToEndAsync();
                }

                // Временная загрузка для проверки
                if (!await _googleService.UploadNewCredentials(json))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Неверный формат файла credentials.json", "OK");
                    return;
                }

                // Проверка подключения с новыми credentials
                ConnectionStatus = "Проверка подключения с новыми credentials...";
                var testUrl = Preferences.Get("SpreadsheetId", null);

                if (!string.IsNullOrEmpty(testUrl))
                {
                    var isConnected = await _googleService.TestConnectionAsync();
                    if (!isConnected)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Ошибка", "Не удалось установить подключение с новыми credentials", "OK");
                        return;
                    }
                }

                // Финальная загрузка
                if (await _googleService.UploadNewCredentials(json, true))
                {
                    CurrentServiceAccount = _googleService.CurrentServiceAccount;
                    await Application.Current.MainPage.DisplayAlert(
                        "Успех", "Файл успешно загружен и проверен", "OK");

                    // Проверяем подключение после успешной загрузки
                    await CheckConnection();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Не удалось сохранить credentials", "OK");
                }
            }
            catch (UnauthorizedAccessException)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Требуется разрешение",
                    "Разрешите доступ к хранилищу в настройках приложения",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка", $"Не удалось загрузить файл: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadCurrentSettings()
        {
            var spreadsheetId = Preferences.Get("SpreadsheetId", null);
            SpreadsheetUrl = string.IsNullOrEmpty(spreadsheetId)
                ? null
                : $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/edit";
        }

        private async Task OnSave()
        {
            try
            {
                IsBusy = true;
                ConnectionStatus = "Сохранение настроек...";

                var spreadsheetId = ExtractSpreadsheetIdFromUrl(SpreadsheetUrl);
                Preferences.Set("SpreadsheetId", spreadsheetId);

                if (await _googleService.TestConnectionAsync(spreadsheetId))
                {
                    _googleService.ResetAuthSettings();
                    (App.Current.MainPage as AppShell)?.UpdateFlyoutBehavior();
                    await Application.Current.MainPage.DisplayAlert(
                        "Успех", "Настройки сохранены и проверены", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Не удалось подключиться к таблице", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnCancel() => await Shell.Current.GoToAsync("//LoginPage");

        private async Task OpenSpreadsheet()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(SpreadsheetUrl))
                {
                    await Launcher.OpenAsync(new Uri(SpreadsheetUrl));
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Ссылка на документ не указана", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка", $"Не удалось открыть документ: {ex.Message}", "OK");
            }
        }

        private string ExtractSpreadsheetIdFromUrl(string url)
        {
            var uri = new Uri(url);
            if (uri.Host != "docs.google.com")
                throw new Exception("Неверный URL Google Sheets");

            var segments = uri.Segments;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "d/")
                {
                    return segments[i + 1].TrimEnd('/');
                }
            }

            throw new Exception("Не удалось извлечь ID документа");
        }
    }
}