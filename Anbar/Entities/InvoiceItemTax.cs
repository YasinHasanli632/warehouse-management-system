using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Qaimə item-in real vergi snapshot-udur.
    // Məhsulun vergisi sonradan dəyişsə belə, köhnə qaimə dəyişməsin deyə burada saxlanır.
    public class InvoiceItemTax : BaseEntity
    {
        public int InvoiceItemId { get; set; }
        public InvoiceItem InvoiceItem { get; set; } = null!;

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? TaxId { get; set; }
        public Tax? Tax { get; set; }

        public TaxType TaxType { get; set; }
        public string TaxName { get; set; } = null!;

        public TaxCalculationSource CalculationSource { get; set; } = TaxCalculationSource.Product;
        public TaxCostTreatment CostTreatment { get; set; } = TaxCostTreatment.ExcludedFromCost;

        public decimal RatePercent { get; set; }

        public decimal TaxBaseAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public decimal ExchangeRate { get; set; } = 1;

        public decimal LocalTaxBaseAmount { get; set; }
        public decimal LocalTaxAmount { get; set; }

        public bool IsRecoverable { get; set; }
        public bool IsIncludedInCost { get; set; }
        public bool IsIncludedInPrice { get; set; }

        public string? Note { get; set; }
    }
}
