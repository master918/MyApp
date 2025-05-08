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
	public partial class InventoryPage : ContentPage
	{
		public InventoryPage ()
		{
			InitializeComponent ();
			BindingContext = new InventoryViewModel();
		}
        protected override void OnAppearing()
        {
            base.OnAppearing();

            BindingContext = new InventoryViewModel();
            if (BindingContext is InventoryViewModel vm)
            {
                if (vm.LoadSheetNamesCommand.CanExecute(null))
                    vm.LoadSheetNamesCommand.Execute(null);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(200);
                });
            }
        }

    }
}