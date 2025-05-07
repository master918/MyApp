using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using MyApp.Services;
using MyApp.Views;

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

            if (!isSettingsOk)
            {
                // Перенаправляем в настройки — напрямую
                MainPage = new NavigationPage(new SettingsPage());
            }
            else if (!isLoggedIn)
            {
                // Пользователь не вошел — показываем LoginPage напрямую
                MainPage = new NavigationPage(new LoginPage());
            }
            else
            {
                // Все в порядке — запускаем AppShell
                MainPage = new AppShell();
            }
        }

        private bool IsSettingsRequired()
        {
            var hasCredentials = SecureStorage.GetAsync(CredentialsKey).Result != null;
            var hasSpreadsheetId = !string.IsNullOrEmpty(Preferences.Get("SpreadsheetId", null));
            return !hasCredentials || !hasSpreadsheetId;
        }

        protected override void OnStart() { }

        protected override void OnSleep() { }

        protected override void OnResume() { }
    }
}
