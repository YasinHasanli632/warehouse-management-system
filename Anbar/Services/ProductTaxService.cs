using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Məhsula default vergi qaydası bağlayan servis.
    // Bu servis qaimə vergisi yaratmır.
    // Sadəcə ProductTax şablonunu idarə edir.
    // Invoice confirm zamanı TaxCalculationService bu ProductTax qaydalarını oxuyub InvoiceItemTax snapshot yaradır.
    public class ProductTaxService
    {
        private readonly AppDbContext _context;

        public ProductTaxService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ProductTax>>> GetByProductIdAsync(int productId, bool includePassive = false)
        {
            try
            {
                if (productId <= 0)
                    return Result<List<ProductTax>>.Fail("Məhsul düzgün seçilməyib.");

                var query = _context.ProductTaxes
                    .Include(x => x.Product)
                    .Include(x => x.Tax)
                    .Where(x => x.ProductId == productId)
                    .AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive);

                var data = await query
                    .OrderBy(x => x.TaxType)
                    .ThenBy(x => x.Tax.Name)
                    .ToListAsync();

                return Result<List<ProductTax>>.Success(data, "Məhsul vergi qaydaları yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ProductTax>>.Fail($"Məhsul vergi qaydaları yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<ProductTax>>> GetActiveForProductAsync(int productId, DateTime? operationDate = null)
        {
            try
            {
                if (productId <= 0)
                    return Result<List<ProductTax>>.Fail("Məhsul düzgün seçilməyib.");

                var date = operationDate?.Date ?? DateTime.Now.Date;

                var data = await _context.ProductTaxes
                    .Include(x => x.Tax)
                    .Where(x =>
                        x.ProductId == productId &&
                        x.IsActive &&
                        x.IsApplicable &&
                        x.Tax.IsActive &&
                        (!x.ValidFrom.HasValue || x.ValidFrom.Value.Date <= date) &&
                        (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date))
                    .OrderBy(x => x.TaxType)
                    .ThenBy(x => x.Tax.Name)
                    .ToListAsync();

                return Result<List<ProductTax>>.Success(data, "Aktiv məhsul vergi qaydaları yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ProductTax>>.Fail($"Aktiv məhsul vergi qaydaları yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<ProductTax>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<ProductTax>.Fail("Vergi qaydası düzgün seçilməyib.");

                var data = await _context.ProductTaxes
                    .Include(x => x.Product)
                    .Include(x => x.Tax)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (data == null)
                    return Result<ProductTax>.Fail("Məhsul vergi qaydası tapılmadı.");

                return Result<ProductTax>.Success(data, "Məhsul vergi qaydası yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<ProductTax>.Fail($"Məhsul vergi qaydası yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<ProductTax>> AddOrUpdateAsync(
            int productId,
            int taxId,
            decimal? ratePercent = null,
            bool? isApplicable = null,
            bool? isRecoverable = null,
            bool? isIncludedInCost = null,
            bool? isIncludedInPrice = null,
            TaxCostTreatment? costTreatment = null,
            DateTime? validFrom = null,
            DateTime? validTo = null,
            string? note = null)
        {
            try
            {
                if (productId <= 0)
                    return Result<ProductTax>.Fail("Məhsul düzgün seçilməyib.");

                if (taxId <= 0)
                    return Result<ProductTax>.Fail("Vergi düzgün seçilməyib.");

                if (ratePercent.HasValue && (ratePercent.Value < 0 || ratePercent.Value > 100))
                    return Result<ProductTax>.Fail("Vergi faizi 0-100 aralığında olmalıdır.");

                if (validFrom.HasValue && validTo.HasValue && validFrom.Value.Date > validTo.Value.Date)
                    return Result<ProductTax>.Fail("Başlama tarixi bitmə tarixindən böyük ola bilməz.");

                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive && x.Status == ProductStatus.Active);

                if (product == null)
                    return Result<ProductTax>.Fail("Aktiv məhsul tapılmadı.");

                var tax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.Id == taxId && x.IsActive);

                if (tax == null)
                    return Result<ProductTax>.Fail("Aktiv vergi tapılmadı.");

                var productTax = await _context.ProductTaxes
                    .FirstOrDefaultAsync(x => x.ProductId == productId && x.TaxId == taxId);

                if (productTax == null)
                {
                    productTax = new ProductTax
                    {
                        ProductId = productId,
                        TaxId = taxId,
                        CreatedAt = DateTime.Now
                    };

                    await _context.ProductTaxes.AddAsync(productTax);
                }

                productTax.TaxType = tax.TaxType;
                productTax.RatePercent = ratePercent ?? tax.DefaultRatePercent;

                productTax.IsApplicable = isApplicable ?? true;
                productTax.IsRecoverable = isRecoverable ?? tax.IsRecoverableByDefault;
                productTax.IsIncludedInCost = isIncludedInCost ?? tax.IsIncludedInCostByDefault;
                productTax.IsIncludedInPrice = isIncludedInPrice ?? tax.IsIncludedInPriceByDefault;

                productTax.CostTreatment = costTreatment ?? tax.DefaultCostTreatment;

                productTax.ValidFrom = validFrom;
                productTax.ValidTo = validTo;
                productTax.Note = NormalizeNullableText(note);

                productTax.IsActive = true;
                productTax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<ProductTax>.Success(productTax, "Məhsul vergi qaydası yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                return Result<ProductTax>.Fail($"Məhsul vergi qaydası yadda saxlanılmadı: {ex.Message}");
            }
        }

        public async Task<Result<ProductTax>> SetVatRuleAsync(
            int productId,
            decimal ratePercent,
            bool isApplicable,
            bool isRecoverable,
            bool isIncludedInPrice,
            bool isIncludedInCost,
            string? note = null)
        {
            try
            {
                var vatTax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.IsActive && x.TaxType == TaxType.VAT);

                if (vatTax == null)
                    return Result<ProductTax>.Fail("ƏDV vergisi tapılmadı. Əvvəl TaxService.EnsureDefaultTaxesAsync işlədilməlidir.");

                var treatment = ResolveCostTreatment(isRecoverable, isIncludedInCost);

                var result = await AddOrUpdateAsync(
                    productId: productId,
                    taxId: vatTax.Id,
                    ratePercent: ratePercent,
                    isApplicable: isApplicable,
                    isRecoverable: isRecoverable,
                    isIncludedInCost: isIncludedInCost,
                    isIncludedInPrice: isIncludedInPrice,
                    costTreatment: treatment,
                    validFrom: null,
                    validTo: null,
                    note: note ?? "Məhsul ƏDV qaydası.");

                if (!result.IsSuccess || result.Data == null)
                    return Result<ProductTax>.Fail(result.Message);

                await SyncProductOldVatFieldsAsync(
                    productId,
                    isApplicable,
                    ratePercent,
                    isIncludedInPrice,
                    isRecoverable);

                return Result<ProductTax>.Success(result.Data, "Məhsul ƏDV qaydası yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                return Result<ProductTax>.Fail($"Məhsul ƏDV qaydası yadda saxlanılmadı: {ex.Message}");
            }
        }

        public async Task<Result<ProductTax>> EnsureVatRuleFromProductDefaultsAsync(int productId)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);

                if (product == null)
                    return Result<ProductTax>.Fail("Məhsul tapılmadı.");

                var vatTax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.IsActive && x.TaxType == TaxType.VAT);

                if (vatTax == null)
                    return Result<ProductTax>.Fail("ƏDV vergisi tapılmadı. Əvvəl default vergiləri yaradın.");

                var isIncludedInCost = !product.IsVatRecoverable;
                var treatment = ResolveCostTreatment(product.IsVatRecoverable, isIncludedInCost);

                return await AddOrUpdateAsync(
                    productId: product.Id,
                    taxId: vatTax.Id,
                    ratePercent: product.VatRate,
                    isApplicable: product.IsVatApplicable,
                    isRecoverable: product.IsVatRecoverable,
                    isIncludedInCost: isIncludedInCost,
                    isIncludedInPrice: product.IsPurchasePriceVatIncluded,
                    costTreatment: treatment,
                    validFrom: null,
                    validTo: null,
                    note: "Köhnə məhsul ƏDV sahələrindən avtomatik yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result<ProductTax>.Fail($"ƏDV qaydası yaradılmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> EnsureVatRulesForAllProductsAsync()
        {
            try
            {
                var vatTax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.IsActive && x.TaxType == TaxType.VAT);

                if (vatTax == null)
                    return Result<bool>.Fail("ƏDV vergisi tapılmadı. Əvvəl TaxService.EnsureDefaultTaxesAsync işlədilməlidir.");

                var products = await _context.Products
                    .Where(x => x.IsActive)
                    .ToListAsync();

                foreach (var product in products)
                {
                    var exists = await _context.ProductTaxes
                        .AnyAsync(x => x.ProductId == product.Id && x.TaxId == vatTax.Id);

                    if (exists)
                        continue;

                    var isIncludedInCost = !product.IsVatRecoverable;
                    var treatment = ResolveCostTreatment(product.IsVatRecoverable, isIncludedInCost);

                    var productTax = new ProductTax
                    {
                        ProductId = product.Id,
                        TaxId = vatTax.Id,
                        TaxType = TaxType.VAT,
                        RatePercent = product.VatRate,
                        IsApplicable = product.IsVatApplicable,
                        IsRecoverable = product.IsVatRecoverable,
                        IsIncludedInCost = isIncludedInCost,
                        IsIncludedInPrice = product.IsPurchasePriceVatIncluded,
                        CostTreatment = treatment,
                        Note = "Köhnə məhsul ƏDV sahələrindən avtomatik yaradıldı.",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.ProductTaxes.AddAsync(productTax);
                }

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Bütün məhsullar üçün ƏDV qaydaları hazırlandı.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Məhsul ƏDV qaydaları hazırlanmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<bool>.Fail("Vergi qaydası düzgün seçilməyib.");

                var productTax = await _context.ProductTaxes
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (productTax == null)
                    return Result<bool>.Fail("Məhsul vergi qaydası tapılmadı.");

                productTax.IsActive = false;
                productTax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Məhsul vergi qaydası passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Məhsul vergi qaydası passiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ActivateAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<bool>.Fail("Vergi qaydası düzgün seçilməyib.");

                var productTax = await _context.ProductTaxes
                    .Include(x => x.Product)
                    .Include(x => x.Tax)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (productTax == null)
                    return Result<bool>.Fail("Məhsul vergi qaydası tapılmadı.");

                if (!productTax.Product.IsActive)
                    return Result<bool>.Fail("Məhsul passivdir. Qayda aktiv edilə bilməz.");

                if (!productTax.Tax.IsActive)
                    return Result<bool>.Fail("Vergi passivdir. Qayda aktiv edilə bilməz.");

                productTax.IsActive = true;
                productTax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Məhsul vergi qaydası aktiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Məhsul vergi qaydası aktiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> RemoveByProductAndTaxAsync(int productId, int taxId)
        {
            try
            {
                if (productId <= 0)
                    return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

                if (taxId <= 0)
                    return Result<bool>.Fail("Vergi düzgün seçilməyib.");

                var productTax = await _context.ProductTaxes
                    .FirstOrDefaultAsync(x => x.ProductId == productId && x.TaxId == taxId && x.IsActive);

                if (productTax == null)
                    return Result<bool>.Fail("Məhsul vergi qaydası tapılmadı.");

                productTax.IsActive = false;
                productTax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Məhsul vergi qaydası passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Məhsul vergi qaydası passiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CopyTaxesFromProductAsync(int sourceProductId, int targetProductId)
        {
            try
            {
                if (sourceProductId <= 0 || targetProductId <= 0)
                    return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

                if (sourceProductId == targetProductId)
                    return Result<bool>.Fail("Eyni məhsula kopyalama etmək olmaz.");

                var sourceTaxes = await _context.ProductTaxes
                    .Where(x => x.ProductId == sourceProductId && x.IsActive)
                    .ToListAsync();

                if (!sourceTaxes.Any())
                    return Result<bool>.Fail("Kopyalanacaq aktiv vergi qaydası tapılmadı.");

                var targetExists = await _context.Products
                    .AnyAsync(x => x.Id == targetProductId && x.IsActive);

                if (!targetExists)
                    return Result<bool>.Fail("Hədəf məhsul tapılmadı.");

                foreach (var source in sourceTaxes)
                {
                    var targetRule = await _context.ProductTaxes
                        .FirstOrDefaultAsync(x => x.ProductId == targetProductId && x.TaxId == source.TaxId);

                    if (targetRule == null)
                    {
                        targetRule = new ProductTax
                        {
                            ProductId = targetProductId,
                            TaxId = source.TaxId,
                            CreatedAt = DateTime.Now
                        };

                        await _context.ProductTaxes.AddAsync(targetRule);
                    }

                    targetRule.TaxType = source.TaxType;
                    targetRule.RatePercent = source.RatePercent;
                    targetRule.IsApplicable = source.IsApplicable;
                    targetRule.IsRecoverable = source.IsRecoverable;
                    targetRule.IsIncludedInCost = source.IsIncludedInCost;
                    targetRule.IsIncludedInPrice = source.IsIncludedInPrice;
                    targetRule.CostTreatment = source.CostTreatment;
                    targetRule.ValidFrom = source.ValidFrom;
                    targetRule.ValidTo = source.ValidTo;
                    targetRule.Note = "Başqa məhsuldan kopyalandı.";
                    targetRule.IsActive = true;
                    targetRule.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Vergi qaydaları məhsula kopyalandı.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Vergi qaydaları kopyalanmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> MarkProductAsImportTaxExemptAsync(int productId, bool isExempt)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);

                if (product == null)
                    return Result<bool>.Fail("Məhsul tapılmadı.");

                product.IsImportTaxExempt = isExempt;
                product.UpdatedAt = DateTime.Now;

                if (isExempt)
                {
                    var importRules = await _context.ProductTaxes
                        .Where(x =>
                            x.ProductId == productId &&
                            x.IsActive &&
                            (x.TaxType == TaxType.ImportVAT ||
                             x.TaxType == TaxType.CustomsDuty ||
                             x.TaxType == TaxType.Excise))
                        .ToListAsync();

                    foreach (var rule in importRules)
                    {
                        rule.IsApplicable = false;
                        rule.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Məhsul idxal vergi azadlığı yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Məhsul idxal vergi azadlığı yenilənmədi: {ex.Message}");
            }
        }

        private async Task SyncProductOldVatFieldsAsync(
            int productId,
            bool isVatApplicable,
            decimal vatRate,
            bool isPurchasePriceVatIncluded,
            bool isVatRecoverable)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
                return;

            product.IsVatApplicable = isVatApplicable;
            product.VatRate = vatRate;
            product.IsPurchasePriceVatIncluded = isPurchasePriceVatIncluded;
            product.IsVatRecoverable = isVatRecoverable;
            product.UpdatedAt = DateTime.Now;
        }

        private static TaxCostTreatment ResolveCostTreatment(bool isRecoverable, bool isIncludedInCost)
        {
            if (isRecoverable)
                return TaxCostTreatment.Recoverable;

            if (isIncludedInCost)
                return TaxCostTreatment.IncludedInCost;

            return TaxCostTreatment.ExcludedFromCost;
        }

        private static string? NormalizeNullableText(string? value)
        {
            value = value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}