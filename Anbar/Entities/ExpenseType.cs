using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Xərc növlərini saxlayır: Daşınma, Fəhlə pulu, Endirim və s.
    public class ExpenseType : BaseEntity
    {
        public string Name { get; set; } = null!;

        // YENI:
        // Qısa kod. Məsələn DASIMA, FEHLE, GOMRUK, BROKER.
        public string Code { get; set; } = null!;

        public ExpenseDirection DefaultDirection { get; set; } = ExpenseDirection.Plus;
        public bool IsSystem { get; set; } = false;

        public bool UseForStockIn { get; set; } = true;
        public bool UseForStockOut { get; set; } = true;

        public bool AffectStockCost { get; set; } = false;

        public bool UseForImport { get; set; } = false;

        public CostAllocationMethod DefaultAllocationMethod { get; set; } = CostAllocationMethod.ByAmount;
        public bool IsImportExpense { get; set; }

        public bool IncludeInProductCost { get; set; }

        public bool IsRecoverableTax { get; set; }

        public bool IsCustomsExpense { get; set; }
        public bool IsTaxRelated { get; set; } = false;

        // YENI:
        // Bu xərc qaimədə default görünsünmü.
        public bool ShowByDefault { get; set; } = true;

        // YENI:
        // Bu xərc mütləqdirmi.
        public bool IsRequired { get; set; } = false;

        // YENI:
        // 0 məbləğli xərc maya hesabına daxil edilsinmi.
        public bool IncludeZeroAmountInCost { get; set; } = false;

        // YENI:
        // Sıralama.
        public int SortOrder { get; set; }

        public ICollection<ExpenseTypeFieldDefinition> FieldDefinitions { get; set; } = new List<ExpenseTypeFieldDefinition>();
        public ICollection<InvoiceExpense> InvoiceExpenses { get; set; } = new List<InvoiceExpense>();
    }
}
