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
    public class UserService
    {
        private readonly GoogleService _sheetsService;
        ObservableCollection<User> Users = new ObservableCollection<User>();
        User CurrentUser { get; set; }

        public UserService()
        {
            _sheetsService = new GoogleService();
        }
        public async Task GetUsers()
        {
            Users = await _sheetsService.GetUsers(); 
        }
        public async Task IsAccess(string login, string password)//Проверка логина и пароля
        {
            CurrentUser = Users.FirstOrDefault(u => u.Login == login && u.Password == password);//Получения User из БД

            if (CurrentUser != null)
            {
                Preferences.Set("IsLoggedIn", true);
                await UpdateUserActivityEntrance();//Обновление последнего входа
                await LocalDbService.SaveCurrentUser(CurrentUser);
            }
            else { Preferences.Set("IsLoggedIn", false); }
        }

        private async Task UpdateUserActivityEntrance()
        {
            CurrentUser.LastEntrance = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await _sheetsService.UpdateCell(
                "Authorization",
                $"D{CurrentUser.Id + 1}",
                CurrentUser.LastEntrance);
        }
    }
}
