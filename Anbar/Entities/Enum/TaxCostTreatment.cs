using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Verginin maya dəyərində necə davranacağını bildirir.
    public enum TaxCostTreatment
    {
        ExcludedFromCost = 1,   // Maya dəyərinə daxil deyil, ayrıca vergi kimi saxlanır
        IncludedInCost = 2,    // Maya dəyərinə daxildir
        Recoverable = 3        // Əvəzləşdirilə bilən ƏDV kimi ayrıca saxlanır
    }
}
