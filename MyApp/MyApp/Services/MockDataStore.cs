using MyApp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Services
{
    public class MockDataStore : IDataStore<InventoryItem>
    {
        private readonly List<InventoryItem> _items = new List<InventoryItem>();

        public async Task<bool> AddItemAsync(InventoryItem item)
        {
            _items.Add(item);
            return await Task.FromResult(true);
        }

        public async Task<IEnumerable<InventoryItem>> GetItemsAsync()
        {
            return await Task.FromResult(_items);
        }

        public async Task<bool> ClearItemsAsync()
        {
            _items.Clear();
            return await Task.FromResult(true);
        }
    }
}
