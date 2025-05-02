using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp.Services
{
    public class GoogleService
    {
        public class GoogleServiceAccountCreds
        {
            public string private_key { get; set; }
            public string client_email { get; set; }
        }

        // Константа для диапазона по умолчанию
        private const string DefaultRange = "Authorization!A:C";

        // Константа для ID таблицы по умолчанию
        private const string DefaultSpreadsheetId = "1QCxjyn23nYLnRcwS-3hvFwq0qd6_t9hnhz6NlQ1iX64";

        public async Task<IList<IList<object>>> GetSheetDataAsync()
        {
            try
            {
                // Получаем текущие настройки
                var currentSpreadsheetId = Preferences.Get("SpreadsheetId", DefaultSpreadsheetId);

                // Загрузка credentials из ресурсов
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(GoogleService)).Assembly;
                using (var stream = assembly.GetManifestResourceStream("MyApp.Services.credentials.json"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync();
                        var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

                        // Аутентификация
                        var credential = new ServiceAccountCredential(
                            new ServiceAccountCredential.Initializer(creds.client_email)
                            {
                                Scopes = new[] { SheetsService.Scope.Spreadsheets }
                            }.FromPrivateKey(creds.private_key));

                        // Создание сервиса
                        var service = new SheetsService(new BaseClientService.Initializer
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "MyApp",
                        });

                        // Запрос данных с текущим spreadsheetId
                        var request = service.Spreadsheets.Values.Get(currentSpreadsheetId, DefaultRange);
                        var response = await request.ExecuteAsync();

                        return response.Values ?? new List<IList<object>>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных: {ex}");
                throw;
            }
        }

        // Метод для проверки подключения с возможностью указания ID таблицы
        public async Task<bool> TestConnectionAsync(string spreadsheetId = null)
        {
            try
            {
                // Используем переданный ID или из настроек
                var currentSpreadsheetId = spreadsheetId ?? Preferences.Get("SpreadsheetId", DefaultSpreadsheetId);

                // Загрузка credentials из ресурсов
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(GoogleService)).Assembly;
                using (var stream = assembly.GetManifestResourceStream("MyApp.Services.credentials.json"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync();
                        var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

                        // Аутентификация
                        var credential = new ServiceAccountCredential(
                            new ServiceAccountCredential.Initializer(creds.client_email)
                            {
                                Scopes = new[] { SheetsService.Scope.Spreadsheets }
                            }.FromPrivateKey(creds.private_key));

                        // Создание сервиса
                        var service = new SheetsService(new BaseClientService.Initializer
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "MyApp",
                        });

                        // Простой запрос метаданных для проверки доступа
                        var request = service.Spreadsheets.Get(currentSpreadsheetId);
                        var response = await request.ExecuteAsync();

                        return response != null;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}