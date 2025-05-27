using MyApp.Items;
using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;

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
                    throw new InvalidOperationException("Database is not initialized. Call InitializeAsync first.");
                return _database;
            }
        }

        public static async Task InitializeAsync()
        {
            if (_database != null) return;

            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Myapp.db");
                _database = new SQLiteAsyncConnection(dbPath);
                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<InventoryItem>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization failed: {ex.Message}");
                throw;
            }
        }

        public static async Task SaveCurrentUser(User user)
        {
            try
            {
                await _database.DeleteAllAsync<User>();
                await _database.InsertOrReplaceAsync(user);
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
                return await _database.Table<User>().FirstOrDefaultAsync();
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
                await _database.DeleteAllAsync<User>();
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
