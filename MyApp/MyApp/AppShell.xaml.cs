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

        private void OnMenuItemClicked(object sender, EventArgs e)
        {
            ResetAuthAndNavigate();
        }

        public void ResetAuthAndNavigate()
        {
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("AccountId", string.Empty);
            UpdateFlyoutBehavior();
            Shell.Current.GoToAsync("//LoginPage");
        }

        public void UpdateFlyoutBehavior()
        {
            FlyoutBehavior = Preferences.Get("IsLoggedIn", false)
                ? FlyoutBehavior.Flyout
                : FlyoutBehavior.Disabled;
        }
    }
}