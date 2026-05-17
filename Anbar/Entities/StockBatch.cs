using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // FIFO üçün partiya sistemi.
    public class StockBatch : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int ShelfId { get; set; }
        public Shelf Shelf { get; set; } = null!;

        public int? SourceInvoiceId { get; set; }
        public Invoice? SourceInvoice { get; set; }

        public int? SourceInvoiceItemId { get; set; }
        public InvoiceItem? SourceInvoiceItem { get; set; }

        public DateTime EntryDate { get; set; } = DateTime.Now;
        public string? Note { get; set; }

        // Köhnə sahə qalır.
        public decimal PurchasePrice { get; set; }

        public decimal OriginalUnitPrice { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;

        public decimal PurchaseUnitPrice { get; set; }

        public decimal ExchangeRate { get; set; } = 1;

        public decimal LocalUnitPrice { get; set; }

        public decimal ExpenseUnitShare { get; set; }

        public decimal TaxUnitShare { get; set; }

        // YENI:
        // 1 vahidə düşən endirim payı.
        public decimal DiscountUnitShare { get; set; }

        public decimal FinalUnitCost { get; set; }

        // YENI:
        // Batch-in ümumi final maya dəyəri.
        public decimal FinalTotalCost { get; set; }

        public bool IsImportBatch { get; set; } = false;

        public decimal InitialQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }

        public string BatchNumber { get; set; } = null!;

        // YENI:
        // FIFO sıralaması üçün ayrıca tarix.
        // EntryDate ilə eyni ola bilər, amma gələcəkdə expiry/lot üçün ayrıdır.
        public DateTime FifoDate { get; set; } = DateTime.Now;

        // YENI:
        // İstehsal/partiya tarixi.
        public DateTime? ProductionDate { get; set; }

        // YENI:
        // Son istifadə tarixi. Qida/dərman tipli mallarda lazım olacaq.
        public DateTime? ExpiryDate { get; set; }

        // YENI:
        // Batch bağlanıbsa, artıq istifadə edilməsin.
        public bool IsClosed { get; set; } = false;

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
