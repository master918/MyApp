using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using MyApp.Services;
using MyApp.Views;
using Xamarin.Essentials;
using MyApp.Models;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace MyApp
{
    public partial class App : Application
    {
        private const string CredentialsKey = "GoogleServiceCredentials";

        public App()
        {
            InitializeComponent();
            InitializeDefaultSettings();

            // Устанавливаем стартовую страницу
            SetMainPage();
        }

        private void InitializeDefaultSettings()
        {
            if (!Preferences.ContainsKey("FirstRun"))
            {
                Preferences.Set("FirstRun", false);
                Preferences.Set("IsLoggedIn", false);
                Preferences.Set("AccountId", string.Empty);
                Preferences.Set("SpreadsheetId", string.Empty);
                SecureStorage.Remove(CredentialsKey);
            }
        }

        private void SetMainPage()
        {
            bool isSettingsOk = !IsSettingsRequired();
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            // Проверяем состояние и выполняем переходы в нужное место
            await CheckAndNavigateAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (Shell.Current != null)
            {
                _ = CheckAndNavigateAsync(); // Подавляем предупреждение намеренно
            }
        }

        private async Task CheckAndNavigateAsync()
        {
            // Ожидаем результата выполнения IsSettingsRequiredAsync()
            if (await IsSettingsRequiredAsync())
            {
                await Shell.Current.GoToAsync("//SettingsPage");
            }
            else if (Preferences.Get("IsLoggedIn", false))
            else if (Preferences.Get("IsLoggedIn", false))
                // Если пользователь авторизован, переходим на страницу AboutPage
                await Shell.Current.GoToAsync("//AboutPage");
                await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
            else
                // Если не авторизован, показываем страницу входа (LoginPage)
                await Shell.Current.GoToAsync("//LoginPage");
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
        }
        private async Task<bool> IsSettingsRequiredAsync()
        {
            try
            {
                // Проверяем наличие сохраненных учетных данных и идентификатора таблицы
                var hasCredentials = await SecureStorage.GetAsync(CredentialsKey) != null;
                var hasSpreadsheetId = !string.IsNullOrEmpty(Preferences.Get("SpreadsheetId", null));
                return !hasCredentials || !hasSpreadsheetId;
            }
            catch (Exception ex)
            {
                // В случае ошибки доступа к SecureStorage — считаем, что настройки требуются
                System.Diagnostics.Debug.WriteLine($"SecureStorage error: {ex.Message}");
                return true;
            }
        }
        }
        protected override void OnSleep()
        {
            base.OnSleep();
        }
        protected override void OnResume() { }        
    }
}
