using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI: Rəf üçün dinamik xüsusiyyət başlığını saxlayır.
    // Məsələn: Maksimum çəki, Maksimum həcm, Hündürlük, En, Dərinlik.
    public class ShelfAttributeDefinition : BaseEntity
    {
        // Xüsusiyyətin sistemdə görünən adı.
        // Məsələn: Maksimum çəki.
        public string Name { get; set; } = null!;

        // Xüsusiyyətin sistem açarı.
        // Məsələn: MaxWeightKg, MaxVolumeM3, HeightCm.
        public string Key { get; set; } = null!;

        // Vahid.
        // Məsələn: kg, m3, cm, ədəd.
        public string? Unit { get; set; }

        // Bu xüsusiyyət limit kimi yoxlanılacaqmı?
        // true olsa giriş qaiməsində yoxlama gedəcək.
        // false olsa sadəcə məlumat kimi saxlanacaq.
        public bool IsLimit { get; set; } = true;

        // Bu xüsusiyyət rəqəmsal dəyər tələb edirmi?
        // Çəki, həcm, ölçü üçün true olmalıdır.
        public bool IsNumeric { get; set; } = true;

        // Bu xüsusiyyətə aid rəflərdə saxlanılan dəyərlər.
        public ICollection<ShelfAttributeValue> Values { get; set; } = new List<ShelfAttributeValue>();
    }
}
