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
        private readonly GoogleService _googleService = new GoogleService();
        private string _username;
        private string _password;

        public Command LoginCommand { get; }
        public Command GoToSettingsCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(async () => await OnLoginClicked());
            GoToSettingsCommand = new Command(async () => await OnGoToSettings());
            InitializeAsync();
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ObservableCollection<LogPass> LogPasses { get; } = new ObservableCollection<LogPass>();

        private async void InitializeAsync()
        {
            await LoadDataAsync();
        }

        private async Task OnGoToSettings()
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }

        public async Task LoadDataAsync()
        {
            try
            {
                LogPasses.Clear();
                var sheetData = await _googleService.GetSheetDataAsync();


                foreach (var row in sheetData)
                {
                    LogPasses.Add(new LogPass
                    {
                        Id = row[0]?.ToString() ?? null,
                        Login = (row.Count > 1) ? row[1]?.ToString() ?? null : null,
                        Password = (row.Count > 2) ? row[2]?.ToString() ?? null : null,
                    });
                }
                LogPasses.Remove(LogPasses[0]);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
                await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
            }
        }

        private async Task OnLoginClicked()
        {
            try
            {
                if (!await _googleService.HasValidSettings())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Требуется настройка",
                        "Пожалуйста, сначала настройте подключение к Google Sheets",
                        "OK");
                    await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
                    return;
                }

                if (LogPasses.Any(i => i.Login == Username && i.Password == Password))
                {
                    Preferences.Set("IsLoggedIn", true);
                    string accountId = LogPasses
                        .FirstOrDefault(i => i.Login == Username && i.Password == Password)?
                        .Id.ToString();
                    Preferences.Set("AccountId", accountId ?? string.Empty);

                    (App.Current.MainPage as AppShell)?.UpdateFlyoutBehavior();
                    await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Неверный логин или пароль!", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}
