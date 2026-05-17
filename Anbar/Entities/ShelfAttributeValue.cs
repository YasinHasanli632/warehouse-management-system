using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI: Konkret rəfin konkret xüsusiyyət dəyərini saxlayır.
    // Məsələn:
    // Rəf A-01 -> MaxWeightKg -> 500
    // Rəf A-01 -> MaxVolumeM3 -> 3
    public class ShelfAttributeValue : BaseEntity
    {
        // Hansı rəfə aid olduğunu göstərir.
        public int ShelfId { get; set; }

        // Aid olduğu rəf.
        public Shelf Shelf { get; set; } = null!;

        // Hansı rəf xüsusiyyətinə aid olduğunu göstərir.
        public int ShelfAttributeDefinitionId { get; set; }

        // Aid olduğu rəf xüsusiyyəti.
        public ShelfAttributeDefinition ShelfAttributeDefinition { get; set; } = null!;

        // Rəqəmsal dəyər.
        // Məsələn: 500, 3, 220.
        public decimal? NumericValue { get; set; }

        // Mətn dəyəri.
        // Məsələn: Bəli, Soyuq zona, Kövrək məhsul üçündür.
        public string? TextValue { get; set; }
    }
}
