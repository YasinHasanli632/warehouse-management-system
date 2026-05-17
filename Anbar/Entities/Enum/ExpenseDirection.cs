using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Qaimə xərcinin yekun məbləğə necə təsir etdiyini göstərir.
    public enum ExpenseDirection
    {
        // Məbləğə əlavə olunur. Məsələn: daşınma, fəhlə pulu.
        Plus = 1,

        // Məbləğdən çıxılır. Məsələn: endirim.
        Minus = 2
    }
}
