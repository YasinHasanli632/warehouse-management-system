using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Settings ekranı üçün bütün ayarları bir yerdə daşıyan UI model.
    // Bu class entity deyil, migration lazım deyil.
    public  class SettingsUiModel
    {
        public AppSetting AppSetting { get; set; } = null!;
        public WarehouseSetting WarehouseSetting { get; set; } = null!;
        public InvoiceSetting InvoiceSetting { get; set; } = null!;
        public StockSetting StockSetting { get; set; } = null!;
        public CostSetting CostSetting { get; set; } = null!;
        public TaxSetting TaxSetting { get; set; } = null!;
        public ImportSetting ImportSetting { get; set; } = null!;
        public LocalPurchaseSetting LocalPurchaseSetting { get; set; } = null!;

        public List<LocalPurchaseSettingValue> LocalPurchaseValues { get; set; } = new();
        public List<ImportFieldSetting> ImportFieldSettings { get; set; } = new();
        public List<ExpenseType> ExpenseTypes { get; set; } = new();
    }

    // YENI:
    // Runtime/business servislər üçün cache model.
    // Məqsəd: InvoiceService / StockService hər dəfə DB-yə settings üçün getməsin.
    public class SettingsRuntimeCache
    {
        public AppSetting AppSetting { get; set; } = null!;
        public WarehouseSetting WarehouseSetting { get; set; } = null!;
        public InvoiceSetting InvoiceSetting { get; set; } = null!;
        public StockSetting StockSetting { get; set; } = null!;
        public CostSetting CostSetting { get; set; } = null!;
        public TaxSetting TaxSetting { get; set; } = null!;
        public ImportSetting ImportSetting { get; set; } = null!;
        public LocalPurchaseSetting LocalPurchaseSetting { get; set; } = null!;

        public List<LocalPurchaseSettingValue> LocalPurchaseValues { get; set; } = new();
        public List<ImportFieldSetting> ImportFieldSettings { get; set; } = new();
        public List<ExpenseType> ExpenseTypes { get; set; } = new();

        public DateTime LoadedAt { get; set; } = DateTime.Now;
    }

    public class SettingsService
    {
        private readonly AppDbContext _context;

        // YENI:
        // Sadə in-memory cache. WPF desktop app üçün kifayətdir.
        private SettingsRuntimeCache? _cache;

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // UI ÜÇÜN BÜTÜN SETTINGS-LƏRİ BİR DƏFƏYƏ YÜKLƏ
        // ============================================================

        public async Task<Result<SettingsUiModel>> GetAllSettingsForUiAsync()
        {
            var ensureResult = await EnsureDefaultSettingsAsync();

            if (!ensureResult.IsSuccess)
                return Result<SettingsUiModel>.Fail(ensureResult.Message);

            var appSettingResult = await GetSettingsAsync();
            var warehouseResult = await GetWarehouseSettingAsync();
            var invoiceResult = await GetInvoiceSettingAsync();
            var stockResult = await GetStockSettingAsync();
            var costResult = await GetCostSettingAsync();
            var taxResult = await GetTaxSettingAsync();
            var importResult = await GetImportSettingAsync();
            var localResult = await GetLocalPurchaseSettingAsync();
            var localValuesResult = await GetLocalPurchaseValuesAsync();
            var importFieldsResult = await GetImportFieldSettingsAsync();
            var expenseTypesResult = await GetExpenseTypesForSettingsAsync();

            if (!appSettingResult.IsSuccess || appSettingResult.Data == null)
                return Result<SettingsUiModel>.Fail("Ümumi sistem ayarları yüklənmədi.");

            if (!warehouseResult.IsSuccess || warehouseResult.Data == null)
                return Result<SettingsUiModel>.Fail("Anbar/şirkət ayarları yüklənmədi.");

            if (!invoiceResult.IsSuccess || invoiceResult.Data == null)
                return Result<SettingsUiModel>.Fail("Qaimə ayarları yüklənmədi.");

            if (!stockResult.IsSuccess || stockResult.Data == null)
                return Result<SettingsUiModel>.Fail("Stok ayarları yüklənmədi.");

            if (!costResult.IsSuccess || costResult.Data == null)
                return Result<SettingsUiModel>.Fail("Maya dəyəri ayarları yüklənmədi.");

            if (!taxResult.IsSuccess || taxResult.Data == null)
                return Result<SettingsUiModel>.Fail("Vergi ayarları yüklənmədi.");

            if (!importResult.IsSuccess || importResult.Data == null)
                return Result<SettingsUiModel>.Fail("İdxal ayarları yüklənmədi.");

            if (!localResult.IsSuccess || localResult.Data == null)
                return Result<SettingsUiModel>.Fail("Yerli alış ayarları yüklənmədi.");

            var model = new SettingsUiModel
            {
                AppSetting = appSettingResult.Data,
                WarehouseSetting = warehouseResult.Data,
                InvoiceSetting = invoiceResult.Data,
                StockSetting = stockResult.Data,
                CostSetting = costResult.Data,
                TaxSetting = taxResult.Data,
                ImportSetting = importResult.Data,
                LocalPurchaseSetting = localResult.Data,
                LocalPurchaseValues = localValuesResult.Data ?? new List<LocalPurchaseSettingValue>(),
                ImportFieldSettings = importFieldsResult.Data ?? new List<ImportFieldSetting>(),
                ExpenseTypes = expenseTypesResult.Data ?? new List<ExpenseType>()
            };

            return Result<SettingsUiModel>.Success(model, "Bütün ayarlar yükləndi.");
        }

        public async Task<Result<bool>> SaveAllSettingsAsync(SettingsUiModel model)
        {
            if (model == null)
                return Result<bool>.Fail("Yadda saxlanılacaq settings modeli boşdur.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var appResult = await UpdateSettingsAsync(
                    model.AppSetting.AppName,
                    model.AppSetting.CompanyName,
                    model.AppSetting.CompanyVoen,
                    model.AppSetting.CompanyPhone,
                    model.AppSetting.CompanyAddress,
                    model.AppSetting.InvoicePrefix,
                    model.AppSetting.AllowNegativeStock,
                    model.AppSetting.AutoShelfAssign,
                    model.AppSetting.EnableCriticalStockWarning);

                if (!appResult.IsSuccess)
                    return Result<bool>.Fail(appResult.Message);

                var warehouseResult = await UpdateWarehouseSettingAsync(
                    model.WarehouseSetting.AppName,
                    model.WarehouseSetting.CompanyName,
                    model.WarehouseSetting.CompanyVoen,
                    model.WarehouseSetting.CompanyPhone,
                    model.WarehouseSetting.CompanyAddress,
                    model.WarehouseSetting.DefaultCurrency);

                if (!warehouseResult.IsSuccess)
                    return Result<bool>.Fail(warehouseResult.Message);

                var invoiceResult = await UpdateInvoiceSettingAsync(
                    model.InvoiceSetting.InvoicePrefix,
                    model.InvoiceSetting.LockConfirmedInvoice,
                    model.InvoiceSetting.RequireReturnReason,
                    model.InvoiceSetting.RequireShelfSelection,
                    model.InvoiceSetting.RequireBatchSelectionForReturn,
                    model.InvoiceSetting.CopyProductBarcodeToInvoiceItem);

                if (!invoiceResult.IsSuccess)
                    return Result<bool>.Fail(invoiceResult.Message);

                var stockResult = await UpdateStockSettingAsync(
                    model.StockSetting.EnableFIFO,
                    model.StockSetting.PreventNegativeStock,
                    model.StockSetting.CheckShelfCapacity,
                    model.StockSetting.BlockPassiveProductInInvoice,
                    model.StockSetting.AutoCreateBatchOnStockIn);

                if (!stockResult.IsSuccess)
                    return Result<bool>.Fail(stockResult.Message);

                var costResult = await UpdateCostSettingAsync(
                    model.CostSetting.IncludeExpensesInStockCost,
                    model.CostSetting.DefaultAllocationMethod,
                    model.CostSetting.SuggestSalePrice,
                    model.CostSetting.MinimumMarginPercent,
                    model.CostSetting.AutoCalculateCostOnConfirm,
                    model.CostSetting.RecalculateCostWhenExpenseChanges,
                    model.CostSetting.ExcludeZeroAmountExpenses,
                    model.CostSetting.LockCostAfterConfirm);

                if (!costResult.IsSuccess)
                    return Result<bool>.Fail(costResult.Message);

                var taxResult = await UpdateTaxSettingAsync(
                    model.TaxSetting.TaxRegime,
                    model.TaxSetting.EnableVAT,
                    model.TaxSetting.VATPercent,
                    model.TaxSetting.PurchasePricesIncludeVATByDefault,
                    model.TaxSetting.VATRecoverableByDefault,
                    model.TaxSetting.EnableProfitTax,
                    model.TaxSetting.ProfitTaxPercent,
                    model.TaxSetting.EnableSimplifiedTax,
                    model.TaxSetting.SimplifiedTaxPercent,
                    model.TaxSetting.IncludeImportVATInCost,
                    model.TaxSetting.IncludeCustomsDutyInCost,
                    model.TaxSetting.IncludeExciseInCost);

                if (!taxResult.IsSuccess)
                    return Result<bool>.Fail(taxResult.Message);

                var importResult = await UpdateImportSettingAsync(
                    model.ImportSetting.EnableImportInvoice,
                    model.ImportSetting.AutoOpenImportFieldsForForeignSupplier,
                    model.ImportSetting.RequireDeclarationNumber,
                    model.ImportSetting.RequireExchangeRate,
                    model.ImportSetting.UseInvoiceDateExchangeRate,
                    model.ImportSetting.IncludeCustomsDutyInCost,
                    model.ImportSetting.IncludeBrokerFeeInCost,
                    model.ImportSetting.IncludeInsuranceInCost,
                    model.ImportSetting.IncludeTransportInCost);

                if (!importResult.IsSuccess)
                    return Result<bool>.Fail(importResult.Message);

                if (model.LocalPurchaseValues != null && model.LocalPurchaseValues.Any())
                {
                    var values = model.LocalPurchaseValues
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                        .ToDictionary(x => x.Key, x => (string?)x.Value);

                    var localSaveResult = await SetLocalPurchaseValuesAsync(values);

                    if (!localSaveResult.IsSuccess)
                        return Result<bool>.Fail(localSaveResult.Message);
                }

                if (model.ImportFieldSettings != null && model.ImportFieldSettings.Any())
                {
                    foreach (var field in model.ImportFieldSettings)
                    {
                        if (field.Id <= 0)
                            continue;

                        var fieldResult = await UpdateImportFieldSettingAsync(
                            field.Id,
                            field.IsVisible,
                            field.IsRequired,
                            field.ShowOnInvoice,
                            field.SortOrder,
                            field.DisplayName,
                            field.Placeholder,
                            field.DefaultValue,
                            field.OptionsJson);

                        if (!fieldResult.IsSuccess)
                            return Result<bool>.Fail(fieldResult.Message);
                    }
                }

                await transaction.CommitAsync();

                await LoadCacheAsync(forceReload: true);

                return Result<bool>.Success(true, "Bütün ayarlar yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Ayarlar yadda saxlanılarkən xəta baş verdi: {ex.Message}");
            }
        }

        // ============================================================
        // RUNTIME CACHE
        // ============================================================

        public async Task<Result<SettingsRuntimeCache>> LoadCacheAsync(bool forceReload = false)
        {
            if (_cache != null && !forceReload)
                return Result<SettingsRuntimeCache>.Success(_cache, "Settings cache hazırdır.");

            var uiResult = await GetAllSettingsForUiAsync();

            if (!uiResult.IsSuccess || uiResult.Data == null)
                return Result<SettingsRuntimeCache>.Fail(uiResult.Message);

            _cache = new SettingsRuntimeCache
            {
                AppSetting = uiResult.Data.AppSetting,
                WarehouseSetting = uiResult.Data.WarehouseSetting,
                InvoiceSetting = uiResult.Data.InvoiceSetting,
                StockSetting = uiResult.Data.StockSetting,
                CostSetting = uiResult.Data.CostSetting,
                TaxSetting = uiResult.Data.TaxSetting,
                ImportSetting = uiResult.Data.ImportSetting,
                LocalPurchaseSetting = uiResult.Data.LocalPurchaseSetting,
                LocalPurchaseValues = uiResult.Data.LocalPurchaseValues,
                ImportFieldSettings = uiResult.Data.ImportFieldSettings,
                ExpenseTypes = uiResult.Data.ExpenseTypes,
                LoadedAt = DateTime.Now
            };

            return Result<SettingsRuntimeCache>.Success(_cache, "Settings cache yeniləndi.");
        }

        public SettingsRuntimeCache? GetCache()
        {
            return _cache;
        }

        public void ClearCache()
        {
            _cache = null;
        }

        public async Task<Result<InvoiceSetting>> GetInvoiceSettingCachedAsync()
        {
            var cacheResult = await LoadCacheAsync();

            if (!cacheResult.IsSuccess || cacheResult.Data == null)
                return Result<InvoiceSetting>.Fail(cacheResult.Message);

            return Result<InvoiceSetting>.Success(cacheResult.Data.InvoiceSetting);
        }

        public async Task<Result<StockSetting>> GetStockSettingCachedAsync()
        {
            var cacheResult = await LoadCacheAsync();

            if (!cacheResult.IsSuccess || cacheResult.Data == null)
                return Result<StockSetting>.Fail(cacheResult.Message);

            return Result<StockSetting>.Success(cacheResult.Data.StockSetting);
        }

        public async Task<Result<CostSetting>> GetCostSettingCachedAsync()
        {
            var cacheResult = await LoadCacheAsync();

            if (!cacheResult.IsSuccess || cacheResult.Data == null)
                return Result<CostSetting>.Fail(cacheResult.Message);

            return Result<CostSetting>.Success(cacheResult.Data.CostSetting);
        }

        public async Task<Result<TaxSetting>> GetTaxSettingCachedAsync()
        {
            var cacheResult = await LoadCacheAsync();

            if (!cacheResult.IsSuccess || cacheResult.Data == null)
                return Result<TaxSetting>.Fail(cacheResult.Message);

            return Result<TaxSetting>.Success(cacheResult.Data.TaxSetting);
        }

        public async Task<Result<ImportSetting>> GetImportSettingCachedAsync()
        {
            var cacheResult = await LoadCacheAsync();

            if (!cacheResult.IsSuccess || cacheResult.Data == null)
                return Result<ImportSetting>.Fail(cacheResult.Message);

            return Result<ImportSetting>.Success(cacheResult.Data.ImportSetting);
        }

        // ============================================================
        // RESET DEFAULT
        // ============================================================

        public async Task<Result<bool>> ResetToDefaultAsync()
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var app = await _context.AppSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (app != null)
                {
                    app.AppName = "Mebel Anbar Sistemi";
                    app.DefaultCurrency = CurrencyType.AZN;
                    app.InvoicePrefix = "QAI";
                    app.EnableCriticalStockWarning = true;
                    app.AllowNegativeStock = false;
                    app.AutoShelfAssign = false;
                    app.UpdatedAt = DateTime.Now;
                }

                var warehouse = await _context.WarehouseSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (warehouse != null)
                {
                    warehouse.AppName = "Mebel Anbar Sistemi";
                    warehouse.DefaultCurrency = CurrencyType.AZN;
                    warehouse.UpdatedAt = DateTime.Now;
                }

                var invoice = await _context.InvoiceSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (invoice != null)
                {
                    invoice.InvoicePrefix = "QAI";
                    invoice.LockConfirmedInvoice = true;
                    invoice.RequireReturnReason = true;
                    invoice.RequireShelfSelection = true;
                    invoice.RequireBatchSelectionForReturn = true;
                    invoice.CopyProductBarcodeToInvoiceItem = true;
                    invoice.UpdatedAt = DateTime.Now;
                }

                var stock = await _context.StockSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (stock != null)
                {
                    stock.EnableFIFO = true;
                    stock.PreventNegativeStock = true;
                    stock.CheckShelfCapacity = true;
                    stock.BlockPassiveProductInInvoice = true;
                    stock.AutoCreateBatchOnStockIn = true;
                    stock.UpdatedAt = DateTime.Now;
                }

                var cost = await _context.CostSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (cost != null)
                {
                    cost.IncludeExpensesInStockCost = true;
                    cost.DefaultAllocationMethod = CostAllocationMethod.ByAmount;
                    cost.SuggestSalePrice = true;
                    cost.MinimumMarginPercent = 0;
                    cost.AutoCalculateCostOnConfirm = true;
                    cost.RecalculateCostWhenExpenseChanges = true;
                    cost.ExcludeZeroAmountExpenses = true;
                    cost.LockCostAfterConfirm = true;
                    cost.UpdatedAt = DateTime.Now;
                }

                var tax = await _context.TaxSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (tax != null)
                {
                    tax.TaxRegime = TaxRegime.NoTax;
                    tax.EnableVAT = false;
                    tax.VATPercent = 18;
                    tax.PurchasePricesIncludeVATByDefault = true;
                    tax.VATRecoverableByDefault = true;
                    tax.EnableProfitTax = false;
                    tax.ProfitTaxPercent = 20;
                    tax.EnableSimplifiedTax = false;
                    tax.SimplifiedTaxPercent = 2;
                    tax.IncludeImportVATInCost = false;
                    tax.IncludeCustomsDutyInCost = true;
                    tax.IncludeExciseInCost = true;
                    tax.UpdatedAt = DateTime.Now;
                }

                var import = await _context.ImportSettings.FirstOrDefaultAsync(x => x.IsActive);
                if (import != null)
                {
                    import.EnableImportInvoice = true;
                    import.AutoOpenImportFieldsForForeignSupplier = true;
                    import.RequireDeclarationNumber = false;
                    import.RequireExchangeRate = true;
                    import.UseInvoiceDateExchangeRate = false;
                    import.IncludeCustomsDutyInCost = true;
                    import.IncludeBrokerFeeInCost = true;
                    import.IncludeInsuranceInCost = true;
                    import.IncludeTransportInCost = true;
                    import.UpdatedAt = DateTime.Now;
                }

                var local = await _context.LocalPurchaseSettings
                    .Include(x => x.Values)
                    .FirstOrDefaultAsync(x => x.IsActive && x.Code == "LOCAL_PURCHASE");

                if (local != null)
                {
                    foreach (var value in local.Values)
                        value.IsActive = false;

                    foreach (var defaultValue in CreateDefaultLocalPurchaseValues())
                        local.Values.Add(defaultValue);

                    local.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await EnsureDefaultSettingsAsync();
                await LoadCacheAsync(forceReload: true);

                return Result<bool>.Success(true, "Default ayarlar bərpa edildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Default ayarlar bərpa edilərkən xəta baş verdi: {ex.Message}");
            }
        }

        // ============================================================
        // LEGACY / APP SETTINGS
        // ============================================================

        public async Task<Result<AppSetting>> GetSettingsAsync()
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                setting = new AppSetting
                {
                    AppName = "Mebel Anbar Sistemi",
                    DefaultCurrency = CurrencyType.AZN,
                    InvoicePrefix = "QAI",
                    EnableCriticalStockWarning = true,
                    AllowNegativeStock = false,
                    AutoShelfAssign = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.AppSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<AppSetting>.Success(setting, "Ayarlar yükləndi.");
        }

        public async Task<Result<AppSetting>> UpdateSettingsAsync(
            string appName,
            string? companyName,
            string? companyVoen,
            string? companyPhone,
            string? companyAddress,
            string invoicePrefix,
            bool allowNegativeStock,
            bool autoShelfAssign,
            bool enableCriticalStockWarning)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return Result<AppSetting>.Fail("Proqram adı boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(invoicePrefix))
                return Result<AppSetting>.Fail("Qaimə prefix boş ola bilməz.");

            var settingResult = await GetSettingsAsync();

            if (!settingResult.IsSuccess || settingResult.Data == null)
                return Result<AppSetting>.Fail("Ayarlar tapılmadı.");

            var setting = settingResult.Data;

            setting.AppName = appName.Trim();
            setting.CompanyName = ToNull(companyName);
            setting.CompanyVoen = ToNull(companyVoen);
            setting.CompanyPhone = ToNull(companyPhone);
            setting.CompanyAddress = ToNull(companyAddress);
            setting.InvoicePrefix = invoicePrefix.Trim().ToUpper();
            setting.AllowNegativeStock = allowNegativeStock;
            setting.AutoShelfAssign = autoShelfAssign;
            setting.EnableCriticalStockWarning = enableCriticalStockWarning;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<AppSetting>.Success(setting, "Sistem ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // WAREHOUSE SETTINGS
        // ============================================================

        public async Task<Result<WarehouseSetting>> GetWarehouseSettingAsync()
        {
            var setting = await _context.WarehouseSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                var legacy = await GetSettingsAsync();

                setting = new WarehouseSetting
                {
                    AppName = legacy.Data?.AppName ?? "Mebel Anbar Sistemi",
                    CompanyName = legacy.Data?.CompanyName,
                    CompanyVoen = legacy.Data?.CompanyVoen,
                    CompanyPhone = legacy.Data?.CompanyPhone,
                    CompanyAddress = legacy.Data?.CompanyAddress,
                    DefaultCurrency = legacy.Data?.DefaultCurrency ?? CurrencyType.AZN,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.WarehouseSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<WarehouseSetting>.Success(setting, "Anbar/şirkət ayarları yükləndi.");
        }

        public async Task<Result<WarehouseSetting>> UpdateWarehouseSettingAsync(
            string appName,
            string? companyName,
            string? companyVoen,
            string? companyPhone,
            string? companyAddress,
            CurrencyType defaultCurrency)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return Result<WarehouseSetting>.Fail("Proqram adı boş ola bilməz.");

            var result = await GetWarehouseSettingAsync();

            if (!result.IsSuccess || result.Data == null)
                return Result<WarehouseSetting>.Fail("Anbar/şirkət ayarları tapılmadı.");

            var setting = result.Data;

            setting.AppName = appName.Trim();
            setting.CompanyName = ToNull(companyName);
            setting.CompanyVoen = ToNull(companyVoen);
            setting.CompanyPhone = ToNull(companyPhone);
            setting.CompanyAddress = ToNull(companyAddress);
            setting.DefaultCurrency = defaultCurrency;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<WarehouseSetting>.Success(setting, "Anbar/şirkət ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // INVOICE SETTINGS
        // ============================================================

        public async Task<Result<InvoiceSetting>> GetInvoiceSettingAsync()
        {
            var setting = await _context.InvoiceSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                var legacy = await GetSettingsAsync();

                setting = new InvoiceSetting
                {
                    InvoicePrefix = legacy.Data?.InvoicePrefix ?? "QAI",
                    LockConfirmedInvoice = true,
                    RequireReturnReason = true,
                    RequireShelfSelection = true,
                    RequireBatchSelectionForReturn = true,
                    CopyProductBarcodeToInvoiceItem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.InvoiceSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<InvoiceSetting>.Success(setting, "Qaimə ayarları yükləndi.");
        }

        public async Task<Result<InvoiceSetting>> UpdateInvoiceSettingAsync(
            string invoicePrefix,
            bool lockConfirmedInvoice,
            bool requireReturnReason,
            bool requireShelfSelection,
            bool requireBatchSelectionForReturn,
            bool copyProductBarcodeToInvoiceItem)
        {
            if (string.IsNullOrWhiteSpace(invoicePrefix))
                return Result<InvoiceSetting>.Fail("Qaimə prefix boş ola bilməz.");

            var result = await GetInvoiceSettingAsync();

            if (!result.IsSuccess || result.Data == null)
                return Result<InvoiceSetting>.Fail("Qaimə ayarları tapılmadı.");

            var setting = result.Data;

            setting.InvoicePrefix = invoicePrefix.Trim().ToUpper();
            setting.LockConfirmedInvoice = lockConfirmedInvoice;
            setting.RequireReturnReason = requireReturnReason;
            setting.RequireShelfSelection = requireShelfSelection;
            setting.RequireBatchSelectionForReturn = requireBatchSelectionForReturn;
            setting.CopyProductBarcodeToInvoiceItem = copyProductBarcodeToInvoiceItem;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<InvoiceSetting>.Success(setting, "Qaimə ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // STOCK SETTINGS
        // ============================================================

        public async Task<Result<StockSetting>> GetStockSettingAsync()
        {
            var setting = await _context.StockSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                var legacy = await GetSettingsAsync();

                setting = new StockSetting
                {
                    EnableFIFO = true,
                    PreventNegativeStock = !(legacy.Data?.AllowNegativeStock ?? false),
                    CheckShelfCapacity = true,
                    BlockPassiveProductInInvoice = true,
                    AutoCreateBatchOnStockIn = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.StockSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<StockSetting>.Success(setting, "Stok ayarları yükləndi.");
        }

        public async Task<Result<StockSetting>> UpdateStockSettingAsync(
            bool enableFIFO,
            bool preventNegativeStock,
            bool checkShelfCapacity,
            bool blockPassiveProductInInvoice,
            bool autoCreateBatchOnStockIn)
        {
            var result = await GetStockSettingAsync();

            if (!result.IsSuccess || result.Data == null)
                return Result<StockSetting>.Fail("Stok ayarları tapılmadı.");

            var setting = result.Data;

            setting.EnableFIFO = enableFIFO;
            setting.PreventNegativeStock = preventNegativeStock;
            setting.CheckShelfCapacity = checkShelfCapacity;
            setting.BlockPassiveProductInInvoice = blockPassiveProductInInvoice;
            setting.AutoCreateBatchOnStockIn = autoCreateBatchOnStockIn;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<StockSetting>.Success(setting, "Stok ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // COST SETTINGS
        // ============================================================

        public async Task<Result<CostSetting>> GetCostSettingAsync()
        {
            var setting = await _context.CostSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                setting = new CostSetting
                {
                    IncludeExpensesInStockCost = true,
                    DefaultAllocationMethod = CostAllocationMethod.ByAmount,
                    SuggestSalePrice = true,
                    MinimumMarginPercent = 0,
                    AutoCalculateCostOnConfirm = true,
                    RecalculateCostWhenExpenseChanges = true,
                    ExcludeZeroAmountExpenses = true,
                    LockCostAfterConfirm = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.CostSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<CostSetting>.Success(setting, "Maya dəyəri ayarları yükləndi.");
        }

        public async Task<Result<CostSetting>> UpdateCostSettingAsync(
            bool includeExpensesInStockCost,
            CostAllocationMethod defaultAllocationMethod,
            bool suggestSalePrice,
            decimal minimumMarginPercent,
            bool autoCalculateCostOnConfirm,
            bool recalculateCostWhenExpenseChanges,
            bool excludeZeroAmountExpenses,
            bool lockCostAfterConfirm)
        {
            if (minimumMarginPercent < 0)
                return Result<CostSetting>.Fail("Minimum marja faizi mənfi ola bilməz.");

            var result = await GetCostSettingAsync();

            if (!result.IsSuccess || result.Data == null)
                return Result<CostSetting>.Fail("Maya dəyəri ayarları tapılmadı.");

            var setting = result.Data;

            setting.IncludeExpensesInStockCost = includeExpensesInStockCost;
            setting.DefaultAllocationMethod = defaultAllocationMethod;
            setting.SuggestSalePrice = suggestSalePrice;
            setting.MinimumMarginPercent = minimumMarginPercent;
            setting.AutoCalculateCostOnConfirm = autoCalculateCostOnConfirm;
            setting.RecalculateCostWhenExpenseChanges = recalculateCostWhenExpenseChanges;
            setting.ExcludeZeroAmountExpenses = excludeZeroAmountExpenses;
            setting.LockCostAfterConfirm = lockCostAfterConfirm;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<CostSetting>.Success(setting, "Maya dəyəri ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // TAX SETTINGS
        // ============================================================

        public async Task<Result<TaxSetting>> GetTaxSettingAsync()
        {
            var setting = await _context.TaxSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                setting = new TaxSetting
                {
                    TaxRegime = TaxRegime.NoTax,
                    EnableVAT = false,
                    VATPercent = 18,
                    PurchasePricesIncludeVATByDefault = true,
                    VATRecoverableByDefault = true,
                    EnableProfitTax = false,
                    ProfitTaxPercent = 20,
                    EnableSimplifiedTax = false,
                    SimplifiedTaxPercent = 2,
                    IncludeImportVATInCost = false,
                    IncludeCustomsDutyInCost = true,
                    IncludeExciseInCost = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.TaxSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<TaxSetting>.Success(setting, "Vergi ayarları yükləndi.");
        }

        public async Task<Result<TaxSetting>> UpdateTaxSettingAsync(
            TaxRegime taxRegime,
            bool enableVAT,
            decimal vatPercent,
            bool purchasePricesIncludeVATByDefault,
            bool vatRecoverableByDefault,
            bool enableProfitTax,
            decimal profitTaxPercent,
            bool enableSimplifiedTax,
            decimal simplifiedTaxPercent,
            bool includeImportVATInCost,
            bool includeCustomsDutyInCost,
            bool includeExciseInCost)
        {
            if (vatPercent < 0)
                return Result<TaxSetting>.Fail("ƏDV faizi mənfi ola bilməz.");

            if (profitTaxPercent < 0)
                return Result<TaxSetting>.Fail("Mənfəət vergisi faizi mənfi ola bilməz.");

            if (simplifiedTaxPercent < 0)
                return Result<TaxSetting>.Fail("Sadələşdirilmiş vergi faizi mənfi ola bilməz.");

            var result = await GetTaxSettingAsync();

            if (!result.IsSuccess || result.Data == null)
                return Result<TaxSetting>.Fail("Vergi ayarları tapılmadı.");

            var setting = result.Data;

            setting.TaxRegime = taxRegime;
            setting.EnableVAT = enableVAT;
            setting.VATPercent = vatPercent;
            setting.PurchasePricesIncludeVATByDefault = purchasePricesIncludeVATByDefault;
            setting.VATRecoverableByDefault = vatRecoverableByDefault;
            setting.EnableProfitTax = enableProfitTax;
            setting.ProfitTaxPercent = profitTaxPercent;
            setting.EnableSimplifiedTax = enableSimplifiedTax;
            setting.SimplifiedTaxPercent = simplifiedTaxPercent;
            setting.IncludeImportVATInCost = includeImportVATInCost;
            setting.IncludeCustomsDutyInCost = includeCustomsDutyInCost;
            setting.IncludeExciseInCost = includeExciseInCost;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<TaxSetting>.Success(setting, "Vergi ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // IMPORT SETTINGS
        // ============================================================

        public async Task<Result<ImportSetting>> GetImportSettingAsync()
        {
            var setting = await _context.ImportSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            if (setting == null)
            {
                setting = new ImportSetting
                {
                    EnableImportInvoice = true,
                    AutoOpenImportFieldsForForeignSupplier = true,
                    RequireDeclarationNumber = false,
                    RequireExchangeRate = true,
                    UseInvoiceDateExchangeRate = false,
                    IncludeCustomsDutyInCost = true,
                    IncludeBrokerFeeInCost = true,
                    IncludeInsuranceInCost = true,
                    IncludeTransportInCost = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.ImportSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }

            return Result<ImportSetting>.Success(setting, "İdxal ayarları yükləndi.");
        }

        public async Task<Result<ImportSetting>> UpdateImportSettingAsync(
            bool enableImportInvoice,
            bool autoOpenImportFieldsForForeignSupplier,
            bool requireDeclarationNumber,
            bool requireExchangeRate,
            bool useInvoiceDateExchangeRate,
            bool includeCustomsDutyInCost,
            bool includeBrokerFeeInCost,
            bool includeInsuranceInCost,
            bool includeTransportInCost)
        {
            var result = await GetImportSettingAsync();

            if (!result.IsSuccess || result.Data == null)
                return Result<ImportSetting>.Fail("İdxal ayarları tapılmadı.");

            var setting = result.Data;

            setting.EnableImportInvoice = enableImportInvoice;
            setting.AutoOpenImportFieldsForForeignSupplier = autoOpenImportFieldsForForeignSupplier;
            setting.RequireDeclarationNumber = requireDeclarationNumber;
            setting.RequireExchangeRate = requireExchangeRate;
            setting.UseInvoiceDateExchangeRate = useInvoiceDateExchangeRate;
            setting.IncludeCustomsDutyInCost = includeCustomsDutyInCost;
            setting.IncludeBrokerFeeInCost = includeBrokerFeeInCost;
            setting.IncludeInsuranceInCost = includeInsuranceInCost;
            setting.IncludeTransportInCost = includeTransportInCost;
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<ImportSetting>.Success(setting, "İdxal ayarları yadda saxlanıldı.");
        }

        // ============================================================
        // IMPORT FIELD SETTINGS
        // ============================================================

        public async Task<Result<List<ImportFieldSetting>>> GetImportFieldSettingsAsync()
        {
            var fields = await _context.ImportFieldSettings
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            if (!fields.Any())
            {
                fields = CreateDefaultImportFieldSettings();

                await _context.ImportFieldSettings.AddRangeAsync(fields);
                await _context.SaveChangesAsync();
            }

            return Result<List<ImportFieldSetting>>.Success(fields, "İdxal field ayarları yükləndi.");
        }

        public async Task<Result<ImportFieldSetting>> UpdateImportFieldSettingAsync(
            int id,
            bool isVisible,
            bool isRequired,
            bool showOnInvoice,
            int sortOrder,
            string? displayName = null,
            string? placeholder = null,
            string? defaultValue = null,
            string? optionsJson = null)
        {
            var field = await _context.ImportFieldSettings
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (field == null)
                return Result<ImportFieldSetting>.Fail("İdxal field ayarı tapılmadı.");

            field.IsVisible = isVisible;
            field.IsRequired = isRequired;
            field.ShowOnInvoice = showOnInvoice;
            field.SortOrder = sortOrder;

            if (!string.IsNullOrWhiteSpace(displayName))
                field.DisplayName = displayName.Trim();

            field.Placeholder = ToNull(placeholder);
            field.DefaultValue = ToNull(defaultValue);
            field.OptionsJson = ToNull(optionsJson);
            field.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<ImportFieldSetting>.Success(field, "İdxal field ayarı yadda saxlanıldı.");
        }

        // ============================================================
        // LOCAL PURCHASE SETTINGS
        // ============================================================

        public async Task<Result<LocalPurchaseSetting>> GetLocalPurchaseSettingAsync()
        {
            var setting = await _context.LocalPurchaseSettings
                .Include(x => x.Values.Where(v => v.IsActive))
                .FirstOrDefaultAsync(x => x.IsActive && x.Code == "LOCAL_PURCHASE");

            if (setting == null)
            {
                setting = new LocalPurchaseSetting
                {
                    Name = "Yerli Alış Ayarları",
                    Code = "LOCAL_PURCHASE",
                    Description = "Yerli alış qaimələri və əlavə xərclər üçün ayarlar.",
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                foreach (var value in CreateDefaultLocalPurchaseValues())
                    setting.Values.Add(value);

                await _context.LocalPurchaseSettings.AddAsync(setting);
                await _context.SaveChangesAsync();
            }
            else
            {
                var defaults = CreateDefaultLocalPurchaseValues();

                foreach (var defaultValue in defaults)
                {
                    var exists = setting.Values.Any(x =>
                        x.IsActive &&
                        x.Key.Equals(defaultValue.Key, StringComparison.OrdinalIgnoreCase));

                    if (exists)
                        continue;

                    setting.Values.Add(defaultValue);
                }

                setting.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Result<LocalPurchaseSetting>.Success(setting, "Yerli alış ayarları yükləndi.");
        }

        public async Task<Result<List<LocalPurchaseSettingValue>>> GetLocalPurchaseValuesAsync()
        {
            var settingResult = await GetLocalPurchaseSettingAsync();

            if (!settingResult.IsSuccess || settingResult.Data == null)
                return Result<List<LocalPurchaseSettingValue>>.Fail("Yerli alış ayarları tapılmadı.");

            var values = settingResult.Data.Values
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToList();

            return Result<List<LocalPurchaseSettingValue>>.Success(values, "Yerli alış key-value ayarları yükləndi.");
        }

        public async Task<Result<string>> GetLocalPurchaseValueAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result<string>.Fail("Ayar key boş ola bilməz.");

            var settingResult = await GetLocalPurchaseSettingAsync();

            if (!settingResult.IsSuccess || settingResult.Data == null)
                return Result<string>.Fail("Yerli alış ayarları tapılmadı.");

            var value = settingResult.Data.Values
                .FirstOrDefault(x =>
                    x.IsActive &&
                    x.Key.Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));

            if (value == null)
                return Result<string>.Fail($"Yerli alış ayarı tapılmadı: {key}");

            return Result<string>.Success(value.Value);
        }

        public async Task<Result<bool>> SetLocalPurchaseValueAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result<bool>.Fail("Ayar key boş ola bilməz.");

            var settingResult = await GetLocalPurchaseSettingAsync();

            if (!settingResult.IsSuccess || settingResult.Data == null)
                return Result<bool>.Fail("Yerli alış ayarları tapılmadı.");

            var normalizedKey = key.Trim();

            var setting = settingResult.Data;

            var settingValue = setting.Values
                .FirstOrDefault(x =>
                    x.IsActive &&
                    x.Key.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase));

            if (settingValue == null)
            {
                settingValue = new LocalPurchaseSettingValue
                {
                    Key = normalizedKey,
                    Value = value,
                    ValueType = "String",
                    DisplayName = normalizedKey,
                    SortOrder = setting.Values.Any() ? setting.Values.Max(x => x.SortOrder) + 1 : 1,
                    IsSystem = false,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                setting.Values.Add(settingValue);
            }
            else
            {
                settingValue.Value = value;
                settingValue.UpdatedAt = DateTime.Now;
            }

            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Yerli alış ayarı yadda saxlanıldı.");
        }

        public async Task<Result<bool>> SetLocalPurchaseValuesAsync(Dictionary<string, string?> values)
        {
            if (values == null || !values.Any())
                return Result<bool>.Success(true, "Yadda saxlanılacaq ayar yoxdur.");

            foreach (var pair in values)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                var value = pair.Value ?? string.Empty;

                var result = await SetLocalPurchaseValueAsync(pair.Key, value);

                if (!result.IsSuccess)
                    return result;
            }

            return Result<bool>.Success(true, "Yerli alış ayarları yadda saxlanıldı.");
        }

        public async Task<Result<bool>> GetLocalPurchaseBoolAsync(string key, bool defaultValue = false)
        {
            var result = await GetLocalPurchaseValueAsync(key);

            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Data))
                return Result<bool>.Success(defaultValue);

            if (bool.TryParse(result.Data, out var parsed))
                return Result<bool>.Success(parsed);

            if (result.Data == "1")
                return Result<bool>.Success(true);

            if (result.Data == "0")
                return Result<bool>.Success(false);

            return Result<bool>.Success(defaultValue);
        }

        public async Task<Result<decimal>> GetLocalPurchaseDecimalAsync(string key, decimal defaultValue = 0)
        {
            var result = await GetLocalPurchaseValueAsync(key);

            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Data))
                return Result<decimal>.Success(defaultValue);

            if (decimal.TryParse(result.Data, out var parsed))
                return Result<decimal>.Success(parsed);

            var normalized = result.Data.Replace(",", ".");

            if (decimal.TryParse(
                    normalized,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out parsed))
            {
                return Result<decimal>.Success(parsed);
            }

            return Result<decimal>.Success(defaultValue);
        }

        public async Task<Result<CostAllocationMethod>> GetLocalPurchaseAllocationMethodAsync()
        {
            var result = await GetLocalPurchaseValueAsync("DefaultExpenseAllocationMethod");

            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Data))
                return Result<CostAllocationMethod>.Success(CostAllocationMethod.ByQuantity);

            if (Enum.TryParse<CostAllocationMethod>(result.Data, true, out var parsed))
                return Result<CostAllocationMethod>.Success(parsed);

            return Result<CostAllocationMethod>.Success(CostAllocationMethod.ByQuantity);
        }

        // ============================================================
        // EXPENSE TYPES
        // ============================================================

        public async Task<Result<List<ExpenseType>>> GetExpenseTypesForSettingsAsync()
        {
            var expenses = await _context.ExpenseTypes
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync();

            return Result<List<ExpenseType>>.Success(expenses, "Xərc növləri yükləndi.");
        }

        // ============================================================
        // DEFAULT ENSURE
        // ============================================================

        public async Task<Result<bool>> EnsureDefaultSettingsAsync()
        {
            await GetSettingsAsync();
            await GetWarehouseSettingAsync();
            await GetInvoiceSettingAsync();
            await GetStockSettingAsync();
            await GetCostSettingAsync();
            await GetTaxSettingAsync();
            await GetImportSettingAsync();
            await GetImportFieldSettingsAsync();
            await GetLocalPurchaseSettingAsync();

            return Result<bool>.Success(true, "Default ayarlar hazırlandı.");
        }

        // ============================================================
        // DEFAULT DATA HELPERS
        // ============================================================

        private static List<LocalPurchaseSettingValue> CreateDefaultLocalPurchaseValues()
        {
            return new List<LocalPurchaseSettingValue>
            {
                new LocalPurchaseSettingValue
                {
                    Key = "EnableLocalPurchaseInvoice",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "Yerli alış qaiməsi aktiv olsun",
                    Description = "Adi StockIn qaimələrinin yaradılmasına icazə verir.",
                    SortOrder = 1,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "EnableAdditionalExpenses",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "Əlavə xərclər aktiv olsun",
                    Description = "Yerli alış qaiməsinə daşıma, fəhlə, yol pulu və digər xərclərin əlavə olunmasına icazə verir.",
                    SortOrder = 2,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "AutoCalculateCostOnConfirm",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "Təsdiq zamanı maya avtomatik hesablansın",
                    Description = "Qaimə təsdiqlənəndə CostCalculationService avtomatik işləsin.",
                    SortOrder = 3,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "DefaultExpenseAllocationMethod",
                    Value = "ByQuantity",
                    ValueType = "Enum",
                    DisplayName = "Default xərc paylama üsulu",
                    Description = "Xərc item-lərə necə paylansın: ByQuantity, ByAmount və s.",
                    SortOrder = 4,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "EnableVATCalculation",
                    Value = "false",
                    ValueType = "Boolean",
                    DisplayName = "Yerli alışda ƏDV hesabla",
                    Description = "ProductTax yoxdursa fallback kimi yerli alış ƏDV-si hesablansın.",
                    SortOrder = 5,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "VATPercent",
                    Value = "18",
                    ValueType = "Decimal",
                    DisplayName = "ƏDV faizi",
                    Description = "Yerli alış üçün default ƏDV faizi.",
                    SortOrder = 6,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "PurchaseVATIncludedInPrice",
                    Value = "false",
                    ValueType = "Boolean",
                    DisplayName = "Alış qiymətinə ƏDV daxildir",
                    Description = "Yerli alış qiyməti ƏDV daxil yazılırsa aktiv edilir.",
                    SortOrder = 7,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "PurchaseVATIncludedInCost",
                    Value = "false",
                    ValueType = "Boolean",
                    DisplayName = "ƏDV maya dəyərinə daxil edilsin",
                    Description = "ƏDV recoverable deyilsə maya dəyərinə düşə bilər.",
                    SortOrder = 8,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "RecalculateBatchCostWhenExpenseChanges",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "Xərc dəyişəndə batch maya yenilənsin",
                    Description = "Draft qaimədə xərc dəyişəndə maya yenidən hesablanacaq.",
                    SortOrder = 9,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "IgnoreZeroAmountExpenses",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "0 məbləğli xərcləri nəzərə alma",
                    Description = "Sıfır məbləğli xərc sətirləri maya hesabına qatılmasın.",
                    SortOrder = 10,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "HidePassiveExpenses",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "Passiv xərc növlərini gizlət",
                    Description = "UI-də passiv xərc növlərini göstərmir.",
                    SortOrder = 11,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new LocalPurchaseSettingValue
                {
                    Key = "ShowVATSeparatelyInReports",
                    Value = "true",
                    ValueType = "Boolean",
                    DisplayName = "ƏDV hesabatda ayrıca görünsün",
                    Description = "Yerli alış hesabatlarında ƏDV ayrıca sətir/sütun kimi göstərilsin.",
                    SortOrder = 12,
                    IsSystem = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                }
            };
        }

        private static List<ImportFieldSetting> CreateDefaultImportFieldSettings()
        {
            return new List<ImportFieldSetting>
            {
                new ImportFieldSetting
                {
                    FieldKey = "DeclarationNumber",
                    DisplayName = "Bəyannamə nömrəsi",
                    FieldType = FieldDataType.Text,
                    IsVisible = true,
                    IsRequired = false,
                    ShowOnInvoice = true,
                    SortOrder = 1,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "ImportDate",
                    DisplayName = "İdxal tarixi",
                    FieldType = FieldDataType.Date,
                    IsVisible = true,
                    IsRequired = false,
                    ShowOnInvoice = true,
                    SortOrder = 2,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "OriginCountry",
                    DisplayName = "Mənşə ölkəsi",
                    FieldType = FieldDataType.Text,
                    IsVisible = true,
                    IsRequired = false,
                    ShowOnInvoice = true,
                    SortOrder = 3,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "CustomsPoint",
                    DisplayName = "Gömrük məntəqəsi",
                    FieldType = FieldDataType.Text,
                    IsVisible = true,
                    IsRequired = false,
                    ShowOnInvoice = true,
                    SortOrder = 4,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "Currency",
                    DisplayName = "Valyuta",
                    FieldType = FieldDataType.Dropdown,
                    IsVisible = true,
                    IsRequired = true,
                    ShowOnInvoice = true,
                    OptionsJson = "AZN,USD,EUR,TRY,RUB",
                    SortOrder = 5,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "ExchangeRate",
                    DisplayName = "Məzənnə",
                    FieldType = FieldDataType.Number,
                    IsVisible = true,
                    IsRequired = true,
                    ShowOnInvoice = true,
                    SortOrder = 6,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "ForeignInvoiceNumber",
                    DisplayName = "Xarici invoice nömrəsi",
                    FieldType = FieldDataType.Text,
                    IsVisible = true,
                    IsRequired = false,
                    ShowOnInvoice = true,
                    SortOrder = 7,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new ImportFieldSetting
                {
                    FieldKey = "TransportDocumentNumber",
                    DisplayName = "Transport sənədi nömrəsi",
                    FieldType = FieldDataType.Text,
                    IsVisible = true,
                    IsRequired = false,
                    ShowOnInvoice = true,
                    SortOrder = 8,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                }
            };
        }

        private static string? ToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}