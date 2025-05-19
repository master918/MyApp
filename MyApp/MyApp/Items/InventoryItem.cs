using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace MyApp.Items
{
    public class InventoryItem
    {
        [PrimaryKey, AutoIncrement]
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string Стеллаж { get; set; }
        public string Полка { get; set; }
        public string Место { get; set; }
        public string Количество_фактич { get; set; }
        public string Доп_описание { get; set; }
        public string StorageName { get; set; }
    }
}
