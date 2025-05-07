using MyApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyApp.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsPage : ContentPage
	{
		public SettingsPage ()
		{
			InitializeComponent ();
			BindingContext = new SettingsViewModel();
		}

        private async void OnSpreadsheetUrlUnfocused(object sender, FocusEventArgs e)
        {
            if (BindingContext is SettingsViewModel vm)
            {
                await vm.CheckConnectionStatusAsync();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is SettingsViewModel vm)
            {
                await vm.CheckConnectionStatusAsync();
            }
        }
    }

}