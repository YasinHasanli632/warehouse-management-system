using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Qaimə xərcinin hansı məhsula nə qədər paylandığını saxlayır.
    public class InvoiceExpenseAllocation : BaseEntity
    {
        public int InvoiceExpenseId { get; set; }
        public InvoiceExpense InvoiceExpense { get; set; } = null!;

        public int InvoiceItemId { get; set; }
        public InvoiceItem InvoiceItem { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public CostAllocationMethod AllocationMethod { get; set; } = CostAllocationMethod.ByAmount;

        // YENI:
        // Paylama üçün istifadə olunan baza.
        // Məsələn miqdar, məbləğ, çəki, həcm.
        public decimal AllocationBaseValue { get; set; }

        public decimal AllocatedAmount { get; set; }
        public decimal UnitAllocatedAmount { get; set; }

        public string? Note { get; set; }
    }
}
