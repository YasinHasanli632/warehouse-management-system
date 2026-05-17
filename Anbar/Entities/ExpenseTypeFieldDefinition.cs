using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Xərc növünə görə default detail sahələrini saxlayır.
    // Məsələn Daşınma üçün: Sürücü adı, Telefon, Maşın, Dövlət nömrəsi, Məsafə.
    public class ExpenseTypeFieldDefinition : BaseEntity
    {
        // Hansı xərc növünə aid olduğunu göstərir.
        public int ExpenseTypeId { get; set; }

        public ExpenseType ExpenseType { get; set; } = null!;

        // Texniki key. Məsələn: DriverName, Phone, VehicleNumber.
        public string FieldKey { get; set; } = null!;

        // UI-da görünən ad. Məsələn: Sürücü adı.
        public string Label { get; set; } = null!;

        // Field tipi. Hələlik text saxlayırıq: Text, Number, Date.
        public string FieldType { get; set; } = "Text";

        // Boş buraxmaq olar ya yox.
        public bool IsRequired { get; set; } = false;

        // UI-da sıralama.
        public int SortOrder { get; set; } = 0;
    }
}
