using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Qaimə vergisinin/rüsumunun hansı məhsula nə qədər paylandığını saxlayır.
    // Məsələn gömrük rüsumu 100 AZN-dir, bunun 30 AZN-i qapıya, 70 AZN-i profillərə düşür.
    public class InvoiceTaxAllocation : BaseEntity
    {
        public int InvoiceTaxId { get; set; }
        public InvoiceTax InvoiceTax { get; set; } = null!;

        public int InvoiceItemId { get; set; }
        public InvoiceItem InvoiceItem { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public decimal AllocatedAmount { get; set; }
        public decimal UnitAllocatedAmount { get; set; }

        public int? InvoiceItemTaxId { get; set; }
        public InvoiceItemTax? InvoiceItemTax { get; set; }
        public string? Note { get; set; }
    }
}
