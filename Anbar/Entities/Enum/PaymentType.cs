using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Müştəri və təchizatçı üçün ödəniş formasını göstərir.
    public enum PaymentType
    {
        // Nağd ödəniş.
        Cash = 1,

        // Bank köçürməsi.
        BankTransfer = 2,

        // Kartla ödəniş.
        Card = 3,

        // Kredit/borc ilə ödəniş.
        Credit = 4
    }
}
