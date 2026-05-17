using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // Bu servis sadə tək admin login məntiqini idarə edir.
    // Burada JWT, role, permission yoxdur.
    // Sadəcə username/password yoxlanır və admin sistemə daxil olur.
    public class AuthService
    {
        private readonly AppDbContext _context;

        // DbContext servisin içinə verilir ki, admin məlumatını database-dən oxuya bilək.
        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        // Bu metod admin login üçün istifadə olunur.
        // Username və password qəbul edir, database-də yoxlayır.
        public async Task<Result<AdminUser>> LoginAsync(string username, string password)
        {
            // Boş username və password yoxlanır.
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Result<AdminUser>.Fail("İstifadəçi adı və şifrə boş ola bilməz.");
            }

            // Username-ə görə aktiv admin tapılır.
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.Username == username && x.IsActive);

            // Admin tapılmadısa xəta qaytarırıq.
            if (admin == null)
            {
                return Result<AdminUser>.Fail("İstifadəçi adı və ya şifrə yanlışdır.");
            }

            // Gələn password hash edilir və database-dəki hash ilə müqayisə olunur.
            var passwordHash = HashPassword(password);

            if (admin.PasswordHash != passwordHash)
            {
                return Result<AdminUser>.Fail("İstifadəçi adı və ya şifrə yanlışdır.");
            }

            // Son giriş tarixi yenilənir.
            admin.LastLoginAt = DateTime.Now;
            admin.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Login uğurlu olduqda admin datası qaytarılır.
            return Result<AdminUser>.Success(admin, "Giriş uğurla tamamlandı.");
        }

        // Bu metod admin şifrəsini dəyişmək üçün istifadə olunur.
        public async Task<Result<bool>> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            // Köhnə və yeni şifrə boş ola bilməz.
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Result<bool>.Fail("Köhnə və yeni şifrə boş ola bilməz.");
            }

            // Yeni şifrə minimum 4 simvol olsun.
            if (newPassword.Length < 4)
            {
                return Result<bool>.Fail("Yeni şifrə minimum 4 simvol olmalıdır.");
            }

            // Sistemdəki aktiv admin tapılır.
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.IsActive);

            if (admin == null)
            {
                return Result<bool>.Fail("Admin istifadəçi tapılmadı.");
            }

            // Köhnə şifrə yoxlanır.
            var oldHash = HashPassword(oldPassword);

            if (admin.PasswordHash != oldHash)
            {
                return Result<bool>.Fail("Köhnə şifrə yanlışdır.");
            }

            // Yeni şifrə hash edilir və yadda saxlanır.
            admin.PasswordHash = HashPassword(newPassword);
            admin.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Şifrə uğurla dəyişdirildi.");
        }

        // Bu metod proqram ilk dəfə açılanda default admin yaratmaq üçündür.
        // Əgər database-də admin yoxdursa, admin/admin123 yaradılır.
        public async Task<Result<bool>> EnsureDefaultAdminAsync()
        {
            // Database-də admin varmı yoxlayırıq.
            var hasAdmin = await _context.AdminUsers.AnyAsync();

            if (hasAdmin)
            {
                return Result<bool>.Success(true, "Admin istifadəçi artıq mövcuddur.");
            }

            // Default admin yaradılır.
            var admin = new AdminUser
            {
                Username = "admin",
                FullName = "Sistem Admini",
                Email = null,
                PasswordHash = HashPassword("admin123"),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.AdminUsers.AddAsync(admin);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Default admin yaradıldı. Username: admin, Password: admin123");
        }

        // Bu private metod şifrəni SHA256 ilə hash edir.
        // Sadə lokal WPF sistem üçün kifayətdir.
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hashBytes);
        }
    }
}
