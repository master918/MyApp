using MyApp.Items;
using MyApp.Services;
using MyApp.ViewModels;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
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
            try
            {
                InitializeComponent();
                var googleService = DependencyService.Get<GoogleService>();
                var repository = new InventoryRepository(new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "Myapp.db")));
                BindingContext = new InventoryViewModel(googleService, repository);
            }
            catch (Exception ex)
            {
                // Логируем ошибку или показываем
                System.Diagnostics.Debug.WriteLine($"Error in constructor: {ex}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var vm = BindingContext as InventoryViewModel;
            if (vm != null)
            {
                // Загружаем данные после загрузки страницы
                if (vm.LoadSheetsCommand.CanExecute(null))
                {
                    vm.LoadSheetsCommand.Execute(null);
                }
                    

                // Инкрементируем задержку, чтобы все успело загрузиться
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(200);
                });
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<InventoryViewModel>(this, "StartScanner");
        }

        private void OnNameEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is InventoryField nameField && nameField.IsNameField)
            {
                
                // Скрыть подсказки
                nameField.SuggestionsVisible = false;

                // Обновить видимость других полей
                if (BindingContext is InventoryViewModel vm)
                {
                    vm.UpdateFieldVisibility(nameField.Value);
                }
            }
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