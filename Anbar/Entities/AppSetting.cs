using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity sistem ayarlarını saxlayır.
    // Məsələn default valyuta, mənfi stok icazəsi, avtomatik rəf yerləşdirmə.
    public class AppSetting : BaseEntity
    {
        // Proqramın görünən adı.
        public string AppName { get; set; } = "Mebel Anbar Sistemi";

        // Default valyuta.
        public CurrencyType DefaultCurrency { get; set; } = CurrencyType.AZN;

        // Mənfi stok icazəsi.
        public bool AllowNegativeStock { get; set; } = false;

        // Giriş qaiməsində avtomatik rəf seçimi aktivdirmi.
        public bool AutoShelfAssign { get; set; } = false;

        // Kritik stok xəbərdarlığı aktivdirmi.
        public bool EnableCriticalStockWarning { get; set; } = true;

        // Qaimə nömrəsi üçün prefix.
        public string InvoicePrefix { get; set; } = "QAI";

        // Şirkət adı.
        public string? CompanyName { get; set; }

        // Şirkət VÖEN.
        public string? CompanyVoen { get; set; }

        // Şirkət telefonu.
        public string? CompanyPhone { get; set; }

        // Şirkət ünvanı.
        public string? CompanyAddress { get; set; }
    }
}
