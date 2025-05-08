using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using MyApp.Services;
using System.Linq;
using System;
using MyApp.Models;
using Xamarin.Essentials;

namespace MyApp.ViewModels
{
    public class InventoryViewModel : BaseViewModel
    {
        private readonly GoogleService _googleService = new GoogleService();
        private string _selectedSheet;

        public ObservableCollection<string> SheetNames { get; } = new ObservableCollection<string>();//Названия помщений
        public ObservableCollection<InventoryField> InventoryFields { get; } = new ObservableCollection<InventoryField>();//Поля форм

        public string SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                if (SetProperty(ref _selectedSheet, value))
                {
                    // Загружаем структуру при смене выбранного листа
                    _ = LoadSheetStructureAsync();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public InventoryViewModel()
        {
            LoadSheetNamesCommand = new Command(async () => await LoadSheetNamesAsync());
        }

        public Command LoadSheetNamesCommand { get; }

        private async Task LoadSheetNamesAsync()
        {
            try
            {
                IsLoading = true;

                var sheetTitles = await _googleService.GetSheetTitlesAsync();

                SheetNames.Clear();
                foreach (var title in sheetTitles.Where(t => !string.Equals(t, "Authorization", StringComparison.OrdinalIgnoreCase)))
                {
                    SheetNames.Add(title);
                }

                if (SheetNames.Count > 0)
                {
                    SelectedSheet = SheetNames[0];
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось загрузить список помещений: {ex.Message}", "OK");
            }
        }
        public async Task LoadSheetStructureAsync()
        {
            try
            {
                IsLoading = true;

                var service = new GoogleService();
                var currentSpreadsheetId = Preferences.Get("SpreadsheetId", null);
                var sheetName = SelectedSheet;

                var range = $"{sheetName}!A1:Z2"; // Читаем 1 и 2 строку
                var serviceData = await service.GetRangeValuesAsync(currentSpreadsheetId, range);

                InventoryFields.Clear();

                if (serviceData.Count >= 2)
                {
                    var firstRow = serviceData[0]; // Флаги включения (!)
                    var secondRow = serviceData[1]; // Названия полей

                    for (int i = 0; i < secondRow.Count; i++)
                    {
                        var skip = i < firstRow.Count && firstRow[i]?.ToString().Trim() == "!";
                        var title = secondRow[i]?.ToString();

                        if (!skip && !string.IsNullOrWhiteSpace(title))
                        {
                            InventoryFields.Add(new InventoryField { Label = title });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка при загрузке структуры формы: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

    }
}
