using MyApp.Services;
using MyApp.Views;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private string _spreadsheetUrl;
        private bool _isBusy;

        public SettingsViewModel()
        {
            LoadCurrentSettings();

            SaveCommand = new Command(async () => await OnSave());
            CancelCommand = new Command(async () => await OnCancel());
            OpenSpreadsheetCommand = new Command(async () => await OpenSpreadsheet());
            TestConnectionCommand = new Command(async () => await TestConnection());
        }

        private void LoadCurrentSettings()
        {
            // Извлекаем ID документа из Preferences и формируем URL
            var spreadsheetId = Preferences.Get("SpreadsheetId", "1QCxjyn23nYLnRcwS-3hvFwq0qd6_t9hnhz6NlQ1iX64");
            SpreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/edit";
        }

        public string SpreadsheetUrl
        {
            get => _spreadsheetUrl;
            set
            {
                if (SetProperty(ref _spreadsheetUrl, value))
                {
                    // Извлекаем ID документа из URL при изменении
                    if (!string.IsNullOrWhiteSpace(value) &&
                        value.Contains("docs.google.com/spreadsheets/d/"))
                    {
                        var start = value.IndexOf("/d/") + 3;
                        var end = value.IndexOf("/", start);
                        if (end == -1) end = value.Length;
                        var id = value.Substring(start, end - start);
                        Preferences.Set("SpreadsheetId", id);
                    }
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenSpreadsheetCommand { get; }
        public ICommand TestConnectionCommand { get; }

        private async Task OnSave()
        {
            try
            {
                IsBusy = true;

                // Сохраняем настройки
                var spreadsheetId = ExtractSpreadsheetIdFromUrl(SpreadsheetUrl);
                Preferences.Set("SpreadsheetId", spreadsheetId);

                // Проверяем подключение
                var service = new GoogleService();
                if (await service.TestConnectionAsync(spreadsheetId))
                {
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Настройки сохранены и подключение проверено", "OK");

                    // Возвращаем на страницу авторизации
                    await Shell.Current.GoToAsync("//LoginPage");

                    // Обновляем данные
                    var loginVM = (Shell.Current.CurrentPage as LoginPage)?.BindingContext as LoginViewModel;
                    await loginVM?.LoadDataAsync();
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