using MyApp.Models;
using MyApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp.ViewModels
{
    public class QR_PageViewModel : BaseViewModel
    {
        private readonly IDataStore<InventoryItem> _dataStore;

        public bool IsNotBusy => !IsBusy;

        private bool _showStorageSelection = true;
        private bool _showInputMethod;
        private bool _showManualInput;
        private string _selectedStorage;

        private ObservableCollection<string> _storageOptions = new ObservableCollection<string>();
        public ObservableCollection<string> StorageOptions
        {
            get => _storageOptions;
            set => SetProperty(ref _storageOptions, value);
        }

        public InventoryItem CurrentItem { get; set; } = new InventoryItem();

        public bool ShowStorageSelection
        {
            get => _showStorageSelection;
            set => SetProperty(ref _showStorageSelection, value);
        }

        public bool ShowInputMethod
        {
            get => _showInputMethod;
            set => SetProperty(ref _showInputMethod, value);
        }

        public bool ShowManualInput
        {
            get => _showManualInput;
            set => SetProperty(ref _showManualInput, value);
        }

        public string SelectedStorage
        {
            get => _selectedStorage;
            set
            {
                if (SetProperty(ref _selectedStorage, value))
                {
                    CurrentItem.StorageName = value;
                    (SelectStorageCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public ICommand SelectStorageCommand { get; set; }
        public ICommand ManualInputCommand { get; set; }
        public ICommand ScanQrCommand { get; set; }
        public ICommand SaveAndContinueCommand { get; set; }
        public ICommand SaveAndFinishCommand { get; set; }

        public QR_PageViewModel()
        {
            _dataStore = DependencyService.Get<IDataStore<InventoryItem>>();
            InitializeCommands();
        }

        public async Task OnAppearing()
        {
            await LoadStorageOptions();
        }

        private async Task LoadStorageOptions()
        {
            try
            {
                IsBusy = true;
                var googleService = DependencyService.Get<GoogleService>();
                var storages = await googleService.GetStorageListAsync();

                StorageOptions.Clear();
                foreach (var storage in storages)
                {
                    StorageOptions.Add(storage);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка",
                    $"Не удалось загрузить список помещений: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InitializeCommands()
        {
            SelectStorageCommand = new Command<string>(
                execute: async (storage) => await SelectStorage(storage),
                canExecute: (storage) => !string.IsNullOrEmpty(storage));

            ManualInputCommand = new Command(async () => await ShowManualInputMethod());
            ScanQrCommand = new Command(async () => await ScanQrCode());
            SaveAndContinueCommand = new Command(async () => await SaveItem(false));
            SaveAndFinishCommand = new Command(async () => await SaveItem(true));
        }

        private async Task SelectStorage(string storage)
        {
            CurrentItem.StorageName = storage;
            ShowStorageSelection = false;
            ShowInputMethod = true;
        }

        private async Task ShowManualInputMethod()
        {
            ShowInputMethod = false;
            ShowManualInput = true;
        }

        private async Task ScanQrCode()
        {
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();
            var result = await scanner.Scan();

            if (result != null)
            {
                var qrData = ParseQrData(result.Text);
                CurrentItem.Наименование = qrData.ContainsKey("Наименование") ? qrData["Наименование"] : "";
                ShowInputMethod = false;
                ShowManualInput = true;
            }
        }

        private Dictionary<string, string> ParseQrData(string qrData)
        {
            var result = new Dictionary<string, string>();
            var pairs = qrData.Split(';');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    result[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            return result;
        }

        private async Task SaveItem(bool finish)
        {
            if (await _dataStore.AddItemAsync(CurrentItem))
            {
                CurrentItem = new InventoryItem { StorageName = CurrentItem.StorageName };

                if (finish)
                {
                    await UploadToGoogleSheets();
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    ShowManualInput = false;
                    ShowInputMethod = true;
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось сохранить данные", "OK");
            }
        }

        private async Task UploadToGoogleSheets()
        {
            try
            {
                var googleService = DependencyService.Get<GoogleService>();
                await googleService.UploadInventoryData(CurrentItem);
                await Shell.Current.DisplayAlert("Успех", "Данные загружены в Google Sheets", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
        }
    }
}