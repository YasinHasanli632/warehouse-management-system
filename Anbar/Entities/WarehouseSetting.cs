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
    // Şirkət/anbar üzrə ümumi ayarları saxlayır.
    public class WarehouseSetting : BaseEntity
    {
        public string AppName { get; set; } = "Mebel Anbar Sistemi";
        public string? CompanyName { get; set; }
        public string? CompanyVoen { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyAddress { get; set; }
        public CurrencyType DefaultCurrency { get; set; } = CurrencyType.AZN;
    }
}
