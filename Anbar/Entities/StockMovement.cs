using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity stok hərəkət tarixçəsini saxlayır.
    public class StockMovement : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public StockMovementType MovementType { get; set; }
        public decimal Quantity { get; set; }

        public int? FromShelfId { get; set; }
        public int? ToShelfId { get; set; }

        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public int? StockBatchId { get; set; }
        public StockBatch? StockBatch { get; set; }

        // YENI:
        // Hərəkət zamanı istifadə edilən vahid maya dəyəri.
        public decimal UnitCost { get; set; }

        // YENI:
        // Hərəkətin ümumi maya dəyəri.
        public decimal TotalCost { get; set; }

        // YENI:
        // Hərəkət valyutası.
        public CurrencyType Currency { get; set; } = CurrencyType.AZN;

        // YENI:
        // Hərəkət məzənnəsi.
        public decimal ExchangeRate { get; set; } = 1;

        public string? Note { get; set; }
    }
}

