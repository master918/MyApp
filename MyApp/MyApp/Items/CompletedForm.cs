using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyApp.Items
{
    public class CompletedForm
    {
        public int Index { get; set; }
        public List<InventoryField> Fields { get; set; }

        public string DisplayText => $"{Index}. {GetTitle()}";

        private string GetTitle()
        {
            var nameField = Fields?.FirstOrDefault(f =>
                !string.IsNullOrEmpty(f.Label) &&
                f.Label.IndexOf("наименование", StringComparison.OrdinalIgnoreCase) >= 0);
            return nameField?.Value ?? "(без наименования)";
        }
    }
}
