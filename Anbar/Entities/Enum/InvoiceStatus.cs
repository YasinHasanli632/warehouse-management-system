using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Qaimənin cari statusunu göstərir.
    public enum InvoiceStatus
    {
        // Qaimə hazırlanır, hələ stok dəyişməyib.
        Draft = 1,

        // Qaimə təsdiqlənib və stok hərəkəti icra olunub.
        Confirmed = 2,

        // Qaimə ləğv edilib.
        Cancelled = 3
    }
}
