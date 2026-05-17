using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // Bu servis ölçü vahidlərini idarə edir.
    // Ölçü vahidləri enum kimi yox, database table kimi saxlanır.
    // Məsələn: Ədəd, Kiloqram, Qram, Litr, Metr, Qutu, Paket və s.
    public class UnitService
    {
        private readonly AppDbContext _context;

        // DbContext database əməliyyatları üçün istifadə olunur.
        public UnitService(AppDbContext context)
        {
            _context = context;
        }

        // Bütün aktiv ölçü vahidlərini gətirir.
        // UI ComboBox üçün əsas istifadə olunacaq metod budur.
        public async Task<Result<List<Unit>>> GetActiveAsync()
        {
            var units = await _context.Units
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return Result<List<Unit>>.Success(units);
        }

        // Bütün ölçü vahidlərini gətirir.
        // Admin paneldə aktiv/passiv vahidləri görmək üçün istifadə oluna bilər.
        public async Task<Result<List<Unit>>> GetAllAsync()
        {
            var units = await _context.Units
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return Result<List<Unit>>.Success(units);
        }

        // ID-yə görə ölçü vahidini gətirir.
        public async Task<Result<Unit>> GetByIdAsync(int id)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(x => x.Id == id);

            if (unit == null)
            {
                return Result<Unit>.Fail("Ölçü vahidi tapılmadı.");
            }

            return Result<Unit>.Success(unit);
        }

        // Sistemin default ölçü vahidini gətirir.
        // Adətən Ədəd default olur.
        public async Task<Result<Unit>> GetDefaultAsync()
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(x => x.IsActive && x.IsDefault);

            if (unit == null)
            {
                return Result<Unit>.Fail("Default ölçü vahidi tapılmadı.");
            }

            return Result<Unit>.Success(unit);
        }

        // Yeni ölçü vahidi yaradır.
        public async Task<Result<Unit>> CreateAsync(
            string key,
            string name,
            string symbol,
            int sortOrder = 0,
            bool isDefault = false)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Result<Unit>.Fail("Ölçü vahidinin açarı boş ola bilməz.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Unit>.Fail("Ölçü vahidinin adı boş ola bilməz.");
            }

            if (string.IsNullOrWhiteSpace(symbol))
            {
                return Result<Unit>.Fail("Ölçü vahidinin simvolu boş ola bilməz.");
            }

            var normalizedKey = NormalizeKey(key);
            var normalizedName = name.Trim();
            var normalizedSymbol = symbol.Trim();

            var keyExists = await _context.Units
                .AnyAsync(x => x.Key.ToLower() == normalizedKey.ToLower());

            if (keyExists)
            {
                return Result<Unit>.Fail("Bu açarla ölçü vahidi artıq mövcuddur.");
            }

            var nameExists = await _context.Units
                .AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower());

            if (nameExists)
            {
                return Result<Unit>.Fail("Bu adda ölçü vahidi artıq mövcuddur.");
            }

            // Əgər yeni yaradılan vahid default olacaqsa,
            // əvvəlki default vahidləri default-dan çıxarırıq.
            if (isDefault)
            {
                await ClearDefaultUnitsAsync();
            }

            var unit = new Unit
            {
                Key = normalizedKey,
                Name = normalizedName,
                Symbol = normalizedSymbol,
                SortOrder = sortOrder,
                IsDefault = isDefault,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _context.Units.AddAsync(unit);
            await _context.SaveChangesAsync();

            return Result<Unit>.Success(unit, "Ölçü vahidi uğurla yaradıldı.");
        }

        // Mövcud ölçü vahidini yeniləyir.
        public async Task<Result<Unit>> UpdateAsync(
            int id,
            string key,
            string name,
            string symbol,
            int sortOrder = 0,
            bool isDefault = false)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(x => x.Id == id);

            if (unit == null)
            {
                return Result<Unit>.Fail("Ölçü vahidi tapılmadı.");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return Result<Unit>.Fail("Ölçü vahidinin açarı boş ola bilməz.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Unit>.Fail("Ölçü vahidinin adı boş ola bilməz.");
            }

            if (string.IsNullOrWhiteSpace(symbol))
            {
                return Result<Unit>.Fail("Ölçü vahidinin simvolu boş ola bilməz.");
            }

            var normalizedKey = NormalizeKey(key);
            var normalizedName = name.Trim();
            var normalizedSymbol = symbol.Trim();

            var keyExists = await _context.Units
                .AnyAsync(x =>
                    x.Id != id &&
                    x.Key.ToLower() == normalizedKey.ToLower());

            if (keyExists)
            {
                return Result<Unit>.Fail("Bu açarla başqa ölçü vahidi artıq mövcuddur.");
            }

            var nameExists = await _context.Units
                .AnyAsync(x =>
                    x.Id != id &&
                    x.Name.ToLower() == normalizedName.ToLower());

            if (nameExists)
            {
                return Result<Unit>.Fail("Bu adda başqa ölçü vahidi artıq mövcuddur.");
            }

            // Əgər bu vahid default seçilibsə,
            // digər vahidlərin default statusunu söndürürük.
            if (isDefault)
            {
                await ClearDefaultUnitsAsync(id);
            }
            else
            {
                // Əgər user mövcud default vahidi default-dan çıxarırsa,
                // sistemdə başqa default vahid olub-olmadığını yoxlayırıq.
                if (unit.IsDefault)
                {
                    var hasAnotherDefault = await _context.Units
                        .AnyAsync(x => x.Id != id && x.IsActive && x.IsDefault);

                    if (!hasAnotherDefault)
                    {
                        return Result<Unit>.Fail("Sistemdə ən azı bir default ölçü vahidi qalmalıdır.");
                    }
                }
            }

            unit.Key = normalizedKey;
            unit.Name = normalizedName;
            unit.Symbol = normalizedSymbol;
            unit.SortOrder = sortOrder;
            unit.IsDefault = isDefault;
            unit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<Unit>.Success(unit, "Ölçü vahidi uğurla yeniləndi.");
        }

        // Ölçü vahidini passiv edir.
        // Əgər kateqoriya və ya məhsulda istifadə olunursa, passiv etməyə icazə vermirik.
        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (unit == null)
            {
                return Result<bool>.Fail("Ölçü vahidi tapılmadı.");
            }

            var usedInCategories = await _context.Categories
                .AnyAsync(x => x.IsActive && x.DefaultUnitId == id);

            if (usedInCategories)
            {
                return Result<bool>.Fail("Bu ölçü vahidi aktiv kateqoriyada istifadə olunur. Əvvəl kateqoriyadan dəyişin.");
            }

            var usedInProducts = await _context.Products
                .AnyAsync(x => x.IsActive && x.UnitId == id);

            if (usedInProducts)
            {
                return Result<bool>.Fail("Bu ölçü vahidi aktiv məhsulda istifadə olunur. Əvvəl məhsullardan dəyişin.");
            }

            if (unit.IsDefault)
            {
                var hasAnotherDefault = await _context.Units
                    .AnyAsync(x => x.Id != id && x.IsActive && x.IsDefault);

                if (!hasAnotherDefault)
                {
                    return Result<bool>.Fail("Default ölçü vahidini passiv etmək olmaz. Əvvəl başqa vahidi default edin.");
                }
            }

            unit.IsActive = false;
            unit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Ölçü vahidi passiv edildi.");
        }

        // Passiv edilmiş ölçü vahidini yenidən aktiv edir.
        public async Task<Result<bool>> ActivateAsync(int id)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(x => x.Id == id);

            if (unit == null)
            {
                return Result<bool>.Fail("Ölçü vahidi tapılmadı.");
            }

            if (unit.IsActive)
            {
                return Result<bool>.Success(true, "Ölçü vahidi artıq aktivdir.");
            }

            unit.IsActive = true;
            unit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Ölçü vahidi aktiv edildi.");
        }

        // İlk dəfə sistem açılarkən ölçü vahidləri yoxdursa, əsas vahidləri yaradır.
        // Əgər AppDbContext HasData ilə seed edirsənsə, bu metod məcburi deyil,
        // amma servis səviyyəsində saxlamaq gələcəkdə rahatdır.
        public async Task<Result<bool>> SeedDefaultUnitsAsync()
        {
            var anyUnit = await _context.Units.AnyAsync();

            if (anyUnit)
            {
                return Result<bool>.Success(true, "Ölçü vahidləri artıq mövcuddur.");
            }

            var units = new List<Unit>
            {
                new Unit
                {
                    Key = "eded",
                    Name = "Ədəd",
                    Symbol = "əd",
                    SortOrder = 1,
                    IsDefault = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "kg",
                    Name = "Kiloqram",
                    Symbol = "kq",
                    SortOrder = 2,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "qram",
                    Name = "Qram",
                    Symbol = "qr",
                    SortOrder = 3,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "litr",
                    Name = "Litr",
                    Symbol = "l",
                    SortOrder = 4,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "metr",
                    Name = "Metr",
                    Symbol = "m",
                    SortOrder = 5,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "m2",
                    Name = "Kvadrat metr",
                    Symbol = "m²",
                    SortOrder = 6,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "m3",
                    Name = "Kub metr",
                    Symbol = "m³",
                    SortOrder = 7,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "qutu",
                    Name = "Qutu",
                    Symbol = "qutu",
                    SortOrder = 8,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Unit
                {
                    Key = "paket",
                    Name = "Paket",
                    Symbol = "pkt",
                    SortOrder = 9,
                    IsDefault = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            await _context.Units.AddRangeAsync(units);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Default ölçü vahidləri yaradıldı.");
        }

        // Daxili metod: key dəyərini sistemə uyğun formaya salır.
        // Məsələn: "Kiloqram" -> "kiloqram", "Kvadrat metr" -> "kvadrat_metr".
        private static string NormalizeKey(string key)
        {
            return key
                .Trim()
                .ToLower()
                .Replace(" ", "_");
        }

        // Daxili metod: bütün default-ları söndürür.
        private async Task ClearDefaultUnitsAsync(int? exceptId = null)
        {
            var defaultUnits = await _context.Units
                .Where(x => x.IsDefault && (!exceptId.HasValue || x.Id != exceptId.Value))
                .ToListAsync();

            foreach (var defaultUnit in defaultUnits)
            {
                defaultUnit.IsDefault = false;
                defaultUnit.UpdatedAt = DateTime.Now;
            }
        }
    }
}