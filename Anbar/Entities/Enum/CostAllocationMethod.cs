using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Qaimə xərclərinin məhsullara necə paylanacağını bildirir.
    public enum CostAllocationMethod
    {
        ByQuantity = 1,   // Sayına görə
        ByAmount = 2,     // Məbləğinə görə
        ByWeight = 3,     // Çəkisinə görə
        ByVolume = 4,     // Həcminə görə
        Manual = 5        // Manual paylama
    }
}
