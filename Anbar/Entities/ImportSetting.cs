using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // İdxal qaiməsi davranış ayarlarını saxlayır.
    public class ImportSetting : BaseEntity
    {
        public bool EnableImportInvoice { get; set; } = true;
        public bool AutoOpenImportFieldsForForeignSupplier { get; set; } = true;
        public bool RequireDeclarationNumber { get; set; } = false;
        public bool RequireExchangeRate { get; set; } = true;
        public bool UseInvoiceDateExchangeRate { get; set; } = false;
        public bool IncludeCustomsDutyInCost { get; set; } = true;
        public bool IncludeBrokerFeeInCost { get; set; } = true;
        public bool IncludeInsuranceInCost { get; set; } = true;
        public bool IncludeTransportInCost { get; set; } = true;
    }
}
