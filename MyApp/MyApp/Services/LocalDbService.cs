using MyApp.Items;
using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MyApp.Services
{
    public static class LocalDbService
    {
        private static SQLiteAsyncConnection _database;

        private static async Task InitAsync()
        {
            if (_database != null)
                return;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "myapp.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            await _database.CreateTableAsync<User>();
        }

        public static async Task SaveCurrentUser(User user)
        {
            await InitAsync();

            // Сначала очищаем старые записи (храним только одного пользователя)
            await _database.DeleteAllAsync<User>();
            // Сохраняем нового пользователя
            await _database.InsertAsync(user);
        }

        public static async Task<User> GetCurrentUser()
        {
            await InitAsync();
            return await _database.Table<User>().FirstOrDefaultAsync();
        }

        public static async Task ClearUser()
        {
            await InitAsync();
            await _database.DeleteAllAsync<User>();
        }
    }
}
