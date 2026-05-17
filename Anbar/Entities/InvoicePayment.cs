using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    public class InvoicePayment : BaseEntity
    {
        // Aid olduğu qaimə ID-si.
        public int InvoiceId { get; set; }

        // Aid olduğu qaimə.
        public Invoice Invoice { get; set; } = null!;

        // Ödəniş tipi.
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;

        // Ödəniş valyutası.
        public CurrencyType Currency { get; set; } = CurrencyType.AZN;

        // Ödəniş məzənnəsi.
        public decimal ExchangeRate { get; set; } = 1;

        // Orijinal valyutada ödənilən məbləğ.
        public decimal OriginalAmount { get; set; }

        // AZN qarşılığı.
        public decimal LocalAmount { get; set; }

        // Ödəniş tarixi.
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Qəbz, transfer kodu və ya bank referansı.
        public string? ReferenceNumber { get; set; }

        // Əlavə qeyd.
        public string? Note { get; set; }
    }
}
