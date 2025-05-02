using MyApp.Models;
using MyApp.Services;
using MyApp.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public Command LoginCommand { get; }
        private string username;
        private string password;

        public Command GoToSettingsCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(OnLoginClicked);
            GoToSettingsCommand = new Command(OnGoToSettings);
            InitializeAsync();
        }

        private async void OnGoToSettings()
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }


        public string Username
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ObservableCollection<LogPass> LogPasses { get; } = new ObservableCollection<LogPass>();

        private async void InitializeAsync(){ await LoadDataAsync(); }

        public async Task LoadDataAsync()
        {
            LogPasses.Clear();
            GoogleService service = new GoogleService();
            var sheetData = await service.GetSheetDataAsync(); // Ожидание завершения асинхронного операции

            foreach (var row in sheetData)
            {
                LogPasses.Add(new LogPass
                {
                    Id = row[0]?.ToString() ?? null,
                    Login = (row.Count > 1) ? row[1]?.ToString() ?? null : null,
                    Password = (row.Count > 2) ? row[2]?.ToString() ?? null : null,
                });
            }
        }

        private async void OnLoginClicked(object obj)
        {
            if (LogPasses.Any(i => i.Login == username && i.Password == password))
            {
                Preferences.Set("IsLoggedIn", true);
                string accountId = (from i in LogPasses
                                    where i.Login == username && i.Password == password
                                    select i.Id.ToString()).FirstOrDefault();
                Preferences.Set("AccountId", accountId);

                // Обновляем FlyoutBehavior
                (App.Current.MainPage as AppShell)?.UpdateFlyoutBehavior();

                await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Неверный логин или пароль!", "OK");
            }
        }
    }
}
