using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{

    // YENI:
    // Qaiməyə bağlı vergi/rüsum tipini göstərir.
    public enum TaxType
    {
        // Yerli ƏDV.
        VAT = 1,

        // İdxal ƏDV-si.
        ImportVAT = 2,

        // Gömrük rüsumu.
        CustomsDuty = 3,

        // Aksiz vergisi.
        Excise = 4,

        // Mənfəət vergisi. Hesabat mərhələsi üçün saxlanılır.
        ProfitTax = 5,

        // Sadələşdirilmiş vergi. Hesabat mərhələsi üçün saxlanılır.
        SimplifiedTax = 6,

        // Digər vergi/rüsum.
        Other = 99
    }
}
