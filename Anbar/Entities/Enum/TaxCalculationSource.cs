using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Verginin hansı mənbədən hesablandığını bildirir.
    public enum TaxCalculationSource
    {
        Product = 1,          // Məhsul ƏDV-si
        Invoice = 2,          // Qaimə səviyyəsində vergi
        Import = 3,           // İdxal/gömrük vergisi
        Manual = 4,           // Manual əlavə edilmiş vergi
        Expense = 5           // Xərc əsasında yaranan vergi
    }
}
