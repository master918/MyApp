using MyApp.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyApp.Services
{
    public class AuthService
    {
        private readonly GoogleService _sheetsService;
        public ObservableCollection<User> UsersList { get; } = new ObservableCollection<User>();

        public AuthService()
        {
            _sheetsService = new GoogleService();
        }

        public async Task LocalUsersList()
        {
            if (!NetworkService.IsConnectedToInternet())
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Нет подключения",
                    "Проверьте подключение к интернету и повторите попытку.",
                    "OK");
                return;
            }

            var users = await _sheetsService.GetUsers();

            UsersList.Clear();

            foreach (var row in users)
            {
                UsersList.Add(new User
                {
                    Id = int.Parse(row[0].ToString()),
                    Login = (row.Count > 1) ? row[1]?.ToString() ?? null : null,
                    Password = (row.Count > 2) ? row[2]?.ToString() ?? null : null,
                    LastEntrance = row.Count > 3 ? row[3]?.ToString() ?? null : null,
                    LastActivity = row.Count > 4 ? row[4]?.ToString() ?? null : null,
                });
            }
        }

        public async Task IsAccess(string login, string password)
        {
            var user = UsersList.FirstOrDefault(u =>
                       u.Login == login && u.Password == password);

            if (user != null)
            {
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("AccountId", user.Id.ToString());

                await LocalDbService.SaveCurrentUser(user);
                await UpdateUserActivity(user.Id);
            }
            else { Preferences.Set("IsLoggedIn", false); }
        }

        private async Task UpdateUserActivity(int userId)
        {
            await _sheetsService.UpdateCell(
                "Authorization",
                $"D{userId + 1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
