using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MyApp.Models;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;


namespace MyApp.Services
{
    public class GoogleService
    {
        public class GoogleServiceAccountCreds
        {
            public string type { get; set; }
            public string project_id { get; set; }
            public string private_key_id { get; set; }
            public string private_key { get; set; }
            public string client_email { get; set; }
            public string client_id { get; set; }
            public string auth_uri { get; set; }
            public string token_uri { get; set; }
            public string auth_provider_x509_cert_url { get; set; }
            public string client_x509_cert_url { get; set; }
        }

        private const string DefaultRange = "Authorization!A:C";
        private const string CredentialsFileName = "credentials.json";
        private const string CredentialsKey = "GoogleServiceCredentials";

        public Task<bool> HasValidSettings()
        {
            var hasCredentials = !string.IsNullOrEmpty(GetCredentialsJson());
            var hasSpreadsheetId = !string.IsNullOrEmpty(Preferences.Get("SpreadsheetId", null));

            return Task.FromResult(hasCredentials && hasSpreadsheetId);
        }

        public void ResetAuthSettings()
        {
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("AccountId", string.Empty);
        }

        public string CurrentServiceAccount { get; private set; }

        public GoogleService()
        {
            TryLoadEmbeddedCredentials();
            LoadCurrentServiceAccount();
        }

        private void TryLoadEmbeddedCredentials()
        {
            try
            {
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(GoogleService)).Assembly;
                using (var stream = assembly.GetManifestResourceStream($"MyApp.Services.{CredentialsFileName}"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            // Сохраняем в SecureStorage
                            SecureStorage.SetAsync(CredentialsKey, json).Wait();
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки при загрузке встроенного файла
            }
        }
        private void LoadCurrentServiceAccount()
        {
            try
            {
                var json = GetCredentialsJson();
                if (!string.IsNullOrEmpty(json))
                {
                    var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);
                    CurrentServiceAccount = creds.client_email;
                }
                else
                {
                    CurrentServiceAccount = "Не настроен";
                }
            }
            catch
            {
                CurrentServiceAccount = "Ошибка загрузки";
            }
        }

        public async Task<bool> UploadNewCredentials(string json, bool permanentSave = false)
        {
            try
            {
                var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);
                if (creds?.client_email == null)
                    return false;

                await SecureStorage.SetAsync(CredentialsKey, json);
                CurrentServiceAccount = creds.client_email;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetCredentialsJson()
        {
            // Затем проверяем сохраненные в SecureStorage
            var secureStorageTask = SecureStorage.GetAsync(CredentialsKey);
            secureStorageTask.Wait(); // Блокируем, так как в конструкторе нельзя async
            var secureCredentials = secureStorageTask.Result;

            if (!string.IsNullOrEmpty(secureCredentials))
                return secureCredentials;

            // Затем проверяем локальный файл (если сохраняли)
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CredentialsFileName);
            if (File.Exists(localPath))
                return File.ReadAllText(localPath);

            // Если нет сохраненных, используем встроенный файл
            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(GoogleService)).Assembly;
            using (var stream = assembly.GetManifestResourceStream($"MyApp.Services.{CredentialsFileName}"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public async Task<IList<IList<object>>> GetSheetDataAsync()
        {
            try
            {
                var currentSpreadsheetId = Preferences.Get("SpreadsheetId", null);
                if (string.IsNullOrEmpty(currentSpreadsheetId))
                {
                    throw new InvalidOperationException("Не указана ссылка на документ");
                }

                var json = GetCredentialsJson();
                if (string.IsNullOrEmpty(json))
                {
                    throw new InvalidOperationException("Реквизиты Service account не найдены");
                }

                var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(creds.client_email)
                    {
                        Scopes = new[] { SheetsService.Scope.Spreadsheets }
                    }.FromPrivateKey(creds.private_key));

                var service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MyApp",
                });

                var request = service.Spreadsheets.Values.Get(currentSpreadsheetId, DefaultRange);
                var response = await request.ExecuteAsync();

                return response.Values ?? new List<IList<object>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных: {ex}");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(string spreadsheetId = null)
        {
            try
            {
                var currentSpreadsheetId = spreadsheetId ?? Preferences.Get("SpreadsheetId", null);
                if (string.IsNullOrEmpty(currentSpreadsheetId))
                {
                    return false;
                }

                var json = GetCredentialsJson();
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(creds.client_email)
                    {
                        Scopes = new[] { SheetsService.Scope.Spreadsheets }
                    }.FromPrivateKey(creds.private_key));

                var service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MyApp",
                });

