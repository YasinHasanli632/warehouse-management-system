using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    public class ExpenseTypeService
    {
        private readonly AppDbContext _context;

        public ExpenseTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ExpenseType>>> GetAllAsync(
            InvoiceType? invoiceType = null,
            bool includePassive = false,
            bool? useForImport = null,
            bool? affectStockCost = null)
        {
            try
            {
                var query = _context.ExpenseTypes
                    .Include(x => x.FieldDefinitions.Where(f => f.IsActive))
                    .AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive);

                if (invoiceType == InvoiceType.StockIn)
                    query = query.Where(x => x.UseForStockIn);

                if (invoiceType == InvoiceType.StockOut)
                    query = query.Where(x => x.UseForStockOut);

                if (useForImport.HasValue)
                    query = query.Where(x => x.UseForImport == useForImport.Value);

                if (affectStockCost.HasValue)
                    query = query.Where(x => x.AffectStockCost == affectStockCost.Value);

                var data = await query
                    .OrderByDescending(x => x.IsSystem)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

                return Result<List<ExpenseType>>.Success(data, "Xərc növləri yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ExpenseType>>.Fail($"Xərc növləri yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<ExpenseType>> GetByIdAsync(int id)
        {
            if (id <= 0)
                return Result<ExpenseType>.Fail("Xərc növü düzgün seçilməyib.");

            var entity = await _context.ExpenseTypes
                .Include(x => x.FieldDefinitions.Where(f => f.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (entity == null)
                return Result<ExpenseType>.Fail("Xərc növü tapılmadı.");

            return Result<ExpenseType>.Success(entity, "Xərc növü yükləndi.");
        }

        public async Task<Result<ExpenseType>> CreateAsync(
            string name,
            ExpenseDirection defaultDirection = ExpenseDirection.Plus,
            bool useForStockIn = true,
            bool useForStockOut = true,
            bool affectStockCost = false,
            bool useForImport = false,
            bool isTaxRelated = false,
            bool includeZeroAmountInCost = false,
            CostAllocationMethod defaultAllocationMethod = CostAllocationMethod.ByAmount)
        {
            try
            {
                name = (name ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                    return Result<ExpenseType>.Fail("Xərc növünün adı boş ola bilməz.");

                var exists = await _context.ExpenseTypes
                    .AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower());

                if (exists)
                    return Result<ExpenseType>.Fail("Bu adda xərc növü artıq mövcuddur.");

                var entity = new ExpenseType
                {
                    Name = name,
                    DefaultDirection = defaultDirection,
                    DefaultAllocationMethod = defaultAllocationMethod,

                    IsSystem = false,

                    UseForStockIn = useForStockIn,
                    UseForStockOut = useForStockOut,
                    UseForImport = useForImport,

                    AffectStockCost = affectStockCost,
                    IsTaxRelated = isTaxRelated,
                    IncludeZeroAmountInCost = includeZeroAmountInCost,

                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.ExpenseTypes.AddAsync(entity);
                await _context.SaveChangesAsync();

                return Result<ExpenseType>.Success(entity, "Yeni xərc növü yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result<ExpenseType>.Fail($"Xərc növü yaradılmadı: {ex.Message}");
            }
        }

        public async Task<Result<ExpenseType>> UpdateAsync(
            int id,
            string name,
            ExpenseDirection defaultDirection,
            bool useForStockIn,
            bool useForStockOut,
            bool affectStockCost,
            bool useForImport,
            bool isTaxRelated,
            bool includeZeroAmountInCost,
            CostAllocationMethod defaultAllocationMethod)
        {
            try
            {
                if (id <= 0)
                    return Result<ExpenseType>.Fail("Xərc növü düzgün seçilməyib.");

                name = (name ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                    return Result<ExpenseType>.Fail("Xərc növünün adı boş ola bilməz.");

                var entity = await _context.ExpenseTypes
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (entity == null)
                    return Result<ExpenseType>.Fail("Xərc növü tapılmadı.");

                var exists = await _context.ExpenseTypes
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.IsActive &&
                        x.Name.ToLower() == name.ToLower());

                if (exists)
                    return Result<ExpenseType>.Fail("Bu adda başqa xərc növü artıq mövcuddur.");

                entity.Name = name;
                entity.DefaultDirection = defaultDirection;
                entity.DefaultAllocationMethod = defaultAllocationMethod;

                entity.UseForStockIn = useForStockIn;
                entity.UseForStockOut = useForStockOut;
                entity.UseForImport = useForImport;

                entity.AffectStockCost = affectStockCost;
                entity.IsTaxRelated = isTaxRelated;
                entity.IncludeZeroAmountInCost = includeZeroAmountInCost;

                entity.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<ExpenseType>.Success(entity, "Xərc növü yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<ExpenseType>.Fail($"Xərc növü yenilənmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            if (id <= 0)
                return Result<bool>.Fail("Xərc növü düzgün seçilməyib.");

            var entity = await _context.ExpenseTypes
                .Include(x => x.FieldDefinitions.Where(f => f.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (entity == null)
                return Result<bool>.Fail("Xərc növü tapılmadı.");

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.Now;

            foreach (var field in entity.FieldDefinitions.Where(x => x.IsActive))
            {
                field.IsActive = false;
                field.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Xərc növü passiv edildi.");
        }

        public async Task<Result<bool>> ActivateAsync(int id)
        {
            if (id <= 0)
                return Result<bool>.Fail("Xərc növü düzgün seçilməyib.");

            var entity = await _context.ExpenseTypes
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<bool>.Fail("Xərc növü tapılmadı.");

            entity.IsActive = true;
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Xərc növü aktiv edildi.");
        }

        public async Task<Result<List<ExpenseTypeFieldDefinition>>> GetFieldDefinitionsAsync(int expenseTypeId)
        {
            try
            {
                var data = await _context.ExpenseTypeFieldDefinitions
                    .Where(x => x.IsActive && x.ExpenseTypeId == expenseTypeId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Label)
                    .ToListAsync();

                return Result<List<ExpenseTypeFieldDefinition>>.Success(data, "Xərc detail sahələri yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ExpenseTypeFieldDefinition>>.Fail($"Xərc detail sahələri yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<ExpenseTypeFieldDefinition>> AddFieldDefinitionAsync(
            int expenseTypeId,
            string fieldKey,
            string label,
            FieldDataType fieldType = FieldDataType.Text,
            bool isRequired = false,
            int sortOrder = 1)
        {
            if (expenseTypeId <= 0)
                return Result<ExpenseTypeFieldDefinition>.Fail("Xərc növü düzgün seçilməyib.");

            fieldKey = (fieldKey ?? string.Empty).Trim();
            label = (label ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fieldKey))
                return Result<ExpenseTypeFieldDefinition>.Fail("Field key boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(label))
                return Result<ExpenseTypeFieldDefinition>.Fail("Field adı boş ola bilməz.");

            var expenseTypeExists = await _context.ExpenseTypes
                .AnyAsync(x => x.Id == expenseTypeId && x.IsActive);

            if (!expenseTypeExists)
                return Result<ExpenseTypeFieldDefinition>.Fail("Xərc növü tapılmadı.");

            var exists = await _context.ExpenseTypeFieldDefinitions
                .AnyAsync(x =>
                    x.IsActive &&
                    x.ExpenseTypeId == expenseTypeId &&
                    x.FieldKey.ToLower() == fieldKey.ToLower());

            if (exists)
                return Result<ExpenseTypeFieldDefinition>.Fail("Bu field key artıq mövcuddur.");

            var field = new ExpenseTypeFieldDefinition
            {
                ExpenseTypeId = expenseTypeId,
                FieldKey = fieldKey,
                Label = label,
                FieldType = fieldType.ToString(),
                IsRequired = isRequired,
                SortOrder = sortOrder,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.ExpenseTypeFieldDefinitions.AddAsync(field);
            await _context.SaveChangesAsync();

            return Result<ExpenseTypeFieldDefinition>.Success(field, "Xərc field-i yaradıldı.");
        }

        public async Task<Result<ExpenseTypeFieldDefinition>> UpdateFieldDefinitionAsync(
            int id,
            string fieldKey,
            string label,
            FieldDataType fieldType,
            bool isRequired,
            int sortOrder)
        {
            if (id <= 0)
                return Result<ExpenseTypeFieldDefinition>.Fail("Field düzgün seçilməyib.");

            fieldKey = (fieldKey ?? string.Empty).Trim();
            label = (label ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fieldKey))
                return Result<ExpenseTypeFieldDefinition>.Fail("Field key boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(label))
                return Result<ExpenseTypeFieldDefinition>.Fail("Field adı boş ola bilməz.");

            var field = await _context.ExpenseTypeFieldDefinitions
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (field == null)
                return Result<ExpenseTypeFieldDefinition>.Fail("Field tapılmadı.");

            var exists = await _context.ExpenseTypeFieldDefinitions
                .AnyAsync(x =>
                    x.Id != id &&
                    x.IsActive &&
                    x.ExpenseTypeId == field.ExpenseTypeId &&
                    x.FieldKey.ToLower() == fieldKey.ToLower());

            if (exists)
                return Result<ExpenseTypeFieldDefinition>.Fail("Bu field key artıq mövcuddur.");

            field.FieldKey = fieldKey;
            field.Label = label;
            field.FieldType = fieldType.ToString();
            field.IsRequired = isRequired;
            field.SortOrder = sortOrder;
            field.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<ExpenseTypeFieldDefinition>.Success(field, "Xərc field-i yeniləndi.");
        }

        public async Task<Result<bool>> RemoveFieldDefinitionAsync(int id)
        {
            if (id <= 0)
                return Result<bool>.Fail("Field düzgün seçilməyib.");

            var field = await _context.ExpenseTypeFieldDefinitions
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (field == null)
                return Result<bool>.Fail("Field tapılmadı.");

            field.IsActive = false;
            field.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Xərc field-i passiv edildi.");
        }

        public async Task<Result<bool>> EnsureDefaultExpenseTypesAsync()
        {
            var defaults = GetDefaultExpenseTypes();

            foreach (var model in defaults)
            {
                var exists = await _context.ExpenseTypes
                    .Include(x => x.FieldDefinitions)
                    .FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower());

                if (exists == null)
                {
                    await _context.ExpenseTypes.AddAsync(model);
                    continue;
                }

                exists.IsActive = true;
                exists.IsSystem = true;
                exists.DefaultDirection = model.DefaultDirection;
                exists.DefaultAllocationMethod = model.DefaultAllocationMethod;
                exists.UseForStockIn = model.UseForStockIn;
                exists.UseForStockOut = model.UseForStockOut;
                exists.UseForImport = model.UseForImport;
                exists.AffectStockCost = model.AffectStockCost;
                exists.IsTaxRelated = model.IsTaxRelated;
                exists.IncludeZeroAmountInCost = model.IncludeZeroAmountInCost;
                exists.UpdatedAt = DateTime.Now;

                foreach (var defaultField in model.FieldDefinitions)
                {
                    var fieldExists = exists.FieldDefinitions.Any(x =>
                        x.FieldKey.Equals(defaultField.FieldKey, StringComparison.OrdinalIgnoreCase));

                    if (fieldExists)
                        continue;

                    exists.FieldDefinitions.Add(defaultField);
                }
            }

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Default xərc növləri hazırlandı.");
        }

        private static List<ExpenseType> GetDefaultExpenseTypes()
        {
            return new List<ExpenseType>
            {
                new ExpenseType
                {
                    Name = "Daşıma",
                    DefaultDirection = ExpenseDirection.Plus,
                    DefaultAllocationMethod = CostAllocationMethod.ByQuantity,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = false,
                    UseForImport = true,
                    AffectStockCost = true,
                    IsTaxRelated = false,
                    IncludeZeroAmountInCost = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    FieldDefinitions = new List<ExpenseTypeFieldDefinition>
                    {
                        new ExpenseTypeFieldDefinition
                        {
                            FieldKey = "vehicleNo",
                            Label = "Maşın nömrəsi",
                            FieldType = FieldDataType.Text.ToString(),
                            SortOrder = 1,
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new ExpenseTypeFieldDefinition
                        {
                            FieldKey = "driverName",
                            Label = "Sürücü",
                            FieldType = FieldDataType.Text.ToString(),
                            SortOrder = 2,
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        }
                    }
                },
                new ExpenseType
                {
                    Name = "Fəhlə pulu",
                    DefaultDirection = ExpenseDirection.Plus,
                    DefaultAllocationMethod = CostAllocationMethod.ByQuantity,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = false,
                    UseForImport = false,
                    AffectStockCost = true,
                    IsTaxRelated = false,
                    IncludeZeroAmountInCost = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ExpenseType
                {
                    Name = "Endirim",
                    DefaultDirection = ExpenseDirection.Minus,
                    DefaultAllocationMethod = CostAllocationMethod.ByAmount,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = true,
                    UseForImport = false,
                    AffectStockCost = true,
                    IsTaxRelated = false,
                    IncludeZeroAmountInCost = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ExpenseType
                {
                    Name = "Broker xidməti",
                    DefaultDirection = ExpenseDirection.Plus,
                    DefaultAllocationMethod = CostAllocationMethod.ByAmount,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = false,
                    UseForImport = true,
                    AffectStockCost = true,
                    IsTaxRelated = false,
                    IncludeZeroAmountInCost = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ExpenseType
                {
                    Name = "Terminal xərci",
                    DefaultDirection = ExpenseDirection.Plus,
                    DefaultAllocationMethod = CostAllocationMethod.ByAmount,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = false,
                    UseForImport = true,
                    AffectStockCost = true,
                    IsTaxRelated = false,
                    IncludeZeroAmountInCost = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ExpenseType
                {
                    Name = "Sığorta",
                    DefaultDirection = ExpenseDirection.Plus,
                    DefaultAllocationMethod = CostAllocationMethod.ByAmount,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = false,
                    UseForImport = true,
                    AffectStockCost = true,
                    IsTaxRelated = false,
                    IncludeZeroAmountInCost = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                }
            };
        }
    }
}