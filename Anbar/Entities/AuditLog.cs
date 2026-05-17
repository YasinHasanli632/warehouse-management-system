using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity sistemdə edilən əsas əməliyyatların tarixçəsini saxlayır.
    // Məsələn məhsul yaradıldı, qaimə təsdiqləndi, stok transfer edildi.
    public class AuditLog : BaseEntity
    {
        // Əməliyyat adı.
        public string Action { get; set; } = null!;

        // Hansı entity üzərində əməliyyat edilib.
        public string EntityName { get; set; } = null!;

        // Əməliyyat edilən record ID-si.
        public int? EntityId { get; set; }

        // Köhnə dəyər JSON və ya text kimi saxlanıla bilər.
        public string? OldValue { get; set; }

        // Yeni dəyər JSON və ya text kimi saxlanıla bilər.
        public string? NewValue { get; set; }

        // Əməliyyat edən istifadəçi adı.
        public string? PerformedBy { get; set; }

        // Əlavə qeyd.
        public string? Note { get; set; }
    }
}
