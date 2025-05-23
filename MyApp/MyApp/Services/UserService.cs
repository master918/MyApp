using MyApp.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        public void IsAccess(string login, string password)//Проверка логина и пароля
        {
            CurrentUser = Users.FirstOrDefault(u => u.Login == login && u.Password == password);

            if (CurrentUser != null)
            {
                // Устанавливаем флаг доступа сразу
                Preferences.Set("IsLoggedIn", true);

                // Запускаем асинхронные операции без ожидания их завершения
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LocalDbService.SaveCurrentUser(CurrentUser);//Сохранение в БД
                        await UpdateUserActivityEntrance();//Обновление последнего входа
                    }
                    catch (Exception ex)
                    {
                        // Логирование ошибок, если нужно
                        Debug.WriteLine($"Ошибка при сохранении пользователя: {ex.Message}");
                    }
                });
            }
            else
            {
                Preferences.Set("IsLoggedIn", false);
            }
        }

        private async Task UpdateUserActivityEntrance()
        {
            CurrentUser.LastEntrance = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CurrentUser.LastActivity = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await _sheetsService.UpdateCell(
                "Authorization",
                $"D{CurrentUser.Id + 1}",
                CurrentUser.LastEntrance);

            await _sheetsService.UpdateCell(
                "Authorization",
                $"E{CurrentUser.Id + 1}",
                CurrentUser.LastActivity);
        }
    }
}
