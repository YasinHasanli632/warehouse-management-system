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
    // Müştəridən qəbul edilən ödənişləri saxlayır.
    // Satış qaiməsi borc yaradır, CustomerPayment isə həmin borcu azaldır.
    public class CustomerPayment : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public decimal Amount { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public decimal ExchangeRate { get; set; } = 1;

        public decimal OriginalAmount { get; set; }
        public decimal LocalAmount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;

        public decimal DebtBeforePayment { get; set; }
        public decimal DebtAfterPayment { get; set; }

        public string? ReferenceNumber { get; set; }
        public string? Note { get; set; }

        public ICollection<CustomerBalanceTransaction> BalanceTransactions { get; set; } = new List<CustomerBalanceTransaction>();
    }
}
