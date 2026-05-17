using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // YENI: UI-dən ShelfService-ə rəf xüsusiyyəti göndərmək üçün model.
    public class ShelfAttributeInput
    {
        // Seçilmiş rəf xüsusiyyətinin ID-si.
        public int ShelfAttributeDefinitionId { get; set; }

        // Rəqəmsal dəyər.
        public decimal? NumericValue { get; set; }

        // Mətn dəyəri.
        public string? TextValue { get; set; }
    }
}
