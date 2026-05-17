using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Müştəri borc tarixçəsini saxlayır.
    public class CustomerBalanceTransaction : BaseEntity
    {
        // Müştəri ID-si.
        public int CustomerId { get; set; }

        // Müştəri.
        public Customer Customer { get; set; } = null!;

        // Əməliyyat hansı qaimədən yaranıbsa onun ID-si.
        public int? InvoiceId { get; set; }

        // Qaimə.
        public Invoice? Invoice { get; set; }

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

        // YENI:
        // Müştəri ödənişi ilə bağlı transaction üçündür.
        public int? CustomerPaymentId { get; set; }
        public CustomerPayment? CustomerPayment { get; set; }
    }
}
