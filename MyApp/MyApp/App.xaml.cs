using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MyApp.Services;
using MyApp.Views;
using Xamarin.Essentials;
using MyApp.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyApp
{
    public partial class App : Application
    {
        private const string CredentialsKey = "GoogleServiceCredentials";
        public App()
        {
            InitializeComponent();
            // Инициализация начальных настроек
            InitializeDefaultSettings();
            // Устанавливаем начальную страницу
            MainPage = new AppShell();
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        }

        private void InitializeDefaultSettings()
        {
            // Устанавливаем флаг первого запуска
            if (!Preferences.ContainsKey("FirstRun"))
            {
                Preferences.Set("FirstRun", false);
                Preferences.Set("IsLoggedIn", false);
                Preferences.Set("AccountId", string.Empty);
                Preferences.Set("SpreadsheetId", string.Empty);
                SecureStorage.Remove(CredentialsKey);
            }
        }

        protected override async void OnStart()
        {
            if (Shell.Current == null) return;

            // Проверяем необходимость перенаправления в настройки
            if (IsSettingsRequired())
            {
                await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
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

        private bool IsSettingsRequired()
        {
            var hasCredentials = SecureStorage.GetAsync(CredentialsKey).Result != null;
            var hasSpreadsheetId = !string.IsNullOrEmpty(Preferences.Get("SpreadsheetId", null));

            return !hasCredentials || !hasSpreadsheetId;
        }

        protected override void OnSleep() { }

        protected override void OnResume() { }        
    }
}