                var request = service.Spreadsheets.Get(currentSpreadsheetId);
                var response = await request.ExecuteAsync();

                return response != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetSheetTitlesAsync()
        {
            var currentSpreadsheetId = Preferences.Get("SpreadsheetId", null);
            if (string.IsNullOrEmpty(currentSpreadsheetId))
            {
                throw new InvalidOperationException("SpreadsheetId не указан.");
            }

            var json = GetCredentialsJson();
            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException("Учетные данные не найдены.");
            }

            var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

            var credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(creds.client_email)
                {
                    Scopes = new[] { SheetsService.Scope.SpreadsheetsReadonly }
                }.FromPrivateKey(creds.private_key));

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MyApp",
            });

            var spreadsheet = await service.Spreadsheets.Get(currentSpreadsheetId).ExecuteAsync();
            return spreadsheet.Sheets.Select(s => s.Properties.Title).ToList();
        }
        public async Task<IList<IList<object>>> GetRangeValuesAsync(string spreadsheetId, string range)
        {
            var json = GetCredentialsJson();
            if (string.IsNullOrEmpty(json))
                throw new InvalidOperationException("Учетные данные не найдены");

            var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

            var credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(creds.client_email)
                {
                    Scopes = new[] { SheetsService.Scope.SpreadsheetsReadonly }
                }.FromPrivateKey(creds.private_key));

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MyApp",
            });

            var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            return response.Values ?? new List<IList<object>>();
        }

        public async Task<List<string>> GetStorageListAsync()
        {
            try
            {
                var spreadsheetId = Preferences.Get("SpreadsheetId", null);
                if (string.IsNullOrEmpty(spreadsheetId))
                    throw new InvalidOperationException("Не указан ID таблицы");

                var json = GetCredentialsJson();
                if (string.IsNullOrEmpty(json))
                    throw new InvalidOperationException("Реквизиты не найдены");

                var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);
                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(creds.client_email)
                    {
                        Scopes = new[] { SheetsService.Scope.Spreadsheets }
                    }.FromPrivateKey(creds.private_key));

                var service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MyApp",
                });

                var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
                var storageList = new List<string>();

                foreach (var sheet in spreadsheet.Sheets)
                {
                    if (sheet.Properties.Title != "Authorization")
                    {
                        storageList.Add(sheet.Properties.Title);
                    }
                }

                return storageList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении списка хранилищ: {ex}");
                throw;
            }
        }
        public async Task UploadInventoryData(InventoryItem currentItem)
        {
            var items = await DependencyService.Get<IDataStore<InventoryItem>>().GetItemsAsync();
            var spreadsheetId = Preferences.Get("SpreadsheetId", null);

            if (string.IsNullOrEmpty(spreadsheetId))
            {
                throw new InvalidOperationException("Не указан ID таблицы в настройках");
            }

            var json = GetCredentialsJson();
            var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

            var credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(creds.client_email)
                {
                    Scopes = new[] { SheetsService.Scope.Spreadsheets }
                }.FromPrivateKey(creds.private_key));

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MyApp",
            });

            // Определяем диапазон для записи
            var range = $"{currentItem.StorageName}!A:G";

            // Формируем данные
            var values = new List<IList<object>>();
            foreach (var item in items)
            {
                values.Add(new List<object>
        {
            item.Наименование,
            item.Стеллаж,
            item.Полка,
            item.Место,
            item.Количество_фактич,
            item.Доп_описание,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
            }

            var valueRange = new ValueRange { Values = values };
            var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            await request.ExecuteAsync();
        }
    }
}