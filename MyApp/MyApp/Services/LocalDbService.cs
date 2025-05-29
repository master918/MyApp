using MyApp.Items;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    public class InventoryRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public InventoryRepository(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // Получаем все элементы для листа и формы
        public async Task<List<InventoryItem>> GetItemsAsync(string sheetName, string formType)
        {

            return await _database.Table<InventoryItem>()
                .Where(i => i.SheetName == sheetName && i.FormType == formType)
                .ToListAsync();
        }

        // Получаем уникальные имена для автодополнения
        public async Task<List<string>> GetItemNamesAsync(string sheetName, string formType)
        {
            var items = await GetItemsAsync(sheetName, formType);
            return items.Select(i => i.Name)
                       .Where(n => !string.IsNullOrEmpty(n))
                       .Distinct()
                       .ToList();
        }

        // Получаем значения полей по имени
        public async Task<Dictionary<int, string>> GetFieldValuesAsync(string sheetName, string formType, string itemName)
        {
            var s = await _database.QueryAsync<InventoryItem>("select * from InventoryItem");
            var item = await _database.Table<InventoryItem>()
                .FirstOrDefaultAsync(i =>
                    i.SheetName == sheetName &&
                    i.FormType == formType &&
                    i.Name == itemName);

            if (item == null)
                return new Dictionary<int, string>();

            var result = new Dictionary<int, string>();

            // Добавляем значения из ReadColumnValues
            if (item.ReadColumnValues != null)
            {
                foreach (var pair in item.ReadColumnValues)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            // Добавляем/перезаписываем значениями из WriteColumnValues
            if (item.WriteColumnValues != null)
            {
                foreach (var pair in item.WriteColumnValues)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        // Массовое сохранение
        public async Task SaveItemsAsync(string sheetName, string formType, IEnumerable<InventoryItem> items)
        {
            await _database.RunInTransactionAsync(tx =>
            {
                tx.Execute("DELETE FROM InventoryItem WHERE SheetName = ? AND FormType = ?", sheetName, formType);
                tx.InsertAll(items);
            });
        }

        public async Task SaveItemAsync(InventoryItem item)
        {
            var existingItem = await _database.Table<InventoryItem>()
                .FirstOrDefaultAsync(i =>
                    i.SheetName == item.SheetName &&
                    i.FormType == item.FormType &&
                    i.Name == item.Name);

            if (existingItem != null)
            {
                // Обновляем существующую запись
                existingItem.WriteColumnValues = item.WriteColumnValues;
                await _database.UpdateAsync(existingItem);
            }
            else
            {
                // Добавляем новую запись
                await _database.InsertAsync(item);
            }
            var s = await _database.QueryAsync<InventoryItem>("select * from InventoryItem");
        }
    }
}
