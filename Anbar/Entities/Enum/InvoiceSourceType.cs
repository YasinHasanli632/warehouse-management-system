using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Qaimənin yerli və ya idxal mənbəli olduğunu göstərir.
    // Database-də əsas sahə kimi Invoice.IsImport da saxlanır.
    // Bu enum UI və servis oxunaqlılığı üçün faydalıdır.
    public enum InvoiceSourceType
    {
        // Yerli qaimə.
        Local = 1,

        // İdxal qaiməsi.
        Import = 2
    }
}
