using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
