using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Common
{
    // Bu base class bütün entity-lərdə ortaq istifadə olunacaq.
    // Məqsəd: hər cədvəldə Id, yaradılma tarixi, yenilənmə tarixi və aktiv/passiv status saxlamaqdır.
    public abstract class BaseEntity
    {
        // Hər record üçün unikal ID-dir.
        public int Id { get; set; }

        // Record-un yaradıldığı tarixdir.
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Record son dəfə dəyişdiriləndə bu tarix yenilənəcək.
        public DateTime? UpdatedAt { get; set; }

        // Silmək əvəzinə passiv etmək üçün istifadə olunur.
        public bool IsActive { get; set; } = true;
    }
}
