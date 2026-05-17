using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Vergi kataloqunu idarə edən servis.
    // Bu servis qaiməyə vergi yazmır.
    // Sadəcə sistemdə ƏDV, İdxal ƏDV, Gömrük rüsumu, Aksiz və Digər vergi növlərinin kataloqunu saxlayır.
    public class TaxService
    {
        private readonly AppDbContext _context;

        public TaxService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<Tax>>> GetAllAsync(
            bool includePassive = false,
            TaxType? taxType = null,
            bool? useForLocalPurchase = null,
            bool? useForImportPurchase = null,
            bool? useForSale = null)
        {
            try
            {
                var query = _context.Taxes.AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive);

                if (taxType.HasValue)
                    query = query.Where(x => x.TaxType == taxType.Value);

                if (useForLocalPurchase.HasValue)
                    query = query.Where(x => x.UseForLocalPurchase == useForLocalPurchase.Value);

                if (useForImportPurchase.HasValue)
                    query = query.Where(x => x.UseForImportPurchase == useForImportPurchase.Value);

                if (useForSale.HasValue)
                    query = query.Where(x => x.UseForSale == useForSale.Value);

                var data = await query
                    .OrderBy(x => x.TaxType)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

                return Result<List<Tax>>.Success(data, "Vergilər yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<Tax>>.Fail($"Vergilər yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<Tax>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<Tax>.Fail("Vergi düzgün seçilməyib.");

                var tax = await _context.Taxes
                    .Include(x => x.ProductTaxes.Where(p => p.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (tax == null)
                    return Result<Tax>.Fail("Vergi tapılmadı.");

                return Result<Tax>.Success(tax, "Vergi yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<Tax>.Fail($"Vergi yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<Tax>> GetByCodeAsync(string code, bool includePassive = false)
        {
            try
            {
                code = NormalizeCode(code);

                if (string.IsNullOrWhiteSpace(code))
                    return Result<Tax>.Fail("Vergi kodu boş ola bilməz.");

                var query = _context.Taxes.AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive);

                var tax = await query.FirstOrDefaultAsync(x => x.Code.ToLower() == code.ToLower());

                if (tax == null)
                    return Result<Tax>.Fail("Vergi tapılmadı.");

                return Result<Tax>.Success(tax, "Vergi yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<Tax>.Fail($"Vergi yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<Tax>> CreateAsync(
            string name,
            string code,
            TaxType taxType,
            decimal defaultRatePercent = 0,
            bool isRecoverableByDefault = false,
            bool isIncludedInCostByDefault = false,
            bool isIncludedInPriceByDefault = false,
            TaxCalculationSource defaultCalculationSource = TaxCalculationSource.Product,
            TaxCostTreatment defaultCostTreatment = TaxCostTreatment.ExcludedFromCost,
            bool useForLocalPurchase = true,
            bool useForImportPurchase = true,
            bool useForSale = true,
            string? note = null)
        {
            try
            {
                name = NormalizeName(name);
                code = NormalizeCode(code);

                var validation = ValidateTaxBase(name, code, defaultRatePercent);
                if (!validation.IsSuccess)
                    return Result<Tax>.Fail(validation.Message);

                var codeExists = await _context.Taxes
                    .AnyAsync(x => x.Code.ToLower() == code.ToLower());

                if (codeExists)
                    return Result<Tax>.Fail("Bu kodla vergi artıq mövcuddur. Kod unikal olmalıdır.");

                var nameExists = await _context.Taxes
                    .AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower());

                if (nameExists)
                    return Result<Tax>.Fail("Bu adda aktiv vergi artıq mövcuddur.");

                var tax = new Tax
                {
                    Name = name,
                    Code = code,
                    TaxType = taxType,
                    DefaultRatePercent = defaultRatePercent,

                    IsRecoverableByDefault = isRecoverableByDefault,
                    IsIncludedInCostByDefault = isIncludedInCostByDefault,
                    IsIncludedInPriceByDefault = isIncludedInPriceByDefault,

                    DefaultCalculationSource = defaultCalculationSource,
                    DefaultCostTreatment = defaultCostTreatment,

                    UseForLocalPurchase = useForLocalPurchase,
                    UseForImportPurchase = useForImportPurchase,
                    UseForSale = useForSale,

                    Note = NormalizeNullableText(note),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.Taxes.AddAsync(tax);
                await _context.SaveChangesAsync();

                return Result<Tax>.Success(tax, "Vergi yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result<Tax>.Fail($"Vergi yaradılmadı: {ex.Message}");
            }
        }

        public async Task<Result<Tax>> UpdateAsync(
            int id,
            string name,
            string code,
            TaxType taxType,
            decimal defaultRatePercent,
            bool isRecoverableByDefault,
            bool isIncludedInCostByDefault,
            bool isIncludedInPriceByDefault,
            TaxCalculationSource defaultCalculationSource,
            TaxCostTreatment defaultCostTreatment,
            bool useForLocalPurchase,
            bool useForImportPurchase,
            bool useForSale,
            string? note = null)
        {
            try
            {
                if (id <= 0)
                    return Result<Tax>.Fail("Vergi düzgün seçilməyib.");

                name = NormalizeName(name);
                code = NormalizeCode(code);

                var validation = ValidateTaxBase(name, code, defaultRatePercent);
                if (!validation.IsSuccess)
                    return Result<Tax>.Fail(validation.Message);

                var tax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (tax == null)
                    return Result<Tax>.Fail("Vergi tapılmadı.");

                var codeExists = await _context.Taxes
                    .AnyAsync(x => x.Id != id && x.Code.ToLower() == code.ToLower());

                if (codeExists)
                    return Result<Tax>.Fail("Bu kodla başqa vergi artıq mövcuddur.");

                var nameExists = await _context.Taxes
                    .AnyAsync(x => x.Id != id && x.IsActive && x.Name.ToLower() == name.ToLower());

                if (nameExists)
                    return Result<Tax>.Fail("Bu adda başqa aktiv vergi artıq mövcuddur.");

                tax.Name = name;
                tax.Code = code;
                tax.TaxType = taxType;
                tax.DefaultRatePercent = defaultRatePercent;

                tax.IsRecoverableByDefault = isRecoverableByDefault;
                tax.IsIncludedInCostByDefault = isIncludedInCostByDefault;
                tax.IsIncludedInPriceByDefault = isIncludedInPriceByDefault;

                tax.DefaultCalculationSource = defaultCalculationSource;
                tax.DefaultCostTreatment = defaultCostTreatment;

                tax.UseForLocalPurchase = useForLocalPurchase;
                tax.UseForImportPurchase = useForImportPurchase;
                tax.UseForSale = useForSale;

                tax.Note = NormalizeNullableText(note);
                tax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<Tax>.Success(tax, "Vergi yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<Tax>.Fail($"Vergi yenilənmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<bool>.Fail("Vergi düzgün seçilməyib.");

                var tax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (tax == null)
                    return Result<bool>.Fail("Vergi tapılmadı.");

                var hasActiveProductRules = await _context.ProductTaxes
                    .AnyAsync(x => x.TaxId == id && x.IsActive);

                if (hasActiveProductRules)
                    return Result<bool>.Fail("Bu vergi aktiv məhsul vergi qaydalarında istifadə olunur. Əvvəl ProductTax qaydalarını passiv edin.");

                tax.IsActive = false;
                tax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Vergi passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Vergi passiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ActivateAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<bool>.Fail("Vergi düzgün seçilməyib.");

                var tax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (tax == null)
                    return Result<bool>.Fail("Vergi tapılmadı.");

                if (tax.IsActive)
                    return Result<bool>.Success(true, "Vergi artıq aktivdir.");

                tax.IsActive = true;
                tax.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Vergi aktiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Vergi aktiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<Tax>>> GetLocalPurchaseTaxesAsync()
        {
            return await GetAllAsync(
                includePassive: false,
                useForLocalPurchase: true);
        }

        public async Task<Result<List<Tax>>> GetImportPurchaseTaxesAsync()
        {
            return await GetAllAsync(
                includePassive: false,
                useForImportPurchase: true);
        }

        public async Task<Result<List<Tax>>> GetSaleTaxesAsync()
        {
            return await GetAllAsync(
                includePassive: false,
                useForSale: true);
        }

        public async Task<Result<Tax>> ResolveDefaultTaxAsync(TaxType taxType)
        {
            try
            {
                var tax = await _context.Taxes
                    .Where(x => x.IsActive && x.TaxType == taxType)
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();

                if (tax == null)
                    return Result<Tax>.Fail("Bu tip üçün aktiv vergi tapılmadı.");

                return Result<Tax>.Success(tax, "Default vergi tapıldı.");
            }
            catch (Exception ex)
            {
                return Result<Tax>.Fail($"Default vergi tapılmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> EnsureDefaultTaxesAsync()
        {
            try
            {
                await EnsureTaxAsync(
                    name: "ƏDV",
                    code: "VAT",
                    taxType: TaxType.VAT,
                    defaultRatePercent: 18,
                    isRecoverableByDefault: true,
                    isIncludedInCostByDefault: false,
                    isIncludedInPriceByDefault: true,
                    defaultCalculationSource: TaxCalculationSource.Product,
                    defaultCostTreatment: TaxCostTreatment.Recoverable,
                    useForLocalPurchase: true,
                    useForImportPurchase: true,
                    useForSale: true,
                    note: "Sistem default ƏDV vergisi.");

                await EnsureTaxAsync(
                    name: "İdxal ƏDV",
                    code: "IMPORT_VAT",
                    taxType: TaxType.ImportVAT,
                    defaultRatePercent: 18,
                    isRecoverableByDefault: true,
                    isIncludedInCostByDefault: false,
                    isIncludedInPriceByDefault: false,
                    defaultCalculationSource: TaxCalculationSource.Import,
                    defaultCostTreatment: TaxCostTreatment.Recoverable,
                    useForLocalPurchase: false,
                    useForImportPurchase: true,
                    useForSale: false,
                    note: "İdxal zamanı hesablanan ƏDV.");

                await EnsureTaxAsync(
                    name: "Gömrük rüsumu",
                    code: "CUSTOMS_DUTY",
                    taxType: TaxType.CustomsDuty,
                    defaultRatePercent: 0,
                    isRecoverableByDefault: false,
                    isIncludedInCostByDefault: true,
                    isIncludedInPriceByDefault: false,
                    defaultCalculationSource: TaxCalculationSource.Import,
                    defaultCostTreatment: TaxCostTreatment.IncludedInCost,
                    useForLocalPurchase: false,
                    useForImportPurchase: true,
                    useForSale: false,
                    note: "Gömrük rüsumu maya dəyərinə daxil edilir.");

                await EnsureTaxAsync(
                    name: "Aksiz",
                    code: "EXCISE",
                    taxType: TaxType.Excise,
                    defaultRatePercent: 0,
                    isRecoverableByDefault: false,
                    isIncludedInCostByDefault: true,
                    isIncludedInPriceByDefault: false,
                    defaultCalculationSource: TaxCalculationSource.Import,
                    defaultCostTreatment: TaxCostTreatment.IncludedInCost,
                    useForLocalPurchase: true,
                    useForImportPurchase: true,
                    useForSale: false,
                    note: "Aksiz vergisi.");

                await EnsureTaxAsync(
                    name: "Digər vergi",
                    code: "OTHER_TAX",
                    taxType: TaxType.Other,
                    defaultRatePercent: 0,
                    isRecoverableByDefault: false,
                    isIncludedInCostByDefault: true,
                    isIncludedInPriceByDefault: false,
                    defaultCalculationSource: TaxCalculationSource.Manual,
                    defaultCostTreatment: TaxCostTreatment.IncludedInCost,
                    useForLocalPurchase: true,
                    useForImportPurchase: true,
                    useForSale: true,
                    note: "Manual əlavə olunan digər vergi və rüsumlar.");

                return Result<bool>.Success(true, "Default vergi kataloqu hazırlandı.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Default vergilər yaradılmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SyncFromTaxSettingsAsync()
        {
            try
            {
                var setting = await _context.TaxSettings
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (setting == null)
                    return Result<bool>.Fail("TaxSetting tapılmadı.");

                var vat = await _context.Taxes.FirstOrDefaultAsync(x => x.Code == "VAT");
                if (vat != null)
                {
                    vat.DefaultRatePercent = setting.VATPercent;
                    vat.IsRecoverableByDefault = setting.VATRecoverableByDefault;
                    vat.IsIncludedInPriceByDefault = setting.PurchasePricesIncludeVATByDefault;
                    vat.IsIncludedInCostByDefault = !setting.VATRecoverableByDefault;
                    vat.DefaultCostTreatment = setting.VATRecoverableByDefault
                        ? TaxCostTreatment.Recoverable
                        : TaxCostTreatment.IncludedInCost;
                    vat.UpdatedAt = DateTime.Now;
                }

                var importVat = await _context.Taxes.FirstOrDefaultAsync(x => x.Code == "IMPORT_VAT");
                if (importVat != null)
                {
                    importVat.DefaultRatePercent = setting.VATPercent;
                    importVat.IsIncludedInCostByDefault = setting.IncludeImportVATInCost;
                    importVat.DefaultCostTreatment = setting.IncludeImportVATInCost
                        ? TaxCostTreatment.IncludedInCost
                        : TaxCostTreatment.Recoverable;
                    importVat.UpdatedAt = DateTime.Now;
                }

                var customs = await _context.Taxes.FirstOrDefaultAsync(x => x.Code == "CUSTOMS_DUTY");
                if (customs != null)
                {
                    customs.IsIncludedInCostByDefault = setting.IncludeCustomsDutyInCost;
                    customs.DefaultCostTreatment = setting.IncludeCustomsDutyInCost
                        ? TaxCostTreatment.IncludedInCost
                        : TaxCostTreatment.ExcludedFromCost;
                    customs.UpdatedAt = DateTime.Now;
                }

                var excise = await _context.Taxes.FirstOrDefaultAsync(x => x.Code == "EXCISE");
                if (excise != null)
                {
                    excise.IsIncludedInCostByDefault = setting.IncludeExciseInCost;
                    excise.DefaultCostTreatment = setting.IncludeExciseInCost
                        ? TaxCostTreatment.IncludedInCost
                        : TaxCostTreatment.ExcludedFromCost;
                    excise.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Vergi kataloqu TaxSetting əsasında yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Vergi ayarları sinxron edilmədi: {ex.Message}");
            }
        }

        private async Task EnsureTaxAsync(
            string name,
            string code,
            TaxType taxType,
            decimal defaultRatePercent,
            bool isRecoverableByDefault,
            bool isIncludedInCostByDefault,
            bool isIncludedInPriceByDefault,
            TaxCalculationSource defaultCalculationSource,
            TaxCostTreatment defaultCostTreatment,
            bool useForLocalPurchase,
            bool useForImportPurchase,
            bool useForSale,
            string? note)
        {
            code = NormalizeCode(code);

            var tax = await _context.Taxes
                .FirstOrDefaultAsync(x => x.Code.ToLower() == code.ToLower());

            if (tax == null)
            {
                tax = new Tax
                {
                    Name = NormalizeName(name),
                    Code = code,
                    TaxType = taxType,
                    DefaultRatePercent = defaultRatePercent,
                    IsRecoverableByDefault = isRecoverableByDefault,
                    IsIncludedInCostByDefault = isIncludedInCostByDefault,
                    IsIncludedInPriceByDefault = isIncludedInPriceByDefault,
                    DefaultCalculationSource = defaultCalculationSource,
                    DefaultCostTreatment = defaultCostTreatment,
                    UseForLocalPurchase = useForLocalPurchase,
                    UseForImportPurchase = useForImportPurchase,
                    UseForSale = useForSale,
                    Note = NormalizeNullableText(note),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.Taxes.AddAsync(tax);
                await _context.SaveChangesAsync();
                return;
            }

            tax.Name = NormalizeName(name);
            tax.TaxType = taxType;
            tax.DefaultRatePercent = defaultRatePercent;
            tax.IsRecoverableByDefault = isRecoverableByDefault;
            tax.IsIncludedInCostByDefault = isIncludedInCostByDefault;
            tax.IsIncludedInPriceByDefault = isIncludedInPriceByDefault;
            tax.DefaultCalculationSource = defaultCalculationSource;
            tax.DefaultCostTreatment = defaultCostTreatment;
            tax.UseForLocalPurchase = useForLocalPurchase;
            tax.UseForImportPurchase = useForImportPurchase;
            tax.UseForSale = useForSale;
            tax.Note = NormalizeNullableText(note);
            tax.IsActive = true;
            tax.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        private static Result<bool> ValidateTaxBase(string name, string code, decimal ratePercent)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<bool>.Fail("Vergi adı boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(code))
                return Result<bool>.Fail("Vergi kodu boş ola bilməz.");

            if (ratePercent < 0)
                return Result<bool>.Fail("Vergi faizi mənfi ola bilməz.");

            if (ratePercent > 100)
                return Result<bool>.Fail("Vergi faizi 100-dən böyük ola bilməz.");

            return Result<bool>.Success(true);
        }

        private static string NormalizeName(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeCode(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "_");
        }

        private static string? NormalizeNullableText(string? value)
        {
            value = value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}