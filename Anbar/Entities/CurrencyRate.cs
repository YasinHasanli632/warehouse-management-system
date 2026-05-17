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
    // Tarixə görə valyuta məzənnələrini saxlayır.
    public class CurrencyRate : BaseEntity
    {
        // Hansı valyutadan çevrilir.
        public CurrencyType FromCurrency { get; set; }

        // Hansı valyutaya çevrilir. Əsasən AZN olacaq.
        public CurrencyType ToCurrency { get; set; } = CurrencyType.AZN;

        // Məzənnə.
        public decimal Rate { get; set; }

        // Məzənnənin aid olduğu tarix.
        public DateTime RateDate { get; set; }

        // Mənbə.
        public CurrencyRateSource Source { get; set; } = CurrencyRateSource.Manual;

        // Əlavə qeyd.
        public string? Note { get; set; }
    }
}
