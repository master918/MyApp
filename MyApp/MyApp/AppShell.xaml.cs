using System;
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
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("AccountId", null);
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