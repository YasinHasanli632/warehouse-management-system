using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Qaimə üzrə maya dəyəri yekununu ayrıca saxlayır.
    // Bu həm hesabat, həm audit, həm də UI preview üçün lazımdır.
    public class InvoiceCostSummary : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public decimal BaseItemsAmount { get; set; }
        public decimal NetItemsAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossItemsAmount { get; set; }

        public decimal CostIncludedExpenseAmount { get; set; }
        public decimal CostExcludedExpenseAmount { get; set; }

        public decimal CostIncludedTaxAmount { get; set; }
        public decimal RecoverableTaxAmount { get; set; }
        public decimal CostExcludedTaxAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalCostAmount { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.Now;

        public string? Note { get; set; }
    }
}
