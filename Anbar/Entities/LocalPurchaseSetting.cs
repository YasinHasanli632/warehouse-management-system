using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Yerli alış ayarlarının başlıq entity-si.
    // Əsas məqsəd: yerli alış modulunu ayrıca idarə etmək.
    public class LocalPurchaseSetting : BaseEntity
    {
        public string Name { get; set; } = "Yerli Alış Ayarları";

        public string Code { get; set; } = "LOCAL_PURCHASE";

        public string? Description { get; set; }

        public ICollection<LocalPurchaseSettingValue> Values { get; set; } = new List<LocalPurchaseSettingValue>();
    }
}
