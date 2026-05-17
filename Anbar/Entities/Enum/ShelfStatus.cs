using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Rəfin doluluq vəziyyətini göstərir.
    public enum ShelfStatus
    {
        // Rəf boşdur.
        Empty = 1,

        // Rəfdə az məhsul var.
        Low = 2,

        // Rəf normal doluluqdadır.
        Normal = 3,

        // Rəf tam doludur.
        Full = 4
    }
}
