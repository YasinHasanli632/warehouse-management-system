using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity giriş və çıxış qaimələrinin başlıq məlumatlarını saxlayır.
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = null!;
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public bool IsImport { get; set; } = false;

        public CurrencyType Currency { get; set; } = CurrencyType.AZN;
        public decimal ExchangeRate { get; set; } = 1;

        public decimal OriginalItemsTotalAmount { get; set; }
        public decimal LocalItemsTotalAmount { get; set; }

        // Köhnə sahələr qalır.
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }

        public decimal OriginalPaidAmount { get; set; }
        public decimal OriginalDebtAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;

        public decimal ItemsTotalAmount { get; set; }
        public decimal ExtraExpenseAmount { get; set; }
        public decimal DiscountAmount { get; set; }

        // YENI:
        // ƏDV xaric məhsul toplamı. Enterprise ad.
        public decimal NetItemsAmount { get; set; }

        // YENI:
        // Köhnə servis/UI compatibility üçün.
        // Əgər haradasa Invoice.NetAmount çağırılırsa error verməsin.
        public decimal NetAmount { get; set; }

        // YENI:
        // Məhsul ƏDV toplamı.
        public decimal VatAmount { get; set; }

        // YENI:
        // ƏDV daxil məhsul toplamı. Enterprise ad.
        public decimal GrossItemsAmount { get; set; }

        // YENI:
        // Köhnə servis/UI compatibility üçün.
        // Əgər haradasa Invoice.GrossAmount çağırılırsa error verməsin.
        public decimal GrossAmount { get; set; }

        public decimal CostIncludedExpenseAmount { get; set; }
        public decimal CostIncludedTaxAmount { get; set; }

        public decimal RecoverableTaxAmount { get; set; }
        public decimal CostExcludedTaxAmount { get; set; }
        public decimal FinalCostAmount { get; set; }

        public CostRecalculationStatus CostStatus { get; set; } = CostRecalculationStatus.NotCalculated;

        public bool IsLocked { get; set; } = false;

        public string? Note { get; set; }

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

        public int? ParentInvoiceId { get; set; }
        public Invoice? ParentInvoice { get; set; }
        public ICollection<Invoice> ReturnInvoices { get; set; } = new List<Invoice>();

        public ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();
        public ICollection<InvoiceExpense> Expenses { get; set; } = new List<InvoiceExpense>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        public InvoiceImportInfo? ImportInfo { get; set; }

        public ICollection<SupplierBalanceTransaction> SupplierBalanceTransactions { get; set; } = new List<SupplierBalanceTransaction>();
        public ICollection<CustomerBalanceTransaction> CustomerBalanceTransactions { get; set; } = new List<CustomerBalanceTransaction>();

        public decimal OriginalTotalAmount { get; set; }
        public decimal LocalTotalAmount { get; set; }

        public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
        public ICollection<InvoiceTax> Taxes { get; set; } = new List<InvoiceTax>();

        public ICollection<InvoiceDynamicFieldValue> DynamicFieldValues { get; set; } = new List<InvoiceDynamicFieldValue>();

        // YENI:
        // Qaimə item-lərinin bütün vergi snapshot-ları.
        public ICollection<InvoiceItemTax> ItemTaxes { get; set; } = new List<InvoiceItemTax>();
        public InvoiceCostSummary? CostSummary { get; set; }
    }
}
