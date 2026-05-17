using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity müştəri məlumatlarını saxlayır.
    public class Customer : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Voen { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;
        public decimal CreditLimit { get; set; }
        public decimal DebtAmount { get; set; }

        // YENI:
        // Müştərinin orijinal valyutada borcu. Əsasən gələcək xarici satışlar üçün saxlanır.
        public decimal DebtAmountOriginalCurrency { get; set; }
        // YENI:
        // Müştərinin borcunun AZN qarşılığı.
        public decimal DebtAmountLocal { get; set; }

        // YENI:
        // Müştərinin borcunun orijinal valyutada qarşılığı.
        public decimal DebtAmountOriginal { get; set; }
        public ICollection<CustomerPayment> Payments { get; set; } = new List<CustomerPayment>();

        // YENI:
        // Müştəri borc tarixçəsi.
        public ICollection<CustomerBalanceTransaction> BalanceTransactions { get; set; } = new List<CustomerBalanceTransaction>();
        public string? Note { get; set; }
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        // YENI:
    }
}
