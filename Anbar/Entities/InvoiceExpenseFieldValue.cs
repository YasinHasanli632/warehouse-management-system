using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Qaimə xərcinin detail məlumatlarını saxlayır.
    // Məsələn: Sürücü adı = Əhməd, Maşın = Kamaz, Nömrə = 99-GG-505.
    public class InvoiceExpenseFieldValue : BaseEntity
    {
        // Hansı qaimə xərcinə aid olduğunu göstərir.
        public int InvoiceExpenseId { get; set; }

        public InvoiceExpense InvoiceExpense { get; set; } = null!;

        // Əgər bu field default definition-dan gəlibsə, onun ID-si.
        public int? ExpenseTypeFieldDefinitionId { get; set; }

        public ExpenseTypeFieldDefinition? ExpenseTypeFieldDefinition { get; set; }

        // Key. Məsələn: DriverName və ya custom yazılan ad.
        public string FieldKey { get; set; } = null!;

        // UI-da görünən ad. Məsələn: Sürücü adı.
        public string Label { get; set; } = null!;

        // Dəyər. Məsələn: Əhməd Məmmədov.
        public string? Value { get; set; }

        // Custom field-dir, yoxsa default field.
        public bool IsCustom { get; set; } = false;

        // UI-da sıralama.
        public int SortOrder { get; set; } = 0;
    }
}
