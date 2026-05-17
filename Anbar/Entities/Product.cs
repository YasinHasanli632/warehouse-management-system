using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity anbarda istifadə olunan məhsulları saxlayır.
    // Məsələn: Şkaf qapısı, menteşə, tutacaq, profil.
    public class Product : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;

        // Məhsulun barkodu varsa burada saxlanılır.
        public string? Barcode { get; set; }

        // Köhnə sahə qalır.
        public string Unit { get; set; } = "ədəd";

        public int? UnitId { get; set; }
        public Unit? UnitEntity { get; set; }

        public decimal MinStockQuantity { get; set; }

        // Köhnə alış qiyməti. UI-da default qiymət kimi istifadə oluna bilər.
        public decimal PurchasePrice { get; set; }

        public decimal SalePrice { get; set; }

        // Son hesablanmış real maya dəyəri.
        public decimal LastCostPrice { get; set; }

        // Weighted average üçün saxlanır.
        public decimal AverageCostPrice { get; set; }

        // YENI:
        // Məhsul ƏDV-yə tabedirmi.
        // Yerli mal qaiməsində ƏDV avtomatik bu sahəyə görə hesablanacaq.
        public bool IsVatApplicable { get; set; } = true;

        // YENI:
        // Məhsulun default ƏDV faizi.
        public decimal VatRate { get; set; } = 18;

        // YENI:
        // Alış qiyməti ƏDV daxil yazılır?
        // Azərbaycanda çox vaxt alış qiyməti ƏDV daxil göstərilə bilər.
        public bool IsPurchasePriceVatIncluded { get; set; } = true;

        // YENI:
        // ƏDV əvəzləşdirilə biləndirmi?
        // Əgər bəlidirsə, ƏDV maya dəyərinə düşmür, ayrıca vergi kimi saxlanır.
        public bool IsVatRecoverable { get; set; } = true;

        // YENI:
        // Məhsul aksizə tabedirmi.
        public bool IsExciseApplicable { get; set; } = false;

        // YENI:
        // Məhsul idxal/gömrük vergisindən azaddırmı.
        public bool IsImportTaxExempt { get; set; } = false;

        // YENI:
        // Məhsul çəkisi. Xərclərin çəkiyə görə paylanması üçün lazımdır.
        public decimal Weight { get; set; }

        // YENI:
        // Məhsul həcmi. Xərclərin həcmə görə paylanması üçün lazımdır.
        public decimal Volume { get; set; }

        public string? Description { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // YENI:
        // Məhsula bağlı default vergi qaydaları.
        public ICollection<ProductTax> ProductTaxes { get; set; } = new List<ProductTax>();
        public ICollection<ShelfStock> ShelfStocks { get; set; } = new List<ShelfStock>();
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();
    }
}
