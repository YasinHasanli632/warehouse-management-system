using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    public class AttributeService
    {
        private readonly AppDbContext _context;

        public AttributeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<AttributeDefinition>>> GetDefinitionsByCategoryAsync(int categoryId)
        {
            var definitions = await _context.AttributeDefinitions
                .Include(x => x.Values.Where(v => v.IsActive))
                .Where(x => x.CategoryId == categoryId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<AttributeDefinition>>.Success(definitions);
        }

        public async Task<Result<List<AttributeValue>>> GetValuesByDefinitionAsync(int definitionId)
        {
            var values = await _context.AttributeValues
                .Include(x => x.AttributeDefinition)
                .Where(x => x.AttributeDefinitionId == definitionId && x.IsActive)
                .OrderBy(x => x.Value)
                .ToListAsync();

            return Result<List<AttributeValue>>.Success(values);
        }

        public async Task<Result<AttributeDefinition>> CreateDefinitionAsync(int categoryId, string name)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Result<AttributeDefinition>.Fail("Xüsusiyyət adı boş ola bilməz.");

            var categoryExists = await _context.Categories
                .AnyAsync(x => x.Id == categoryId && x.IsActive);

            if (!categoryExists)
                return Result<AttributeDefinition>.Fail("Kateqoriya tapılmadı.");

            var exists = await _context.AttributeDefinitions
                .AnyAsync(x =>
                    x.CategoryId == categoryId &&
                    x.Name.ToLower() == name.ToLower() &&
                    x.IsActive);

            if (exists)
                return Result<AttributeDefinition>.Fail("Bu kateqoriya üçün belə xüsusiyyət artıq mövcuddur.");

            var definition = new AttributeDefinition
            {
                CategoryId = categoryId,
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _context.AttributeDefinitions.AddAsync(definition);
            await _context.SaveChangesAsync();

            return Result<AttributeDefinition>.Success(definition);
        }

        public async Task<Result<AttributeValue>> CreateValueAsync(int definitionId, string value)
        {
            value = value.Trim();

            if (string.IsNullOrWhiteSpace(value))
                return Result<AttributeValue>.Fail("Xüsusiyyət dəyəri boş ola bilməz.");

            var definitionExists = await _context.AttributeDefinitions
                .AnyAsync(x => x.Id == definitionId && x.IsActive);

            if (!definitionExists)
                return Result<AttributeValue>.Fail("Xüsusiyyət başlığı tapılmadı.");

            var exists = await _context.AttributeValues
                .AnyAsync(x =>
                    x.AttributeDefinitionId == definitionId &&
                    x.Value.ToLower() == value.ToLower() &&
                    x.IsActive);

            if (exists)
                return Result<AttributeValue>.Fail("Bu xüsusiyyət dəyəri artıq mövcuddur.");

            var attributeValue = new AttributeValue
            {
                AttributeDefinitionId = definitionId,
                Value = value,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _context.AttributeValues.AddAsync(attributeValue);
            await _context.SaveChangesAsync();

            return Result<AttributeValue>.Success(attributeValue);
        }

        // 🔥 ƏN VACİB HİSSƏ — DELETE PROTECTION

        public async Task<Result<bool>> DeleteDefinitionAsync(int id)
        {
            var definition = await _context.AttributeDefinitions
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (definition == null)
                return Result<bool>.Fail("Xüsusiyyət tapılmadı.");

            // 🔴 İSTİFADƏ OLUNUBSA SİLİNMƏSİN
            var isUsed = await _context.ProductAttributes
                .AnyAsync(x => x.AttributeDefinitionId == id && x.IsActive);

            if (isUsed)
                return Result<bool>.Fail("Bu xüsusiyyət məhsullarda istifadə olunur və silinə bilməz.");

            definition.IsActive = false;
            definition.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Xüsusiyyət silindi.");
        }

        public async Task<Result<bool>> DeleteValueAsync(int id)
        {
            var value = await _context.AttributeValues
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (value == null)
                return Result<bool>.Fail("Xüsusiyyət dəyəri tapılmadı.");

            // 🔴 İSTİFADƏ OLUNUBSA SİLİNMƏSİN
            var isUsed = await _context.ProductAttributes
                .AnyAsync(x => x.AttributeValueId == id && x.IsActive);

            if (isUsed)
                return Result<bool>.Fail("Bu dəyər məhsullarda istifadə olunur və silinə bilməz.");

            value.IsActive = false;
            value.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Xüsusiyyət dəyəri silindi.");
        }

        public async Task<Result<string>> GetProductAttributesTextAsync(int productId)
        {
            var attributes = await _context.ProductAttributes
                .Include(x => x.AttributeValue)
                    .ThenInclude(x => x.AttributeDefinition)
                .Where(x =>
                    x.ProductId == productId &&
                    x.IsActive &&
                    x.AttributeValue.IsActive &&
                    x.AttributeValue.AttributeDefinition.IsActive)
                .OrderBy(x => x.AttributeValue.AttributeDefinition.Name)
                .ToListAsync();

            if (!attributes.Any())
                return Result<string>.Success("Xüsusiyyət yoxdur");

            var text = string.Join(", ", attributes.Select(x =>
                $"{x.AttributeValue.AttributeDefinition.Name}: {x.AttributeValue.Value}"));

            return Result<string>.Success(text);
        }
    }
}