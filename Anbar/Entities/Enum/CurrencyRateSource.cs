using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Məzənnənin haradan gəldiyini göstərir.
    public enum CurrencyRateSource
    {
        // İstifadəçi manual yazıb.
        Manual = 1,

        // Qaimə tarixinə görə götürülüb.
        InvoiceDate = 2,

        // Mərkəzi Bank və ya başqa rəsmi mənbə. Gələcək üçün saxlanılır.
        CentralBank = 3
    }
}
