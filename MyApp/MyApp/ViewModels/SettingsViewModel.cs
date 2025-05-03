using MyApp.Services;
using MyApp.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace MyApp.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly GoogleService _googleService = new GoogleService();
        private string _currentServiceAccount;
        private string _spreadsheetUrl;

        public SettingsViewModel()
        {
            CurrentServiceAccount = _googleService.CurrentServiceAccount;
            LoadCurrentSettings();

            SaveCommand = new Command(async () => await OnSave());
            CancelCommand = new Command(async () => await OnCancel());
            OpenSpreadsheetCommand = new Command(async () => await OpenSpreadsheet());
            TestConnectionCommand = new Command(async () => await TestConnection());
            UploadCredentialsCommand = new Command(async () => await UploadCredentials());

            CheckInitialSetup();
        }

        private async void CheckInitialSetup()
        {
            if (!await _googleService.HasValidSettings())
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Требуется настройка",
                    "Пожалуйста, настройте подключение к Google Sheets",
                    "OK");
            }
        }

        private void LoadCurrentSettings()
        {
            var spreadsheetId = Preferences.Get("SpreadsheetId", null);
            SpreadsheetUrl = string.IsNullOrEmpty(spreadsheetId)
                ? null
                : $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/edit";
        }

        public string SpreadsheetUrl
        {
            get => _spreadsheetUrl;
            set => SetProperty(ref _spreadsheetUrl, value);
        }
       
        public string CurrentServiceAccount
        {
            get => _currentServiceAccount;
            set => SetProperty(ref _currentServiceAccount, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenSpreadsheetCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand UploadCredentialsCommand { get; }


        private async Task UploadCredentials()
        {
            try
            {
                var fileResult = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите credentials.json",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.UWP, new[] { ".json" } },
                    { DevicePlatform.macOS, new[] { "json" } },
                        })
                });

                if (fileResult == null) return;

                if (!fileResult.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Пожалуйста, выберите файл с расширением .json", "OK");
                    return;
                }

                IsBusy = true;

                string json;
                using (var stream = await fileResult.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    json = await reader.ReadToEndAsync();
                }

                // Временное сохранение для проверки
                var tempSuccess = await _googleService.UploadNewCredentials(json);
                if (!tempSuccess)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Неверный формат файла credentials.json", "OK");
                    return;
                }

                // Проверка подключения с новыми credentials
                var testUrl = Preferences.Get("SpreadsheetId", null);
                if (!string.IsNullOrEmpty(testUrl))
                {
                    var isConnected = await _googleService.TestConnectionAsync();
                    if (!isConnected)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Ошибка",
                            "Не удалось установить подключение к Google Sheets с этими credentials",
                            "OK");
                        return;
                    }
                }

                // Если проверка прошла успешно - сохраняем окончательно
                var finalSuccess = await _googleService.UploadNewCredentials(json, true);
                if (finalSuccess)
                {
                    CurrentServiceAccount = _googleService.CurrentServiceAccount;
                    await Application.Current.MainPage.DisplayAlert(
                        "Успех", "Файл credentials.json успешно загружен и проверен", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка", "Не удалось сохранить credentials", "OK");
                }
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



        private async Task OnSave()
        {
            try
            {
                IsBusy = true;
                var spreadsheetId = ExtractSpreadsheetIdFromUrl(SpreadsheetUrl);
                Preferences.Set("SpreadsheetId", spreadsheetId);

                if (await _googleService.TestConnectionAsync(spreadsheetId))
                {
                    _googleService.ResetAuthSettings();
                    (App.Current.MainPage as AppShell)?.UpdateFlyoutBehavior();

                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Настройки сохранены и подключение проверено", "OK");

                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось подключиться к таблице", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnCancel()
        {
            // Всегда возвращаем на страницу авторизации
            await Shell.Current.GoToAsync("//LoginPage");
        }

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
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Ссылка на документ не указана", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось открыть документ: {ex.Message}", "OK");
            }
        }

        private async Task TestConnection()
        {
            try
            {
                IsBusy = true;

                // Извлекаем ID документа из URL
                var spreadsheetId = ExtractSpreadsheetIdFromUrl(SpreadsheetUrl);

                // Проверяем подключение с текущим ID
                var service = new GoogleService();
                var isConnected = await service.TestConnectionAsync(spreadsheetId);

                if (isConnected)
                {
                    // Если подключение успешно, сохраняем новый ID
                    Preferences.Set("SpreadsheetId", spreadsheetId);
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Подключение к Google Sheets успешно установлено", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось подключиться к указанной таблице", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка при проверке подключения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
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