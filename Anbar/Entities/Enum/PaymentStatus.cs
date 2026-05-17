using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Qaimə üzrə ödəniş vəziyyətini göstərir.
    public enum PaymentStatus
    {
        // Heç ödəniş edilməyib.
        Unpaid = 1,

        // Qismən ödənilib.
        PartialPaid = 2,

        // Tam ödənilib.
        Paid = 3,

        // Artıq ödəniş edilib.
        OverPaid = 4
    }
}
