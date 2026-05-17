using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity kateqoriyaya aid xüsusiyyət başlığını saxlayır.
    // Məsələn: Rəng, Ölçü, Material.
    public class AttributeDefinition : BaseEntity
    {
        // Xüsusiyyət adı.
        // Məsələn: Rəng, Ölçü, Material.
        public string Name { get; set; } = null!;

        // Bu xüsusiyyət hansı kateqoriyaya aiddirsə onun Id-si.
        // Məsələn: Qapı kateqoriyasında Rəng, Ölçü, Material ola bilər.
        public int CategoryId { get; set; }

        // Aid olduğu kateqoriya.
        public Category Category { get; set; } = null!;

        // Bu xüsusiyyətin mümkün dəyərləri.
        // Məsələn Rəng üçün: Ağ, Qara, Boz.
        public ICollection<AttributeValue> Values { get; set; } = new List<AttributeValue>();
    }
}
