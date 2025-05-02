using System;
using System.Collections.Generic;
using MyApp.ViewModels;
using MyApp.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            // Сброс состояния входа
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("AccountId", null);

            // Переход на страницу входа
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
