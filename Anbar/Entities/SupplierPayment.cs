using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity təchizatçıya edilən ödənişləri saxlayır.
    public class SupplierPayment : BaseEntity
    {
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        // Köhnə sahə qalır. AZN qarşılığı kimi istifadə oluna bilər.
        public decimal Amount { get; set; }

        // YENI:
        public CurrencyType Currency { get; set; } = CurrencyType.AZN;

        // YENI:
        public decimal ExchangeRate { get; set; } = 1;

        // YENI:
        public decimal OriginalAmount { get; set; }

        // YENI:
        public decimal LocalAmount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;
        public decimal DebtBeforePayment { get; set; }
        public decimal DebtAfterPayment { get; set; }

        // YENI:
        // Qəbz, bank transfer kodu və s.
        public string? ReferenceNumber { get; set; }

        public string? Note { get; set; }

        // YENI:
        // Bu ödənişdən yaranan balans tarixçəsi.
        public ICollection<SupplierBalanceTransaction> BalanceTransactions { get; set; } = new List<SupplierBalanceTransaction>();
    }
}
