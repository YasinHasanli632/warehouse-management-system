using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Qaiməyə bağlı vergi/rüsum məlumatlarını saxlayır.
    public class InvoiceTax : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public TaxType TaxType { get; set; }
        public string TaxName { get; set; } = null!;

        public TaxCalculationSource CalculationSource { get; set; } = TaxCalculationSource.Manual;
        public TaxCostTreatment CostTreatment { get; set; } = TaxCostTreatment.ExcludedFromCost;

        public decimal RatePercent { get; set; }
        public decimal TaxBaseAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public bool IsIncludedInCost { get; set; }
        public bool IsRecoverable { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public decimal ExchangeRate { get; set; } = 1;

        public decimal LocalTaxAmount { get; set; }

        // YENI:
        // Vergi məhsullara paylanmalıdırmı?
        // Gömrük rüsumu kimi maya dəyərinə düşən vergilər üçün true olacaq.
        public bool ShouldAllocateToItems { get; set; }

        // YENI:
        // Vergi məhsullara necə paylansın.
        public CostAllocationMethod AllocationMethod { get; set; } = CostAllocationMethod.ByAmount;

        public string? Note { get; set; }

        public ICollection<InvoiceTaxAllocation> Allocations { get; set; } = new List<InvoiceTaxAllocation>();
    }
}
