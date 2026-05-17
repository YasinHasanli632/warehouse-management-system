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
    // Vergi kataloqu.
    // Məsələn: ƏDV, Gömrük rüsumu, Aksiz, Sadələşdirilmiş vergi.
    public class Tax : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;

        public TaxType TaxType { get; set; }

        public decimal DefaultRatePercent { get; set; }

        public bool IsRecoverableByDefault { get; set; } = false;
        public bool IsIncludedInCostByDefault { get; set; } = false;
        public bool IsIncludedInPriceByDefault { get; set; } = false;

        public TaxCalculationSource DefaultCalculationSource { get; set; } = TaxCalculationSource.Product;
        public TaxCostTreatment DefaultCostTreatment { get; set; } = TaxCostTreatment.ExcludedFromCost;

        public bool UseForLocalPurchase { get; set; } = true;
        public bool UseForImportPurchase { get; set; } = true;
        public bool UseForSale { get; set; } = true;

        public string? Note { get; set; }

        public ICollection<ProductTax> ProductTaxes { get; set; } = new List<ProductTax>();
        public ICollection<InvoiceItemTax> InvoiceItemTaxes { get; set; } = new List<InvoiceItemTax>();
    }
}
