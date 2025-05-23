using MyApp.Services;
using MyApp.Views;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            UpdateFlyoutBehavior();
            
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            var clearUserTask = LocalDbService.ClearUser();

            // Безопасно вызываем ResetAuthAndNavigate в UI потоке
            var resetTask = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ResetAuthAndNavigate();
            });

            await Task.WhenAll(clearUserTask, resetTask);
        }


        public async Task ResetAuthAndNavigate()
        {
            Preferences.Set("IsLoggedIn", false);
            UpdateFlyoutBehavior();

            await Shell.Current.GoToAsync("//LoginPage");
        }

        public void UpdateFlyoutBehavior()
        {
            FlyoutBehavior = Preferences.Get("IsLoggedIn", false)
                ? FlyoutBehavior.Flyout
                : FlyoutBehavior.Disabled;
        }
    }
}