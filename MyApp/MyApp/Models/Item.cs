using System;

namespace MyApp.Models
{
    public class LogPass
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class InventoryField
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }
}