using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MyApp.Services;
using MyApp.Views;
using Xamarin.Essentials;

namespace MyApp
{
    public partial class App : Application
    {

        public App ()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();

            if (Preferences.Get("IsLoggedIn", false))
            {
                // Если пользователь залогинен, переходим на AboutPage
                Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
            else
            {
                // Если пользователь не залогинен, показываем LoginPage
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
        }

        protected override void OnStart ()
        {

        }

        protected override void OnSleep ()
        {
        }

        protected override void OnResume ()
        {
        }
    }
}
