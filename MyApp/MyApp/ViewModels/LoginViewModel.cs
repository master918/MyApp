using MyApp.Models;
using MyApp.Services;
using MyApp.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public LoginViewModel()
        {
            LoginCommand = new Command(OnLoginClicked);
        }

        private async Task LoadDataAsync()
        {
            GoogleService service = new GoogleService();
            var sheetData = await service.GetSheetDataAsync(); // Ожидание завершения асинхронного операции

            foreach (var row in sheetData)
            {
                Items.Add(new Item
                {
                    Id = row[0]?.ToString() ?? string.Empty,
                    Text = row[1]?.ToString() ?? string.Empty,
                });
            }
        }

        private async void OnLoginClicked(object obj)
        {
            await LoadDataAsync();
            // Пример проверки логина и пароля
            if (username == "1" && password == "1")
            {
                // Сохранение состояния входа
                Preferences.Set("IsLoggedIn", true);
                Username = null;
                Password = null;
                await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
            else 
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Неверный логин или пароль!", "OK");
            }
        }
    }
}
