using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity anbardakı rəfləri saxlayır.
    // Məsələn: A-01, A-02, B-01.
    public class Shelf : BaseEntity
    {
        // Rəfin kodu.
        public string Code { get; set; } = null!;

        // Rəfin zonası və ya sırası.
        public string Zone { get; set; } = null!;

        // Rəfin sıra nömrəsi.
        public int RowNumber { get; set; }

        // Rəfin say üzrə maksimum tutumu.
        // Əgər 0 yazılıbsa, say limiti yoxlanmayacaq.
        public decimal Capacity { get; set; }

        // Rəfin cari doluluq faizi.
        public decimal OccupancyPercent { get; set; }

        // Rəfin statusu.
        public ShelfStatus Status { get; set; } = ShelfStatus.Empty;

        // Rəfin aid olduğu anbar ID-si.
        public int WarehouseId { get; set; }

        // Rəfin aid olduğu anbar obyekti.
        public Warehouse Warehouse { get; set; } = null!;

        // Bu rəfdə saxlanılan məhsul qalıqları.
        public ICollection<ShelfStock> ShelfStocks { get; set; } = new List<ShelfStock>();

        // YENI: Rəfə aid dinamik limit və xüsusiyyət dəyərləri.
        // Məsələn: MaxWeightKg = 500, MaxVolumeM3 = 3, HeightCm = 220.
        public ICollection<ShelfAttributeValue> AttributeValues { get; set; } = new List<ShelfAttributeValue>();
    }
}
