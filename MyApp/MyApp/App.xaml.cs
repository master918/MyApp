using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MyApp.Services;
using MyApp.Views;
using Xamarin.Essentials;
using MyApp.Models;
using System.Collections.ObjectModel;

namespace MyApp
{
    public partial class App : Application
    {
        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public App()
        {
            InitializeComponent();

            LoadDataAsync();


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

        private async void LoadDataAsync()
        {
            GoogleService service = new GoogleService();
            var sheetData = await service.GetSheetDataAsync(); // Ожидание завершения асинхронного операции

            foreach (var row in sheetData) // Теперь sheetData - это IList<IList<object>>
            {
                Items.Add(new Item
                {
                    Id = row[0]?.ToString() ?? string.Empty,
                    Text = row[1]?.ToString() ?? string.Empty,
                });
            }
        }

        protected override async void OnStart()
        {
            // Проверяем оба типа авторизации
            if (Preferences.Get("IsLoggedIn", false))
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new LoginPage();
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
