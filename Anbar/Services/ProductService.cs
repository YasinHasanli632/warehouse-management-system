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
    public class ProductListFilterDto
    {
        public string? SearchText { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? UnitId { get; set; }
        public ProductStatus? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }

    public class ProductSaveDto
    {
        public int? Id { get; set; }

        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Barcode { get; set; }

        public string Unit { get; set; } = "ədəd";
        public int? UnitId { get; set; }

        public int? BrandId { get; set; }

        public decimal MinStockQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }

        public decimal LastCostPrice { get; set; }
        public decimal AverageCostPrice { get; set; }

        public bool IsVatApplicable { get; set; } = true;
        public decimal VatRate { get; set; } = 18;
        public bool IsPurchasePriceVatIncluded { get; set; } = true;
        public bool IsVatRecoverable { get; set; } = true;
        public bool IsExciseApplicable { get; set; } = false;
        public bool IsImportTaxExempt { get; set; } = false;

        public decimal Weight { get; set; }
        public decimal Volume { get; set; }

        public string? Description { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public int CategoryId { get; set; }

        public List<int> AttributeValueIds { get; set; } = new();
    }

    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<Product>>> GetListAsync(ProductListFilterDto? filter = null)
        {
            try
            {
                filter ??= new ProductListFilterDto();

                var query = _context.Products
                    .Include(x => x.Category)
                    .Include(x => x.Brand)
                    .Include(x => x.UnitEntity)
                    .Include(x => x.ProductTaxes.Where(t => t.IsActive))
                        .ThenInclude(x => x.Tax)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var search = filter.SearchText.Trim().ToLower();

                    query = query.Where(x =>
                        x.Name.ToLower().Contains(search) ||
                        x.Code.ToLower().Contains(search) ||
                        (x.Barcode != null && x.Barcode.ToLower().Contains(search)) ||
                        (x.Brand != null && x.Brand.Name.ToLower().Contains(search)));
                }

                if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
                    query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

                if (filter.BrandId.HasValue && filter.BrandId.Value > 0)
                    query = query.Where(x => x.BrandId == filter.BrandId.Value);

                if (filter.UnitId.HasValue && filter.UnitId.Value > 0)
                    query = query.Where(x => x.UnitId == filter.UnitId.Value);

                if (filter.Status.HasValue)
                    query = query.Where(x => x.Status == filter.Status.Value);

                if (filter.CreatedFrom.HasValue)
                {
                    var from = filter.CreatedFrom.Value.Date;
                    query = query.Where(x => x.CreatedAt.Date >= from);
                }

                if (filter.CreatedTo.HasValue)
                {
                    var to = filter.CreatedTo.Value.Date;
                    query = query.Where(x => x.CreatedAt.Date <= to);
                }

                var products = await query
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                return Result<List<Product>>.Success(products, "Məhsul siyahısı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<Product>>.Fail($"Məhsullar yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<Product>>> GetAllAsync()
        {
            return await GetListAsync(new ProductListFilterDto
            {
                Status = null
            });
        }

        public async Task<Result<Product>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<Product>.Fail("Məhsul düzgün seçilməyib.");

                var product = await _context.Products
                    .Include(x => x.Category)
                    .Include(x => x.Brand)
                    .Include(x => x.UnitEntity)
                    .Include(x => x.ProductTaxes.Where(t => t.IsActive))
                        .ThenInclude(x => x.Tax)
                    .Include(x => x.Attributes.Where(a => a.IsActive))
                        .ThenInclude(x => x.AttributeValue)
                            .ThenInclude(x => x.AttributeDefinition)
                    .Include(x => x.StockBatches.Where(b => b.IsActive && b.RemainingQuantity > 0))
                        .ThenInclude(x => x.Shelf)
                    .Include(x => x.ShelfStocks.Where(s => s.IsActive && s.Quantity > 0))
                        .ThenInclude(x => x.Shelf)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (product == null)
                    return Result<Product>.Fail("Məhsul tapılmadı.");

                return Result<Product>.Success(product, "Məhsul məlumatları yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<Product>.Fail($"Məhsul yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<Product>>> GetByCategoryAsync(int categoryId)
        {
            if (categoryId <= 0)
                return Result<List<Product>>.Fail("Kateqoriya düzgün seçilməyib.");

            var categoryExists = await _context.Categories
                .AnyAsync(x => x.Id == categoryId && x.IsActive);

            if (!categoryExists)
                return Result<List<Product>>.Fail("Kateqoriya tapılmadı.");

            return await GetListAsync(new ProductListFilterDto
            {
                CategoryId = categoryId
            });
        }

        public async Task<Result<List<Product>>> GetByBrandAsync(int brandId)
        {
            if (brandId <= 0)
                return Result<List<Product>>.Fail("Marka düzgün seçilməyib.");

            var brandExists = await _context.Brands
                .AnyAsync(x => x.Id == brandId && x.IsActive);

            if (!brandExists)
                return Result<List<Product>>.Fail("Marka tapılmadı.");

            return await GetListAsync(new ProductListFilterDto
            {
                BrandId = brandId
            });
        }

        public async Task<Result<Product>> CreateAsync(ProductSaveDto dto)
        {
            try
            {
                var validation = await ValidateProductAsync(dto);
                if (!validation.IsSuccess)
                    return Result<Product>.Fail(validation.Message);

                var unitResult = await ResolveUnitAsync(dto);
                if (!unitResult.IsSuccess || unitResult.Data == null)
                    return Result<Product>.Fail(unitResult.Message);

                var selectedUnit = unitResult.Data;

                var product = new Product
                {
                    Name = dto.Name.Trim(),
                    Code = dto.Code.Trim(),
                    Barcode = NormalizeNullableText(dto.Barcode),

                    Unit = selectedUnit.Symbol,
                    UnitId = selectedUnit.Id,

                    BrandId = dto.BrandId,

                    MinStockQuantity = dto.MinStockQuantity,
                    PurchasePrice = dto.PurchasePrice,
                    SalePrice = dto.SalePrice,

                    LastCostPrice = 0,
                    AverageCostPrice = 0,

                    IsVatApplicable = dto.IsVatApplicable,
                    VatRate = dto.IsVatApplicable ? dto.VatRate : 0,
                    IsPurchasePriceVatIncluded = dto.IsPurchasePriceVatIncluded,
                    IsVatRecoverable = dto.IsVatRecoverable,
                    IsExciseApplicable = dto.IsExciseApplicable,
                    IsImportTaxExempt = dto.IsImportTaxExempt,

                    Weight = dto.Weight,
                    Volume = dto.Volume,

                    Description = NormalizeNullableText(dto.Description),
                    Status = dto.Status,
                    CategoryId = dto.CategoryId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();

                await SyncProductAttributesAsync(product.Id, dto.AttributeValueIds);
                await SyncDefaultTaxesAsync(product.Id);

                var createdProduct = await GetByIdAsync(product.Id);
                if (createdProduct.IsSuccess && createdProduct.Data != null)
                    return Result<Product>.Success(createdProduct.Data, "Məhsul uğurla yaradıldı.");

                return Result<Product>.Success(product, "Məhsul uğurla yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result<Product>.Fail($"Məhsul yaradılmadı: {ex.Message}");
            }
        }

        public async Task<Result<Product>> UpdateAsync(ProductSaveDto dto)
        {
            try
            {
                if (!dto.Id.HasValue || dto.Id.Value <= 0)
                    return Result<Product>.Fail("Məhsul ID boş ola bilməz.");

                var product = await _context.Products
                    .Include(x => x.Attributes)
                    .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.IsActive);

                if (product == null)
                    return Result<Product>.Fail("Məhsul tapılmadı.");

                var validation = await ValidateProductAsync(dto, dto.Id.Value);
                if (!validation.IsSuccess)
                    return Result<Product>.Fail(validation.Message);

                var unitResult = await ResolveUnitAsync(dto);
                if (!unitResult.IsSuccess || unitResult.Data == null)
                    return Result<Product>.Fail(unitResult.Message);

                var selectedUnit = unitResult.Data;

                product.Name = dto.Name.Trim();
                product.Code = dto.Code.Trim();
                product.Barcode = NormalizeNullableText(dto.Barcode);

                product.Unit = selectedUnit.Symbol;
                product.UnitId = selectedUnit.Id;

                product.BrandId = dto.BrandId;

                product.MinStockQuantity = dto.MinStockQuantity;
                product.PurchasePrice = dto.PurchasePrice;
                product.SalePrice = dto.SalePrice;

                product.IsVatApplicable = dto.IsVatApplicable;
                product.VatRate = dto.IsVatApplicable ? dto.VatRate : 0;
                product.IsPurchasePriceVatIncluded = dto.IsPurchasePriceVatIncluded;
                product.IsVatRecoverable = dto.IsVatRecoverable;
                product.IsExciseApplicable = dto.IsExciseApplicable;
                product.IsImportTaxExempt = dto.IsImportTaxExempt;

                product.Weight = dto.Weight;
                product.Volume = dto.Volume;

                product.Description = NormalizeNullableText(dto.Description);
                product.Status = dto.Status;
                product.CategoryId = dto.CategoryId;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await SyncProductAttributesAsync(product.Id, dto.AttributeValueIds);
                await SyncDefaultTaxesAsync(product.Id);

                var updatedProduct = await GetByIdAsync(product.Id);
                if (updatedProduct.IsSuccess && updatedProduct.Data != null)
                    return Result<Product>.Success(updatedProduct.Data, "Məhsul uğurla yeniləndi.");

                return Result<Product>.Success(product, "Məhsul uğurla yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<Product>.Fail($"Məhsul yenilənmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (product == null)
                    return Result<bool>.Fail("Məhsul tapılmadı.");

                var hasStock = await _context.ShelfStocks
                    .AnyAsync(x => x.ProductId == id && x.IsActive && x.Quantity > 0);

                if (hasStock)
                    return Result<bool>.Fail("Bu məhsulun anbarda qalığı var. Qalıq sıfırlanmadan məhsulu passiv etmək olmaz.");

                product.IsActive = false;
                product.Status = ProductStatus.Passive;
                product.UpdatedAt = DateTime.Now;

                var productTaxes = await _context.ProductTaxes
                    .Where(x => x.ProductId == id && x.IsActive)
                    .ToListAsync();

                foreach (var tax in productTaxes)
                {
                    tax.IsActive = false;
                    tax.UpdatedAt = DateTime.Now;
                }

                var productAttributes = await _context.ProductAttributes
                    .Where(x => x.ProductId == id && x.IsActive)
                    .ToListAsync();

                foreach (var attr in productAttributes)
                {
                    attr.IsActive = false;
                    attr.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Məhsul passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Məhsul passiv edilmədi: {ex.Message}");
            }
        }

        private async Task<Result<bool>> ValidateProductAsync(ProductSaveDto dto, int? currentProductId = null)
        {
            if (dto == null)
                return Result<bool>.Fail("Məhsul məlumatları boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<bool>.Fail("Məhsul adı boş ola bilməz.");

            if (dto.Name.Trim().Length < 2)
                return Result<bool>.Fail("Məhsul adı ən azı 2 simvol olmalıdır.");

            if (dto.Name.Trim().Length > 200)
                return Result<bool>.Fail("Məhsul adı 200 simvoldan uzun ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.Code))
                return Result<bool>.Fail("Məhsul kodu boş ola bilməz.");

            if (dto.Code.Trim().Length > 80)
                return Result<bool>.Fail("Məhsul kodu 80 simvoldan uzun ola bilməz.");

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && dto.Barcode.Trim().Length > 100)
                return Result<bool>.Fail("Barkod 100 simvoldan uzun ola bilməz.");

            if (dto.CategoryId <= 0)
                return Result<bool>.Fail("Kateqoriya seçilməlidir.");

            if (dto.MinStockQuantity < 0)
                return Result<bool>.Fail("Minimum stok mənfi ola bilməz.");

            if (dto.PurchasePrice < 0)
                return Result<bool>.Fail("Alış qiyməti mənfi ola bilməz.");

            if (dto.SalePrice < 0)
                return Result<bool>.Fail("Satış qiyməti mənfi ola bilməz.");

            if (dto.IsVatApplicable && (dto.VatRate < 0 || dto.VatRate > 100))
                return Result<bool>.Fail("ƏDV faizi 0-100 arası olmalıdır.");

            if (!dto.IsVatApplicable && dto.VatRate != 0)
                dto.VatRate = 0;

            if (dto.Weight < 0)
                return Result<bool>.Fail("Çəki mənfi ola bilməz.");

            if (dto.Volume < 0)
                return Result<bool>.Fail("Həcm mənfi ola bilməz.");

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > 500)
                return Result<bool>.Fail("Təsvir 500 simvoldan uzun ola bilməz.");

            var categoryExists = await _context.Categories
                .AnyAsync(x => x.Id == dto.CategoryId && x.IsActive);

            if (!categoryExists)
                return Result<bool>.Fail("Kateqoriya tapılmadı.");

            if (dto.BrandId.HasValue && dto.BrandId.Value > 0)
            {
                var brandExists = await _context.Brands
                    .AnyAsync(x => x.Id == dto.BrandId.Value && x.IsActive);

                if (!brandExists)
                    return Result<bool>.Fail("Seçilmiş marka tapılmadı.");
            }

            if (dto.UnitId.HasValue)
            {
                var unitExists = await _context.Units
                    .AnyAsync(x => x.Id == dto.UnitId.Value && x.IsActive);

                if (!unitExists)
                    return Result<bool>.Fail("Seçilmiş ölçü vahidi tapılmadı.");
            }

            var normalizedCode = dto.Code.Trim().ToLower();

            var codeExists = await _context.Products.AnyAsync(x =>
                x.IsActive &&
                x.Code.ToLower() == normalizedCode &&
                (!currentProductId.HasValue || x.Id != currentProductId.Value));

            if (codeExists)
                return Result<bool>.Fail("Bu məhsul kodu artıq mövcuddur.");

            if (!string.IsNullOrWhiteSpace(dto.Barcode))
            {
                var normalizedBarcode = dto.Barcode.Trim().ToLower();

                var barcodeExists = await _context.Products.AnyAsync(x =>
                    x.IsActive &&
                    x.Barcode != null &&
                    x.Barcode.ToLower() == normalizedBarcode &&
                    (!currentProductId.HasValue || x.Id != currentProductId.Value));

                if (barcodeExists)
                    return Result<bool>.Fail("Bu barkod artıq başqa məhsulda istifadə olunur.");
            }

            if (dto.AttributeValueIds != null && dto.AttributeValueIds.Any())
            {
                var distinctIds = dto.AttributeValueIds.Distinct().ToList();

                if (distinctIds.Count != dto.AttributeValueIds.Count)
                    return Result<bool>.Fail("Eyni xüsusiyyət dəyəri bir neçə dəfə seçilə bilməz.");

                var validAttributeCount = await _context.AttributeValues
                    .Include(x => x.AttributeDefinition)
                    .CountAsync(x =>
                        distinctIds.Contains(x.Id) &&
                        x.IsActive &&
                        x.AttributeDefinition.IsActive &&
                        x.AttributeDefinition.CategoryId == dto.CategoryId);

                if (validAttributeCount != distinctIds.Count)
                    return Result<bool>.Fail("Seçilmiş xüsusiyyətlər bu kateqoriyaya aid deyil.");
            }

            return Result<bool>.Success(true);
        }

        private async Task<Result<Unit>> ResolveUnitAsync(ProductSaveDto dto)
        {
            if (dto.UnitId.HasValue && dto.UnitId.Value > 0)
            {
                var selectedUnit = await _context.Units
                    .FirstOrDefaultAsync(x => x.Id == dto.UnitId.Value && x.IsActive);

                if (selectedUnit == null)
                    return Result<Unit>.Fail("Seçilmiş ölçü vahidi tapılmadı.");

                return Result<Unit>.Success(selectedUnit);
            }

            var category = await _context.Categories
                .Include(x => x.DefaultUnit)
                .FirstOrDefaultAsync(x => x.Id == dto.CategoryId && x.IsActive);

            if (category == null)
                return Result<Unit>.Fail("Kateqoriya tapılmadı.");

            if (category.DefaultUnit != null && category.DefaultUnit.IsActive)
                return Result<Unit>.Success(category.DefaultUnit);

            var defaultUnit = await _context.Units
                .FirstOrDefaultAsync(x => x.IsActive && x.IsDefault);

            if (defaultUnit != null)
                return Result<Unit>.Success(defaultUnit);

            var firstUnit = await _context.Units
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .FirstOrDefaultAsync();

            if (firstUnit == null)
                return Result<Unit>.Fail("Ölçü vahidi tapılmadı. Əvvəlcə Unit cədvəlində ən azı bir vahid yaradın.");

            return Result<Unit>.Success(firstUnit);
        }

        private async Task SyncProductAttributesAsync(int productId, List<int>? attributeValueIds)
        {
            attributeValueIds ??= new List<int>();

            var distinctIds = attributeValueIds
                .Distinct()
                .ToList();

            var existingAttributes = await _context.ProductAttributes
                .Where(x => x.ProductId == productId)
                .ToListAsync();

            foreach (var existing in existingAttributes)
            {
                if (distinctIds.Contains(existing.AttributeValueId))
                {
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.Now;
                }
                else
                {
                    existing.IsActive = false;
                    existing.UpdatedAt = DateTime.Now;
                }
            }

            var existingValueIds = existingAttributes
                .Select(x => x.AttributeValueId)
                .ToList();

            var newValueIds = distinctIds
                .Where(id => !existingValueIds.Contains(id))
                .ToList();

            foreach (var valueId in newValueIds)
            {
                var productAttribute = new ProductAttribute
                {
                    ProductId = productId,
                    AttributeValueId = valueId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.ProductAttributes.AddAsync(productAttribute);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SyncDefaultTaxesAsync(int productId)
        {
            var taxService = new TaxService(_context);
            await taxService.EnsureDefaultTaxesAsync();

            var productTaxService = new ProductTaxService(_context);
            await productTaxService.EnsureVatRuleFromProductDefaultsAsync(productId);
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}