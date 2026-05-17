using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Stok hərəkətinin növünü göstərir.
    public enum StockMovementType
    {
        // Qaimə ilə mal girişi.
        StockIn = 1,

        // Qaimə ilə mal çıxışı.
        StockOut = 2,

        // Eyni anbar daxilində rəfdən-rəfə transfer.
        ShelfTransfer = 3,

              // YENI:
        CustomerReturnIn = 4,

        // YENI:
        SupplierReturnOut = 5
    }
}
