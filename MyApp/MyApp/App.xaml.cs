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
        public App()
        {
            InitializeComponent();
            DependencyService.Register<MockDataStore>();

            // Устанавливаем начальную страницу
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            if (Preferences.Get("IsLoggedIn", false))
            {
                Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
            else
            {
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
