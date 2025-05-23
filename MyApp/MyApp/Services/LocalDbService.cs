using MyApp.Items;
using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms;

namespace MyApp.Services
{
    public static class LocalDbService
    {
        private static SQLiteAsyncConnection _database;
        public static SQLiteAsyncConnection Database
        {
            get
            {
                if (_database == null)
                {
                    InitializeDatabase();
                }
                return _database;
            }
        }

        private static void InitializeDatabase()
        {
            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Myapp.db");
                _database = new SQLiteAsyncConnection(dbPath);

                // Создание таблиц
                CreateTablesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization failed: {ex.Message}");
                throw;
            }
        }
        private static async Task CreateTablesAsync()
        {
            try
            {
                await _database.CreateTableAsync<User>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Table creation failed: {ex.Message}");
                throw;
            }
        }

        public static async Task SaveCurrentUser(User user)
        {
            try
            {
                var s = Database.QueryAsync<User>("select * from User").Result;
                await Database.InsertOrReplaceAsync(user);
                s = Database.QueryAsync<User>("select * from User").Result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save user failed: {ex.Message}");
                throw;
            }
        }

        public static async Task<User> GetCurrentUser()
        {
            try
            {
                return await Database.Table<User>().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Get user failed: {ex.Message}");
                return null;
            }
        }

        public static async Task ClearUser()
        {
            try
            {
                await Database.DeleteAllAsync<User>().ConfigureAwait(false);
                Debug.WriteLine("User data cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Clear user failed: {ex.Message}");
                throw;
            }
        }
    }
}
