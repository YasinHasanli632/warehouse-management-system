using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anbar.Services
{
    public class ShelfService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;

        public ShelfService(AppDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public ShelfService(AppDbContext context)
        {
            _context = context;
            _stockService = new StockService(context);
        }

        public async Task<Result<List<Shelf>>> GetAllAsync()
        {
            var shelves = await _context.Shelves
                .Include(x => x.Warehouse)
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                    .ThenInclude(x => x.ShelfAttributeDefinition)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.AttributeDefinition)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Zone)
                .ThenBy(x => x.RowNumber)
                .ToListAsync();

            return Result<List<Shelf>>.Success(shelves);
        }

        public async Task<Result<Shelf>> GetByIdAsync(int id)
        {
            var shelf = await _context.Shelves
                .Include(x => x.Warehouse)
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                    .ThenInclude(x => x.ShelfAttributeDefinition)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.AttributeDefinition)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (shelf == null)
                return Result<Shelf>.Fail("Rəf tapılmadı.");

            return Result<Shelf>.Success(shelf);
        }

        public async Task<Result<Shelf>> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Result<Shelf>.Fail("Rəf kodu boş ola bilməz.");

            var normalizedCode = code.Trim().ToUpper();

            var shelf = await _context.Shelves
                .Include(x => x.Warehouse)
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                    .ThenInclude(x => x.ShelfAttributeDefinition)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.AttributeDefinition)
                .FirstOrDefaultAsync(x => x.Code.ToUpper() == normalizedCode && x.IsActive);

            if (shelf == null)
                return Result<Shelf>.Fail("Rəf tapılmadı.");

            return Result<Shelf>.Success(shelf);
        }

        public async Task<Result<List<ShelfAttributeDefinition>>> GetShelfAttributeDefinitionsAsync()
        {
            var definitions = await _context.ShelfAttributeDefinitions
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<ShelfAttributeDefinition>>.Success(definitions);
        }

        public async Task<Result<ShelfAttributeDefinition>> CreateShelfAttributeDefinitionAsync(
            string name,
            string key,
            string? unit,
            bool isLimit = true,
            bool isNumeric = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<ShelfAttributeDefinition>.Fail("Xüsusiyyət adı boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(key))
                return Result<ShelfAttributeDefinition>.Fail("Xüsusiyyət açarı boş ola bilməz.");

            var normalizedKey = key.Trim();

            var exists = await _context.ShelfAttributeDefinitions
                .AnyAsync(x => x.Key.ToLower() == normalizedKey.ToLower() && x.IsActive);

            if (exists)
                return Result<ShelfAttributeDefinition>.Fail("Bu xüsusiyyət açarı artıq mövcuddur.");

            var definition = new ShelfAttributeDefinition
            {
                Name = name.Trim(),
                Key = normalizedKey,
                Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(),
                IsLimit = isLimit,
                IsNumeric = isNumeric,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.ShelfAttributeDefinitions.AddAsync(definition);
            await _context.SaveChangesAsync();

            return Result<ShelfAttributeDefinition>.Success(definition, "Rəf xüsusiyyəti yaradıldı.");
        }

        public async Task<Result<Shelf>> CreateAsync(
            string code,
            string zone,
            int rowNumber,
            decimal capacity,
            int warehouseId)
        {
            return await CreateAsync(
                code,
                zone,
                rowNumber,
                capacity,
                warehouseId,
                new List<ShelfAttributeInput>());
        }

        public async Task<Result<Shelf>> CreateAsync(
            string code,
            string zone,
            int rowNumber,
            decimal capacity,
            int warehouseId,
            List<ShelfAttributeInput>? attributes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Result<Shelf>.Fail("Rəf kodu boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(zone))
                return Result<Shelf>.Fail("Zona boş ola bilməz.");

            if (rowNumber <= 0)
                return Result<Shelf>.Fail("Sıra nömrəsi 0-dan böyük olmalıdır.");

            if (capacity < 0)
                return Result<Shelf>.Fail("Rəf tutumu mənfi ola bilməz.");

            var warehouseExists = await _context.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive);
            if (!warehouseExists)
                return Result<Shelf>.Fail("Anbar tapılmadı.");

            var normalizedCode = code.Trim().ToUpper();

            var codeExists = await _context.Shelves.AnyAsync(x => x.Code.ToUpper() == normalizedCode && x.IsActive);
            if (codeExists)
                return Result<Shelf>.Fail("Bu rəf kodu artıq mövcuddur.");

            var attributeValidation = await ValidateShelfAttributesAsync(attributes);
            if (!attributeValidation.IsSuccess)
                return Result<Shelf>.Fail(attributeValidation.Message);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var shelf = new Shelf
                {
                    Code = normalizedCode,
                    Zone = zone.Trim().ToUpper(),
                    RowNumber = rowNumber,
                    Capacity = capacity,
                    OccupancyPercent = 0,
                    Status = ShelfStatus.Empty,
                    WarehouseId = warehouseId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.Shelves.AddAsync(shelf);
                await _context.SaveChangesAsync();

                var saveAttributesResult = await SaveShelfAttributesAsync(
                    shelf.Id,
                    attributes,
                    saveChanges: false);

                if (!saveAttributesResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return Result<Shelf>.Fail(saveAttributesResult.Message);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<Shelf>.Success(shelf, "Rəf uğurla yaradıldı.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<Shelf>.Fail($"Rəf yaradılarkən xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<Shelf>> UpdateAsync(
            int id,
            string code,
            string zone,
            int rowNumber,
            decimal capacity)
        {
            return await UpdateAsync(
                id,
                code,
                zone,
                rowNumber,
                capacity,
                null);
        }

        public async Task<Result<Shelf>> UpdateAsync(
            int id,
            string code,
            string zone,
            int rowNumber,
            decimal capacity,
            List<ShelfAttributeInput>? attributes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Result<Shelf>.Fail("Rəf kodu boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(zone))
                return Result<Shelf>.Fail("Zona boş ola bilməz.");

            if (rowNumber <= 0)
                return Result<Shelf>.Fail("Sıra nömrəsi 0-dan böyük olmalıdır.");

            if (capacity < 0)
                return Result<Shelf>.Fail("Rəf tutumu mənfi ola bilməz.");

            var shelf = await _context.Shelves
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                    .ThenInclude(x => x.ShelfAttributeDefinition)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (shelf == null)
                return Result<Shelf>.Fail("Rəf tapılmadı.");

            var totalStock = await _context.ShelfStocks
                .Where(x => x.ShelfId == id && x.IsActive)
                .SumAsync(x => x.Quantity);

            if (capacity > 0 && capacity < totalStock)
                return Result<Shelf>.Fail($"Yeni tutum rəfdəki mövcud stokdan az ola bilməz. Mövcud stok: {totalStock:0.##}");

            var normalizedCode = code.Trim().ToUpper();

            var codeExists = await _context.Shelves.AnyAsync(x =>
                x.Id != id &&
                x.Code.ToUpper() == normalizedCode &&
                x.IsActive);

            if (codeExists)
                return Result<Shelf>.Fail("Bu rəf kodu artıq mövcuddur.");

            var attributeValidation = await ValidateShelfAttributesAsync(attributes);
            if (!attributeValidation.IsSuccess)
                return Result<Shelf>.Fail(attributeValidation.Message);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                shelf.Code = normalizedCode;
                shelf.Zone = zone.Trim().ToUpper();
                shelf.RowNumber = rowNumber;
                shelf.Capacity = capacity;
                shelf.UpdatedAt = DateTime.Now;

                if (attributes != null)
                {
                    var saveAttributesResult = await SaveShelfAttributesAsync(
                        id,
                        attributes,
                        saveChanges: false);

                    if (!saveAttributesResult.IsSuccess)
                    {
                        await transaction.RollbackAsync();
                        return Result<Shelf>.Fail(saveAttributesResult.Message);
                    }
                }

                await _context.SaveChangesAsync();

                var recalculateResult = await _stockService.UpdateShelfStatusAsync(id, saveChanges: false);

                if (!recalculateResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return Result<Shelf>.Fail(recalculateResult.Message);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<Shelf>.Success(shelf, "Rəf uğurla yeniləndi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<Shelf>.Fail($"Rəf yenilənərkən xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SaveShelfAttributesAsync(
            int shelfId,
            List<ShelfAttributeInput>? attributes,
            bool saveChanges = true)
        {
            var shelfExists = await _context.Shelves.AnyAsync(x => x.Id == shelfId && x.IsActive);
            if (!shelfExists)
                return Result<bool>.Fail("Rəf tapılmadı.");

            var validation = await ValidateShelfAttributesAsync(attributes);
            if (!validation.IsSuccess)
                return Result<bool>.Fail(validation.Message);

            var oldValues = await _context.ShelfAttributeValues
                .Where(x => x.ShelfId == shelfId && x.IsActive)
                .ToListAsync();

            foreach (var oldValue in oldValues)
            {
                oldValue.IsActive = false;
                oldValue.UpdatedAt = DateTime.Now;
            }

            if (attributes != null && attributes.Any())
            {
                foreach (var input in attributes)
                {
                    var hasNumeric = input.NumericValue.HasValue;
                    var hasText = !string.IsNullOrWhiteSpace(input.TextValue);

                    if (!hasNumeric && !hasText)
                        continue;

                    var value = new ShelfAttributeValue
                    {
                        ShelfId = shelfId,
                        ShelfAttributeDefinitionId = input.ShelfAttributeDefinitionId,
                        NumericValue = input.NumericValue,
                        TextValue = string.IsNullOrWhiteSpace(input.TextValue) ? null : input.TextValue.Trim(),
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.ShelfAttributeValues.AddAsync(value);
                }
            }

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Rəf xüsusiyyətləri yadda saxlanıldı.");
        }

        private async Task<Result<bool>> ValidateShelfAttributesAsync(List<ShelfAttributeInput>? attributes)
        {
            if (attributes == null || !attributes.Any())
                return Result<bool>.Success(true);

            var duplicateAttributeIds = attributes
                .Where(x => x.ShelfAttributeDefinitionId > 0)
                .GroupBy(x => x.ShelfAttributeDefinitionId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateAttributeIds.Any())
                return Result<bool>.Fail("Eyni rəf xüsusiyyəti bir dəfə əlavə oluna bilər.");

            foreach (var input in attributes)
            {
                if (input.ShelfAttributeDefinitionId <= 0)
                    return Result<bool>.Fail("Rəf xüsusiyyəti düzgün seçilməyib.");

                var definition = await _context.ShelfAttributeDefinitions
                    .FirstOrDefaultAsync(x => x.Id == input.ShelfAttributeDefinitionId && x.IsActive);

                if (definition == null)
                    return Result<bool>.Fail("Rəf xüsusiyyəti tapılmadı.");

                if (definition.IsNumeric)
                {
                    if (!input.NumericValue.HasValue)
                        return Result<bool>.Fail($"\"{definition.Name}\" üçün rəqəmsal dəyər daxil edilməlidir.");

                    if (input.NumericValue.Value < 0)
                        return Result<bool>.Fail($"\"{definition.Name}\" mənfi ola bilməz.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(input.TextValue))
                        return Result<bool>.Fail($"\"{definition.Name}\" üçün mətn dəyəri daxil edilməlidir.");
                }
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            var shelf = await _context.Shelves
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (shelf == null)
                return Result<bool>.Fail("Rəf tapılmadı.");

            var hasStock = shelf.ShelfStocks.Any(x => x.Quantity > 0 && x.IsActive);

            if (hasStock)
                return Result<bool>.Fail("Bu rəfdə məhsul qalığı var. Əvvəl məhsulları başqa rəfə transfer edin.");

            var hasInvoiceItems = await _context.InvoiceItems
                .AnyAsync(x => x.ShelfId == id && x.IsActive);

            if (hasInvoiceItems)
                return Result<bool>.Fail("Bu rəf qaimələrdə istifadə olunub. Ona görə passiv edilə bilməz.");

            var hasMovements = await _context.StockMovements
                .AnyAsync(x =>
                    x.IsActive &&
                    (x.FromShelfId == id || x.ToShelfId == id));

            if (hasMovements)
                return Result<bool>.Fail("Bu rəf stok hərəkətlərində istifadə olunub. Ona görə passiv edilə bilməz.");

            foreach (var attributeValue in shelf.AttributeValues.Where(x => x.IsActive))
            {
                attributeValue.IsActive = false;
                attributeValue.UpdatedAt = DateTime.Now;
            }

            shelf.IsActive = false;
            shelf.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Rəf passiv edildi.");
        }

        public async Task<Result<bool>> RecalculateShelfAsync(int shelfId)
        {
            return await _stockService.UpdateShelfStatusAsync(shelfId);
        }

        public async Task<Result<bool>> RecalculateAllShelvesAsync()
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var shelfIds = await _context.Shelves
                    .Where(x => x.IsActive)
                    .Select(x => x.Id)
                    .ToListAsync();

                foreach (var shelfId in shelfIds)
                {
                    var result = await _stockService.UpdateShelfStatusAsync(shelfId, saveChanges: false);

                    if (!result.IsSuccess)
                    {
                        await transaction.RollbackAsync();
                        return Result<bool>.Fail(result.Message);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<bool>.Success(true, "Bütün rəflərin statusu yeniləndi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Rəflər yenilənmədi: {ex.Message}");
            }
        }

        public async Task<Result<Dictionary<string, List<Shelf>>>> GetShelvesGroupedByZoneAsync()
        {
            var shelves = await _context.Shelves
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                    .ThenInclude(x => x.ShelfAttributeDefinition)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.AttributeDefinition)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Zone)
                .ThenBy(x => x.RowNumber)
                .ToListAsync();

            var grouped = shelves
                .GroupBy(x => x.Zone)
                .ToDictionary(group => group.Key, group => group.ToList());

            return Result<Dictionary<string, List<Shelf>>>.Success(grouped);
        }
    }
}