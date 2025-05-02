using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
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

        public async Task<IList<IList<object>>> GetSheetDataAsync()
        {
            try
            {
                // 1. Загрузка credentials.json из ресурсов
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(GoogleService)).Assembly;
                using (var stream = assembly.GetManifestResourceStream("MyApp.Services.credentials.json"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync();
                        var creds = JsonConvert.DeserializeObject<GoogleServiceAccountCreds>(json);

                        // 2. Аутентификация
                        var credential = new ServiceAccountCredential(
                            new ServiceAccountCredential.Initializer(creds.client_email)
                            {
                                Scopes = new[] { SheetsService.Scope.Spreadsheets }
                            }.FromPrivateKey(creds.private_key));


                        // 3. Создание сервиса
                        var service = new SheetsService(new BaseClientService.Initializer
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "MyApp",
                        });


                        // 4. Запрос данных
                        string spreadsheetId = "1QCxjyn23nYLnRcwS-3hvFwq0qd6_t9hnhz6NlQ1iX64";
                        string range = "Authorization!A:C";
                        var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

                        var response = await request.ExecuteAsync();
                        return response.Values ?? new List<IList<object>>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex}");
                throw;
            }
        }
    }
}