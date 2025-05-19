using MyApp.Items;
using MyApp.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Mobile;

namespace MyApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InventoryPage : ContentPage
    {
        public InventoryPage()
        {
            InitializeComponent();
            BindingContext = new InventoryViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var vm = BindingContext as InventoryViewModel;
            if (vm != null)
            {
                // Загружаем данные после загрузки страницы
                if (vm.LoadSheetNamesCommand.CanExecute(null))
                    vm.LoadSheetNamesCommand.Execute(null);

                // Инкрементируем задержку, чтобы все успело загрузиться
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(200);
                });
            }

            MessagingCenter.Subscribe<InventoryViewModel>(this, "StartScanner", (sender) =>
            {
                StartScanner();
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<InventoryViewModel>(this, "StartScanner");
        }

        //private void Handle_OnScanResult(ZXing.Result result)
        //{
        //    Device.BeginInvokeOnMainThread(async () =>
        //    {
        //        scannerView.IsScanning = false;
        //        scannerView.IsVisible = false;

        //        if (!string.IsNullOrWhiteSpace(result.Text))
        //        {
        //            var vm = BindingContext as InventoryViewModel;
        //            await vm?.HandleScannedText(result.Text);
        //        }
        //    });
        //}

        private void StartScanner()
        {
            scannerView.IsVisible = true;
            scannerView.IsScanning = true;
        }

        private void OnEntryFocused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is InventoryField field)
            {
                field.FilterSuggestions(entry.Text);
            }
        }
        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is InventoryField field)
            {
                field.FilterSuggestions(e.NewTextValue);
            }
        }
        private void OnEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is InventoryField field)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(200);
                    field.SuggestionsVisible = false;
                    field.OnPropertyChanged(nameof(field.SuggestionsVisible));
                });
            }
        }

        void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (sender is ListView listView &&
                listView.BindingContext is InventoryField field &&
                e.SelectedItem is string selectedItem)
            {
                field.Value = selectedItem;
                field.SuggestionsVisible = false;

                // Сбросим выделение
                listView.SelectedItem = null;
            }
        }
    }
}