using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // Məhsulun aktiv və ya passiv olduğunu göstərir.
    public enum ProductStatus
    {
        // Məhsul aktivdir və sistemdə istifadə oluna bilər.
        Active = 1,

        // Məhsul passivdir və yeni əməliyyatlarda istifadə olunmur.
        Passive = 2
    }
}
