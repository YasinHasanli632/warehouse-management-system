using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Təchizatçı/müştəri balans tarixçəsində əməliyyat tipini göstərir.
    public enum BalanceTransactionType
    {
        // Qaimədən borc yarandı.
        InvoiceDebt = 1,

        // Ödəniş edildi.
        Payment = 2,

        // Geri qaytarma ilə balans dəyişdi.
        Return = 3,

        // Manual düzəliş.
        Correction = 4
    }
}
