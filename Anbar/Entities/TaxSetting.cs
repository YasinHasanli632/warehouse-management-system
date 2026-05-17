using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Vergi ayarlarını saxlayır.
    public class TaxSetting : BaseEntity
    {
        public TaxRegime TaxRegime { get; set; } = TaxRegime.NoTax;

        public bool EnableVAT { get; set; } = false;
        public decimal VATPercent { get; set; } = 18;

        // YENI:
        // Alış qiyməti default ƏDV daxil sayılırmı.
        public bool PurchasePricesIncludeVATByDefault { get; set; } = true;

        // YENI:
        // ƏDV default əvəzləşdirilə bilən sayılırmı.
        public bool VATRecoverableByDefault { get; set; } = true;

        public bool EnableProfitTax { get; set; } = false;
        public decimal ProfitTaxPercent { get; set; } = 20;

        public bool EnableSimplifiedTax { get; set; } = false;
        public decimal SimplifiedTaxPercent { get; set; } = 2;

        // İdxal ƏDV-si maya dəyərinə daxil edilsinmi.
        public bool IncludeImportVATInCost { get; set; } = false;

        // YENI:
        // Gömrük rüsumu maya dəyərinə daxil edilsinmi.
        public bool IncludeCustomsDutyInCost { get; set; } = true;

        // YENI:
        // Aksiz maya dəyərinə daxil edilsinmi.
        public bool IncludeExciseInCost { get; set; } = true;
    }
}
