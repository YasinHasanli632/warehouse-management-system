using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Yerli alış ayarları üçün key-value dəyərlər.
    // Yeni ayar əlavə etmək üçün entity/migration dəyişməyə ehtiyac qalmır.
    public class LocalPurchaseSettingValue : BaseEntity
    {
        public int LocalPurchaseSettingId { get; set; }
        public LocalPurchaseSetting LocalPurchaseSetting { get; set; } = null!;

        public string Key { get; set; } = null!;

        public string Value { get; set; } = null!;

        public string ValueType { get; set; } = "String";

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public bool IsSystem { get; set; } = true;
    }
}
