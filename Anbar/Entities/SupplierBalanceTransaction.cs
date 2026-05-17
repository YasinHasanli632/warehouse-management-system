using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Təchizatçı borc tarixçəsini saxlayır.
    // Supplier.DebtAmount cari borcdur, bu entity isə borcun tarixçəsidir.
    public class SupplierBalanceTransaction : BaseEntity
    {
        // Təchizatçı ID-si.
        public int SupplierId { get; set; }

        // Təchizatçı.
        public Supplier Supplier { get; set; } = null!;

        // Əməliyyat hansı qaimədən yaranıbsa onun ID-si.
        public int? InvoiceId { get; set; }

        // Qaimə.
        public Invoice? Invoice { get; set; }

        // Hansı ödənişdən yaranıbsa onun ID-si.
        public int? SupplierPaymentId { get; set; }

        // Təchizatçı ödənişi.
        public SupplierPayment? SupplierPayment { get; set; }

        // Əməliyyat tipi.
        public BalanceTransactionType TransactionType { get; set; }

        // Valyuta.
        public CurrencyType Currency { get; set; } = CurrencyType.AZN;

        // Məzənnə.
        public decimal ExchangeRate { get; set; } = 1;

        // Orijinal valyutada məbləğ.
        public decimal OriginalAmount { get; set; }

        // AZN qarşılığı.
        public decimal LocalAmount { get; set; }

        // Əməliyyatdan əvvəl borc.
        public decimal DebtBefore { get; set; }

        // Əməliyyatdan sonra borc.
        public decimal DebtAfter { get; set; }

        // Əməliyyat tarixi.
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // Əlavə qeyd.
        public string? Note { get; set; }
    }
}
