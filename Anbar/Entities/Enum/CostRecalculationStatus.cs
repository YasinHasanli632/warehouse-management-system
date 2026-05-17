using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Qaimənin maya dəyəri hesablanma statusu.
    public enum CostRecalculationStatus
    {
        NotCalculated = 1,
        Calculated = 2,
        NeedsRecalculation = 3,
        Locked = 4
    }
}
