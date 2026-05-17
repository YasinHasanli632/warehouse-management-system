using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Qaimə tipləri (2 tərəfli return ilə)
    public enum InvoiceType
    {
        // Təchizatçıdan mal girişi
        StockIn = 1,

        // Müştəriyə satış (çıxış)
        StockOut = 2,

        // YENI:
        // Müştəridən geri qaytarma (satışın reverse-i)
        CustomerReturnIn = 3,

        // YENI:
        // Təchizatçıya geri qaytarma (alışın reverse-i)
        SupplierReturnOut = 4
    }
}
