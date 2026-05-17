using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Şirkətin hansı vergi rejimində işlədiyini göstərir.
    public enum TaxRegime
    {
        // Vergisiz/sadə uçot.
        NoTax = 1,

        // ƏDV-li rejim.
        VAT = 2,

        // Sadələşdirilmiş vergi rejimi.
        Simplified = 3
    }
}
