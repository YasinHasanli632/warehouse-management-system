using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity hansı rəfdə hansı məhsuldan nə qədər olduğunu saxlayır.
    // Anbar xəritəsi və rəf doluluğu əsasən bu cədvəldən hesablanacaq.
    public class ShelfStock : BaseEntity
    {
        // Məhsul ID-si.
        public int ProductId { get; set; }

        // Məhsul obyekti.
        public Product Product { get; set; } = null!;

        // Rəf ID-si.
        public int ShelfId { get; set; }

        // Rəf obyekti.
        public Shelf Shelf { get; set; } = null!;

        // Həmin rəfdə məhsuldan olan miqdar.
        public decimal Quantity { get; set; }

        // Bu rəfdə həmin məhsul üzrə son hərəkət tarixi.
        public DateTime? LastMovementDate { get; set; }
    }
}
