using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Konkret qaiməyə əlavə olunan xərc sətrini saxlayır.
    public class InvoiceExpense : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int? ExpenseTypeId { get; set; }
        public ExpenseType? ExpenseType { get; set; }

        public string Name { get; set; } = null!;

        // Köhnə sahə qalır.
        public decimal Amount { get; set; }

        public ExpenseDirection Direction { get; set; } = ExpenseDirection.Plus;
        public bool AffectStockCost { get; set; } = false;

        public bool IsImportExpense { get; set; } = false;

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public decimal ExchangeRate { get; set; } = 1;

        public decimal OriginalAmount { get; set; }
        public decimal LocalAmount { get; set; }

        public CostAllocationMethod AllocationMethod { get; set; } = CostAllocationMethod.ByAmount;

        // YENI:
        // Xərc məhsullara paylanmalıdırmı?
        public bool ShouldAllocateToItems { get; set; } = true;

        // YENI:
        // Bu xərc vergidirmi? ƏDV kimi ayrıca tax engine-ə getməlidirmi?
        public bool IsTaxRelated { get; set; } = false;

        // YENI:
        // Bu xərc 0 olsa hesablansınmı?
        public bool IncludeZeroAmountInCost { get; set; } = false;

        public string? Note { get; set; }

        public ICollection<InvoiceExpenseFieldValue> FieldValues { get; set; } = new List<InvoiceExpenseFieldValue>();
        public ICollection<InvoiceExpenseAllocation> Allocations { get; set; } = new List<InvoiceExpenseAllocation>();
    }
}
