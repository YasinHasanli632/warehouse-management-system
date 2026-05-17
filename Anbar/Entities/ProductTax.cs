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
    // Məhsulun default vergi qaydasını saxlayır.
    // Bu real qaimə vergisi deyil, sadəcə məhsul üçün default şablondur.
    public class ProductTax : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int TaxId { get; set; }
        public Tax Tax { get; set; } = null!;

        public TaxType TaxType { get; set; }

        public decimal RatePercent { get; set; }

        public bool IsApplicable { get; set; } = true;
        public bool IsRecoverable { get; set; } = false;
        public bool IsIncludedInCost { get; set; } = false;
        public bool IsIncludedInPrice { get; set; } = false;

        public TaxCostTreatment CostTreatment { get; set; } = TaxCostTreatment.ExcludedFromCost;

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public string? Note { get; set; }
    }
}
