using MyApp.Items;
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
using Xamarin.Forms.Xaml;

namespace MyApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly GoogleService _googleService = new GoogleService();
        private readonly UserService _userService = new UserService();

        private string _login;
        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public Command LoginCommand { get; }
        public Command GoToSettingsCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(async () => await OnLoginClicked());
            GoToSettingsCommand = new Command(async () => await OnGoToSettings());
        }
        public async Task InitAsync()
        {
            if (!Preferences.Get("IsLoggedIn", false)) 
            {
                IsBusy = true;
                await _userService.GetUsers();
                IsBusy = false;
            }
        }

        //Кнопки
        private async Task OnGoToSettings()
        {
            await Shell.Current.GoToAsync($"///{nameof(SettingsPage)}");
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
                }//Проверка подключения API -> Переход в настройки

                if (!NetworkService.IsConnectedToInternet())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Нет подключения",
                        "Проверьте подключение к интернету и повторите попытку.",
                        "OK");
                    return;
                }//Проверка соединения

                if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите логин и пароль", "OK");
                    return;
                }//Проверка пустых символов

                await _userService.IsAccess(Login, Password);//Попытка получения доступа 
                
                if (Preferences.Get("IsLoggedIn", false))
                {
                   (App.Current.MainPage as AppShell)?.UpdateFlyoutBehavior();
                    await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");//Переход на страницу About
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
