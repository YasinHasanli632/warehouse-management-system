using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Stok/FIFO ayarlarını saxlayır.
    public class StockSetting : BaseEntity
    {
        public bool EnableFIFO { get; set; } = true;
        public bool PreventNegativeStock { get; set; } = true;
        public bool CheckShelfCapacity { get; set; } = true;
        public bool BlockPassiveProductInInvoice { get; set; } = true;
        public bool AutoCreateBatchOnStockIn { get; set; } = true;
    }
}
