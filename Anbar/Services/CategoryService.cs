using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // Bu servis kateqoriya məntiqini idarə edir.
    // Əsas kateqoriya, alt kateqoriya, düzəliş və passiv etmə əməliyyatları buradadır.
    public class CategoryService
    {
        private readonly AppDbContext _context;

        // DbContext database əməliyyatları üçün istifadə olunur.
        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        // Bütün aktiv kateqoriyaları gətirir.
        public async Task<Result<List<Category>>> GetAllAsync()
        {
            var categories = await _context.Categories
                .Include(x => x.ParentCategory).Include(x=>x.DefaultUnit)
                .Include(x => x.SubCategories.Where(s => s.IsActive))
                .Include(x => x.Products.Where(p => p.IsActive))
                .Where(x => x.IsActive)
                .OrderBy(x => x.ParentCategoryId)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return Result<List<Category>>.Success(categories);
        }

        // Sadəcə əsas kateqoriyaları gətirir.
        // ParentCategoryId null olanlar əsas kateqoriyadır.
        public async Task<Result<List<Category>>> GetMainCategoriesAsync()
        {
            var categories = await _context.Categories
               .Include(x => x.DefaultUnit).Include(x => x.SubCategories.Where(s => s.IsActive))
                .Where(x => x.IsActive && x.ParentCategoryId == null)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<Category>>.Success(categories);
        }

        // Seçilmiş əsas kateqoriyanın alt kateqoriyalarını gətirir.
        public async Task<Result<List<Category>>> GetSubCategoriesAsync(int parentCategoryId)
        {
            var parentExists = await _context.Categories
                .AnyAsync(x => x.Id == parentCategoryId && x.IsActive);

            if (!parentExists)
            {
                return Result<List<Category>>.Fail("Ana kateqoriya tapılmadı.");
            }

            var subCategories = await _context.Categories
                .Where(x => x.IsActive && x.ParentCategoryId == parentCategoryId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<Category>>.Success(subCategories);
        }

        // ID-yə görə kateqoriya detalını gətirir.
        public async Task<Result<Category>> GetByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(x => x.ParentCategory).Include(x => x.DefaultUnit)
                .Include(x => x.SubCategories.Where(s => s.IsActive))
                .Include(x => x.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (category == null)
            {
                return Result<Category>.Fail("Kateqoriya tapılmadı.");
            }

            return Result<Category>.Success(category);
        }

        // Yeni kateqoriya yaradır.
        // Əgər parentCategoryId null göndərilsə əsas kateqoriya yaranır.
        // Əgər parentCategoryId göndərilsə alt kateqoriya yaranır.
        public async Task<Result<Category>> CreateAsync(
    string name,
    string? description = null,
    int? parentCategoryId = null,
    int? defaultUnitId = null) // YENI
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Category>.Fail("Kateqoriya adı boş ola bilməz.");
            }

            var normalizedName = name.Trim();

            if (parentCategoryId.HasValue)
            {
                var parentCategory = await _context.Categories
                    .FirstOrDefaultAsync(x => x.Id == parentCategoryId.Value && x.IsActive);

                if (parentCategory == null)
                {
                    return Result<Category>.Fail("Ana kateqoriya tapılmadı.");
                }
            }

            var exists = await _context.Categories.AnyAsync(x =>
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower() &&
                x.ParentCategoryId == parentCategoryId);

            if (exists)
            {
                return Result<Category>.Fail("Bu adda kateqoriya artıq mövcuddur.");
            }
            // YENI:
            // Əgər kateqoriyaya default ölçü vahidi seçilibsə, həmin vahid database-də aktiv olmalıdır.
            if (defaultUnitId.HasValue)
            {
                var unitExists = await _context.Units
                    .AnyAsync(x => x.Id == defaultUnitId.Value && x.IsActive);

                if (!unitExists)
                {
                    return Result<Category>.Fail("Seçilmiş ölçü vahidi tapılmadı.");
                }
            }
            var category = new Category
            {
                Name = normalizedName,
                Description = description,
                ParentCategoryId = parentCategoryId,
                CreatedAt = DateTime.Now,
                IsActive = true,
                DefaultUnitId = defaultUnitId, // YENI
            };

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            return Result<Category>.Success(category, "Kateqoriya uğurla yaradıldı.");
        }

        // Mövcud kateqoriyanı yeniləyir.
        public async Task<Result<Category>> UpdateAsync(
            int id,
            string name,
            string? description = null,
            int? parentCategoryId = null,
            int? defaultUnitId = null) // YENI
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Category>.Fail("Kateqoriya adı boş ola bilməz.");
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (category == null)
            {
                return Result<Category>.Fail("Kateqoriya tapılmadı.");
            }

            if (parentCategoryId.HasValue)
            {
                if (parentCategoryId.Value == id)
                {
                    return Result<Category>.Fail("Kateqoriya özü özünün ana kateqoriyası ola bilməz.");
                }

                var parentCategory = await _context.Categories
                    .FirstOrDefaultAsync(x => x.Id == parentCategoryId.Value && x.IsActive);

                if (parentCategory == null)
                {
                    return Result<Category>.Fail("Ana kateqoriya tapılmadı.");
                }
            }

            var normalizedName = name.Trim();

            var exists = await _context.Categories.AnyAsync(x =>
                x.Id != id &&
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower() &&
                x.ParentCategoryId == parentCategoryId);

            if (exists)
            {
                return Result<Category>.Fail("Bu adda kateqoriya artıq mövcuddur.");
            }
            // YENI:
            // Update zamanı da ölçü vahidinin aktiv olub-olmadığını yoxlayırıq.
            if (defaultUnitId.HasValue)
            {
                var unitExists = await _context.Units
                    .AnyAsync(x => x.Id == defaultUnitId.Value && x.IsActive);

                if (!unitExists)
                {
                    return Result<Category>.Fail("Seçilmiş ölçü vahidi tapılmadı.");
                }
            }
            category.Name = normalizedName;
            category.Description = description;
            category.ParentCategoryId = parentCategoryId;
            category.UpdatedAt = DateTime.Now;
            category.DefaultUnitId = defaultUnitId; // YENI
            await _context.SaveChangesAsync();

            return Result<Category>.Success(category, "Kateqoriya uğurla yeniləndi.");
        }

        // Kateqoriyanı passiv edir.
        // Əgər altında aktiv məhsul, alt kateqoriya və ya xüsusiyyət varsa, silməyə icazə vermir.
        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            var category = await _context.Categories
                .Include(x => x.Products)
                .Include(x => x.SubCategories)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (category == null)
            {
                return Result<bool>.Fail("Kateqoriya tapılmadı.");
            }

            var hasActiveProducts = category.Products.Any(x => x.IsActive);

            if (hasActiveProducts)
            {
                return Result<bool>.Fail("Bu kateqoriyaya bağlı aktiv məhsullar var. Əvvəl məhsulları başqa kateqoriyaya keçirin və ya passiv edin.");
            }

            var hasActiveSubCategories = category.SubCategories.Any(x => x.IsActive);

            if (hasActiveSubCategories)
            {
                return Result<bool>.Fail("Bu kateqoriyanın aktiv alt kateqoriyaları var. Əvvəl alt kateqoriyaları passiv edin.");
            }

            // YENI: Kateqoriyaya bağlı aktiv xüsusiyyət başlığı varsa kateqoriya silinməsin.
            var hasActiveAttributes = await _context.AttributeDefinitions
                .AnyAsync(x => x.CategoryId == id && x.IsActive);

            if (hasActiveAttributes)
            {
                return Result<bool>.Fail("Bu kateqoriyaya bağlı aktiv xüsusiyyətlər var. Əvvəl xüsusiyyətləri silin və ya passiv edin.");
            }

            category.IsActive = false;
            category.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Kateqoriya passiv edildi.");
        }
    }
}