using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{

    // YENI:
    // İdxal giriş qaiməsinin əlavə metadata məlumatları.
    //
    // VACIB:
    // Burada gömrük xərcləri saxlanmır.
    //
    // Gömrük xərcləri:
    // - InvoiceExpense
    // - Dynamic field
    // - ExpenseType
    //
    // sistemi ilə idarə olunur.
    //
    // Bu entity yalnız:
    // - idxal metadata
    // - gömrük sənədi
    // - valyuta
    // - declaration
    // məlumatlarını saxlayır.
    public class InvoiceImportInfo : BaseEntity
    {
        // Aid olduğu qaimə.
        public int InvoiceId { get; set; }

        public Invoice Invoice { get; set; } = null!;

        // YENI:
        // Gömrük bəyannamə nömrəsi.
        public string? DeclarationNumber { get; set; }

        // YENI:
        // Gömrük bəyannamə tarixi.
        public DateTime? DeclarationDate { get; set; }

        // YENI:
        // İdxal tarixi.
        public DateTime? ImportDate { get; set; }

        // YENI:
        // Malın gəldiyi ölkə.
        public string? OriginCountry { get; set; }

        // YENI:
        // İdxal olunan ölkə.
        public string? ImportCountry { get; set; }

        // YENI:
        // Gömrük postu.
        public string? CustomsPoint { get; set; }

        // YENI:
        // Xarici supplier adı.
        public string? ForeignSupplierName { get; set; }

        // YENI:
        // Xarici supplier VÖEN/VAT NO.
        public string? ForeignSupplierTaxNumber { get; set; }

        // YENI:
        // Xarici qaimə nömrəsi.
        public string? ForeignInvoiceNumber { get; set; }

        // YENI:
        // Xarici qaimə tarixi.
        public DateTime? ForeignInvoiceDate { get; set; }

        // YENI:
        // Daşıma sənədi nömrəsi.
        public string? TransportDocumentNumber { get; set; }

        // YENI:
        // Konteyner nömrəsi.
        public string? ContainerNumber { get; set; }

        // YENI:
        // HS Code / TN VED.
        public string? HsCode { get; set; }

        // YENI:
        // İncoterm.
        // EXW / FOB / CIF və s.
        public string? Incoterm { get; set; }

        // YENI:
        // Gömrük valyutası.
        public CurrencyType Currency { get; set; } = CurrencyType.USD;

        // YENI:
        // Gömrük məzənnəsi.
        public decimal ExchangeRate { get; set; } = 1;

        // YENI:
        // Məzənnə mənbəyi.
        public CurrencyRateSource CurrencyRateSource { get; set; } = CurrencyRateSource.Manual;

        // YENI:
        // Gömrükdə qəbul edilmiş ümumi məbləğ.
        public decimal CustomsDeclaredAmount { get; set; }

        // YENI:
        // Manual qeyd.
        public string? Note { get; set; }

        // YENI:
        // Dynamic custom field-lər.
        // Dövlət yeni field çıxarsa migration lazım olmayacaq.
        public ICollection<InvoiceDynamicFieldValue> DynamicFieldValues { get; set; }
            = new List<InvoiceDynamicFieldValue>();
        // YENI:
        // Köhnə kod compatibility üçün.
        [NotMapped]
        public string? DeclarationNo => DeclarationNumber;

        // YENI:
        // Köhnə kod compatibility üçün.
        [NotMapped]
        public decimal CustomsValue => CustomsDeclaredAmount;

        // YENI:
        // Legacy compatibility.
        // Artıq InvoiceExpense istifadə olunur.
        [NotMapped]
        public decimal CustomsDutyAmount => 0;

        // YENI:
        // Legacy compatibility.
        // Artıq InvoiceExpense istifadə olunur.
        [NotMapped]
        public decimal ImportVatAmount => 0;
    }
}