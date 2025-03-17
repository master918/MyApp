using MyApp.Views;
using System;
using System.Collections.Generic;
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

        public LoginViewModel()
        {
            LoginCommand = new Command(OnLoginClicked);
        }

        private async void OnLoginClicked(object obj)
        {        
            // Пример проверки логина и пароля
            if (username == "" && password == "") //
            {
                // Сохранение состояния входа
                Preferences.Set("IsLoggedIn", true);
                Username = "";
                Password = "";
                await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
            else 
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Неверный логин или пароль!", "OK");
            }
        }
    }
}
