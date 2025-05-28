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
            InitializeDefaultSettings();

            // Устанавливаем AppShell сразу
            MainPage = new AppShell();

            // Инициализация и навигация в фоне
            Device.BeginInvokeOnMainThread(async () => await InitializeAppAsync());
        }

        protected override void OnStart() { }

        protected override void OnResume() { }

        protected override void OnSleep() { }

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

        private async Task InitializeAppAsync()
        {
            try
            {
                await LocalDbService.InitializeAsync();

                // Навигация после инициализации
                await NavigateAfterStartupAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App initialization failed: {ex.Message}");
            }
        }

        private async Task NavigateAfterStartupAsync()
        {
            try
            {
                if (await IsSettingsRequiredAsync())
                {
                    await Shell.Current.GoToAsync("//SettingsPage");
                }
                else if (Preferences.Get("IsLoggedIn", false))
                {
                    await Shell.Current.GoToAsync("//AboutPage");
                }
                else
                {
                    await Shell.Current.GoToAsync("//LoginPage");
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
