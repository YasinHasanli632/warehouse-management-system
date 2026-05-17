using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity tək anbar məlumatını saxlayır.
    // Sistem kiçik anbar üçün nəzərdə tutulur.
    public class Warehouse : BaseEntity
    {
        // Anbarın adı.
        public string Name { get; set; } = null!;

        // Anbarın ünvanı.
        public string? Address { get; set; }

        // Anbar haqqında əlavə qeyd.
        public string? Description { get; set; }

        // Bu anbardakı bütün rəflər.
        public ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();
        public string Code { get;  set; }
    }
}
