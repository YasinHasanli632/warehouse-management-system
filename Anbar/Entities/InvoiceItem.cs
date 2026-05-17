using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity qaimənin içindəki məhsul sətirlərini saxlayır.
    public class InvoiceItem : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int ShelfId { get; set; }
        public Shelf Shelf { get; set; } = null!;

        public string? ProductBarcode { get; set; }

        // YENI:
        // Qaimə yaranan anda məhsul adı/code snapshot saxlanır.
        public string? ProductNameSnapshot { get; set; }
        public string? ProductCodeSnapshot { get; set; }

        public decimal Quantity { get; set; }

        // Köhnə sahələr qalır.
        public decimal Price { get; set; }
        public decimal Total { get; set; }

        // Orijinal valyutada vahid qiymət.
        public decimal OriginalUnitPrice { get; set; }

        // YENI:
        // Bu qaimə item-inə aid real vergi snapshot-ları.
        public ICollection<InvoiceItemTax> Taxes { get; set; } = new List<InvoiceItemTax>();
        // Orijinal valyutada sətir toplamı.
        public decimal OriginalTotalAmount { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public decimal ExchangeRate { get; set; } = 1;

        // AZN qarşılığı vahid qiymət.
        public decimal LocalUnitPrice { get; set; }

        // AZN qarşılığı sətir toplamı.
        public decimal LocalTotalAmount { get; set; }

        // YENI:
        // ƏDV xaric baza məbləğ.
        public decimal NetAmount { get; set; }

        // YENI:
        // ƏDV məbləği.
        public decimal VatAmount { get; set; }

        // YENI:
        // ƏDV daxil yekun məbləğ.
        public decimal GrossAmount { get; set; }

        // YENI:
        // Bu item qaimə yarananda ƏDV-li idimi?
        public bool IsVatApplicable { get; set; }

        // YENI:
        // Qaimə yarananda tətbiq olunan ƏDV faizi.
        public decimal VatRate { get; set; }

        // YENI:
        // Bu sətirdə qiymət ƏDV daxil idimi?
        public bool IsVatIncludedInPrice { get; set; }

        // YENI:
        // Bu sətirdə ƏDV maya dəyərinə daxil edilibmi?
        public bool IsVatIncludedInCost { get; set; }

        // 1 vahidə düşən xərc payı.
        public decimal ExpenseUnitShare { get; set; }

        // 1 vahidə düşən vergi/rüsum payı.
        public decimal TaxUnitShare { get; set; }

        // YENI:
        // 1 vahidə düşən endirim payı.
        public decimal DiscountUnitShare { get; set; }

        // 1 vahidin final maya dəyəri.
        public decimal FinalUnitCost { get; set; }

        // YENI:
        // Bu item-in ümumi final maya dəyəri.
        public decimal FinalTotalCost { get; set; }
        // YENI:
        // Sətir səviyyəsində endirim.
        // Relation deyil, sadəcə InvoiceItem-in öz maliyyə snapshot sahəsidir.
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public ICollection<InvoiceExpenseAllocation> ExpenseAllocations { get; set; } = new List<InvoiceExpenseAllocation>();

        // YENI:
        // Vergi/rüsum payları.
        public ICollection<InvoiceTaxAllocation> TaxAllocations { get; set; } = new List<InvoiceTaxAllocation>();
    }
}
