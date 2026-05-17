using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Qaimə davranış ayarlarını saxlayır.
    public class InvoiceSetting : BaseEntity
    {
        public string InvoicePrefix { get; set; } = "QAI";
        public bool LockConfirmedInvoice { get; set; } = true;
        public bool RequireReturnReason { get; set; } = true;
        public bool RequireShelfSelection { get; set; } = true;
        public bool RequireBatchSelectionForReturn { get; set; } = true;
        public bool CopyProductBarcodeToInvoiceItem { get; set; } = true;
    }
}
