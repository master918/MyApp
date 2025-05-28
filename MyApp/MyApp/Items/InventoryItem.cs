using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SQLite;

namespace MyApp.Items
{
    public class InventoryItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string SheetName { get; set; }
        public string FormType { get; set; }
        public string Name { get; set; }
        [Ignore]
        public Dictionary<int, string> WriteColumnValues { get; set; } = new Dictionary<int, string>();
        [Ignore]
        public Dictionary<int, string> ReadColumnValues { get; set; } = new Dictionary<int, string>();

        // Эти два свойства сериализуются в JSON и сохраняются в SQLite
        public string WriteColumnValuesJson
        {
            get => JsonConvert.SerializeObject(WriteColumnValues);
            set => WriteColumnValues = string.IsNullOrWhiteSpace(value)
                ? new Dictionary<int, string>()
                : JsonConvert.DeserializeObject<Dictionary<int, string>>(value);
        }

        public string ReadColumnValuesJson
        {
            get => JsonConvert.SerializeObject(ReadColumnValues);
            set => ReadColumnValues = string.IsNullOrWhiteSpace(value)
                ? new Dictionary<int, string>()
                : JsonConvert.DeserializeObject<Dictionary<int, string>>(value);
        }
    }
}
