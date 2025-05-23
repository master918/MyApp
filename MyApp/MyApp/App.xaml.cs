using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using MyApp.Views;
using MyApp.Services;

namespace MyApp
{
    public partial class App : Application
    {
        private const string CredentialsKey = "GoogleServiceCredentials";

        public App()
        {
            InitializeComponent();
            InitializeDefaultSettings();//Настройки по усолчанию при первом запуске после установки
            SetMainPage();//Установка главной страницы
            LoadFromDB();//Загрузка из БД если пользователь авторизован
        }

        private void InitializeDefaultSettings()
        {
            if (!Preferences.ContainsKey("FirstRun"))
            {
                Preferences.Set("FirstRun", false);
                Preferences.Set("IsLoggedIn", false);
                Preferences.Set("SpreadsheetId", string.Empty);
                SecureStorage.Remove(CredentialsKey);
            }
        }
        private void SetMainPage()
        {
            // Пока не знаем, что нужно показывать — ставим заглушку
            MainPage = new AppShell();

            // Навигация будет выполнена после полной инициализации
            Device.BeginInvokeOnMainThread(async () => await CheckAndNavigateAsync());
        }
        private void LoadFromDB()
        {
            if (Preferences.ContainsKey("FirstRun") && Preferences.Get("IsLoggedIn", false))
            {

            }
        }


        protected override async void OnStart()
        {
            await CheckAndNavigateAsync();

        }

        protected override async void OnResume()
        {
            await CheckAndNavigateAsync();
        }

        protected override void OnSleep()
        {
            // Здесь можно сохранять состояние приложения, если нужно
        }

        private async Task CheckAndNavigateAsync()
        {
            try
            {
                if (await IsSettingsRequiredAsync())
                {
                    await Shell.Current.GoToAsync("//SettingsPage");
                }
                else if (Preferences.Get("IsLoggedIn", false))
                {
                    await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
                }
                else
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async Task<bool> IsSettingsRequiredAsync()
        {
            try
            {
                var hasCredentials = await SecureStorage.GetAsync(CredentialsKey) != null;
                var hasSpreadsheetId = !string.IsNullOrEmpty(Preferences.Get("SpreadsheetId", null));
                return !hasCredentials || !hasSpreadsheetId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage error: {ex.Message}");
                return true;
            }
        }
    }
}
