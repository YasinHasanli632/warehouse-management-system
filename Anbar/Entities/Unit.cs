using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Bu entity sistemdə istifadə olunan ölçü vahidlərini saxlayır.
    // Enum kimi yox, database table kimi saxlanır ki, gələcəkdə admin yeni vahid əlavə edə bilsin.
    // Məsələn: Ədəd, Kiloqram, Qram, Litr, Metr, m2, m3, Qutu, Paket və s.
    public class Unit : BaseEntity
    {
        // Ölçü vahidinin sistem daxili açarı.
        // Məsələn: eded, kg, qram, litr, metr.
        public string Key { get; set; } = null!;

        // UI-də görünəcək tam adı.
        // Məsələn: Ədəd, Kiloqram, Qram, Litr.
        public string Name { get; set; } = null!;

        // Qısa simvol.
        // Məsələn: əd, kq, qr, l, m.
        public string Symbol { get; set; } = null!;

        // Sıralama üçündür.
        // UI-də ComboBox içində vahidlər bu sıraya görə düzülə bilər.
        public int SortOrder { get; set; }

        // Bu vahidi default sistem vahidi kimi istifadə etmək üçündür.
        // Adətən "Ədəd" default olur.
        public bool IsDefault { get; set; }

        // Bu ölçü vahidindən default olaraq istifadə edən kateqoriyalar.
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        // Bu ölçü vahidinə bağlı məhsullar.
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
