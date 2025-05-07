using System;

namespace MyApp.Models
{
    public class LogPass
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class InventoryItem
    {
        public string Наименование { get; set; }
        public string Стеллаж { get; set; }
        public string Полка { get; set; }
        public string Место { get; set; }
        public string Количество_фактич { get; set; }
        public string Доп_описание { get; set; }
        public string StorageName { get; set; } // Для хранения выбранного помещения
    }

}