using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity məhsul kateqoriyalarını saxlayır.
    // Alt kateqoriya məntiqi də var.
    // Məsələn:
    // Furnitur
    //   └── Menteşə
    //   └── Tutacaq
    public class Category : BaseEntity
    {
        // Kateqoriya adı.
        public string Name { get; set; } = null!;

        // Kateqoriya haqqında əlavə qeyd.
        public string? Description { get; set; }

        // Ana kateqoriya ID-si.
        // Əgər null-dursa, bu əsas kateqoriyadır.
        public int? ParentCategoryId { get; set; }

        // Ana kateqoriya obyekti.
        public Category? ParentCategory { get; set; }

        // Bu kateqoriyanın alt kateqoriyaları.
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // Bu kateqoriyaya aid məhsullar.
        public ICollection<Product> Products { get; set; } = new List<Product>();
        // YENI:
        // Bu kateqoriyanın default ölçü vahidi.
        // Məsələn: İçkilər = Litr, Qapılar = Ədəd, Metal = Kiloqram.
        // Məhsul yaradanda kateqoriya seçiləndə bu vahid avtomatik məhsula təklif olunacaq.
        public int? DefaultUnitId { get; set; }

        // YENI:
        // Default ölçü vahidinin navigation property-si.
        public Unit? DefaultUnit { get; set; }
        // Bu kateqoriyaya aid xüsusiyyət başlıqları.
        // Məsələn Qapı kateqoriyası üçün: Rəng, Ölçü, Material.
        public ICollection<AttributeDefinition> AttributeDefinitions { get; set; } = new List<AttributeDefinition>();
    }
}
