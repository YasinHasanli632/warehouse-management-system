using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity sistemə daxil olacaq tək admin istifadəçini saxlayır.
    // Sistem sadə olacaq: rol, JWT, çox istifadəçi məntiqi yoxdur.
    public class AdminUser : BaseEntity
    {
        // Adminin istifadəçi adı.
        public string Username { get; set; } = null!;

        // Adminin tam adı.
        public string FullName { get; set; } = null!;

        // Şifrə hash formada saxlanacaq, plain text saxlamırıq.
        public string PasswordHash { get; set; } = null!;

        // Adminin emaili, istəyə bağlıdır.
        public string? Email { get; set; }

        // Son giriş tarixi.
        public DateTime? LastLoginAt { get; set; }
    }
}
