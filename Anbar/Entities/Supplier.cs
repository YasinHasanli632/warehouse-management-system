using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity təchizatçı məlumatlarını saxlayır.
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Voen { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }

        // YENI:
        // Təchizatçı yerli və ya xaricidir.
        public SupplierOriginType OriginType { get; set; } = SupplierOriginType.Local;

        // YENI:
        // Xarici təchizatçı üçün ölkə.
        public string? Country { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;

        // YENI:
        // Təchizatçıya verilən kredit limiti. 0 = limit yoxdur.
        public decimal CreditLimit { get; set; } = 0;

        // Cari borc AZN qarşılığı.
        public decimal DebtAmount { get; set; } = 0;

        // YENI:
        // Xarici təchizatçı üçün orijinal valyutada cari borc.
        public decimal DebtAmountOriginalCurrency { get; set; } = 0;

        public string? Note { get; set; }
        // YENI:
        // Təchizatçının default valyutası.
        // Yerli təchizatçı üçün adətən AZN, xarici üçün USD/EUR/TRY ola bilər.
        public CurrencyType DefaultCurrency { get; set; } = CurrencyType.AZN;
        // YENI:
        // Təchizatçıya olan borcun AZN qarşılığı.
        public decimal DebtAmountLocal { get; set; }

        // YENI:
        // Təchizatçıya olan borcun orijinal valyutada qarşılığı.
        // Məsələn xarici təchizatçıya 1000 USD borc.
        public decimal DebtAmountOriginal { get; set; }

        // YENI:
        // Təchizatçı borc tarixçəsi.
        public ICollection<SupplierBalanceTransaction> BalanceTransactions { get; set; } = new List<SupplierBalanceTransaction>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();

        // YENI:
        // Borc/ödəniş tarixçəsi.
       
    }
}

