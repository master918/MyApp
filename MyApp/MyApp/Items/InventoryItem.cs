using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace MyApp.Items
{
    public class InventoryItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int НомерПоПорядку { get; set; }               // Общий для всех

        public string Наименование { get; set; }              // Общий для всех

        // ИНВ-1
        public string НомерИнвентарный { get; set; }
        public string НомерЗаводской { get; set; }
        public string НомерПаспорта { get; set; }
        public int? ФактНаличиеКоличество { get; set; }       // шт.
        public decimal? ФактНаличиеСтоимость { get; set; }    // руб. коп.

        // ИНВ-1а
        public DateTime? ДокРегистрацииДата { get; set; }
        public string ДокРегистрацииНомер { get; set; }
        public decimal? СтоимостьФактически { get; set; }     // руб. коп.

        // ИНВ-3
        public string НоменклатурныйКод { get; set; }
        public string КодОКЕИ { get; set; }
        public decimal? Цена { get; set; }                    // руб. коп.

        // Общие вспомогательные поля
        public string ТипФормы { get; set; }                  // "ИНВ-1", "ИНВ-1а", "ИНВ-3"
        public string ДополнительноеОписание { get; set; }    // при необходимости
    }
}
