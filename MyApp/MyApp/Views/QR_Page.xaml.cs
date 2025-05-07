using MyApp.Models;
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
    public partial class QR_Page : ContentPage
    {
        public QR_Page()
        {
            InitializeComponent();
            BindingContext = new QR_PageViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Сброс состояния при возвращении на страницу
            if (BindingContext is QR_PageViewModel vm)
            {
                vm.ShowStorageSelection = true;
                vm.ShowInputMethod = false;
                vm.ShowManualInput = false;
                vm.CurrentItem = new InventoryItem();
                vm.SelectedStorage = null;
                vm.OnAppearing();
            }
        }
    }
}