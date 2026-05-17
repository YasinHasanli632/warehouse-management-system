using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Qaimə səviyyəsində manual/import/header vergi qeydlərini idarə edən servis.
    // Bu servis StockService deyil.
    // Bu servis maya dəyərini yekun hesablamır.
    // Sadəcə InvoiceTax snapshot yaradır və qaimənin vergi toplamlarını yeniləyir.
    //
    // Əsas istifadə yerləri:
    // 1) İdxal qaiməsində gömrük rüsumu / idxal ƏDV / aksiz əlavə etmək
    // 2) Manual əlavə vergi/rüsum yazmaq
    // 3) Header-level tax əlavə etmək
    // 4) TaxAllocationService üçün InvoiceTax məlumatı hazırlamaq
    public class InvoiceTaxService
    {
        private readonly AppDbContext _context;

        public InvoiceTaxService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<InvoiceTax>>> GetByInvoiceIdAsync(int invoiceId, bool includePassive = false)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<List<InvoiceTax>>.Fail("Qaimə düzgün seçilməyib.");

                var query = _context.InvoiceTaxes
                    .Include(x => x.Allocations.Where(a => a.IsActive))
                    .Where(x => x.InvoiceId == invoiceId)
                    .AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive);

                var data = await query
                    .OrderBy(x => x.TaxType)
                    .ThenBy(x => x.TaxName)
                    .ToListAsync();

                return Result<List<InvoiceTax>>.Success(data, "Qaimə vergiləri yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceTax>>.Fail($"Qaimə vergiləri yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceTax>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return Result<InvoiceTax>.Fail("Vergi qeydi düzgün seçilməyib.");

                var tax = await _context.InvoiceTaxes
                    .Include(x => x.Invoice)
                    .Include(x => x.Allocations.Where(a => a.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (tax == null)
                    return Result<InvoiceTax>.Fail("Qaimə vergi qeydi tapılmadı.");

                return Result<InvoiceTax>.Success(tax, "Qaimə vergi qeydi yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceTax>.Fail($"Qaimə vergi qeydi yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceTax>> AddFromTaxCatalogAsync(
            int invoiceId,
            int taxId,
            decimal taxBaseAmount,
            decimal? ratePercent = null,
            decimal? manualTaxAmount = null,
            CurrencyType? currency = null,
            decimal? exchangeRate = null,
            TaxCalculationSource? calculationSource = null,
            TaxCostTreatment? costTreatment = null,
            bool? isIncludedInCost = null,
            bool? isRecoverable = null,
            bool? shouldAllocateToItems = null,
            CostAllocationMethod? allocationMethod = null,
            string? note = null,
            bool saveChanges = true)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<InvoiceTax>.Fail("Qaimə düzgün seçilməyib.");

                if (taxId <= 0)
                    return Result<InvoiceTax>.Fail("Vergi düzgün seçilməyib.");

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<InvoiceTax>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<InvoiceTax>.Fail("Yalnız draft qaiməyə vergi əlavə etmək olar.");

                if (invoice.IsLocked)
                    return Result<InvoiceTax>.Fail("Qaimə kilidlidir. Vergi əlavə etmək olmaz.");

                var tax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.Id == taxId && x.IsActive);

                if (tax == null)
                    return Result<InvoiceTax>.Fail("Aktiv vergi tapılmadı.");

                var finalRate = ratePercent ?? tax.DefaultRatePercent;
                var finalCurrency = currency ?? invoice.Currency;
                var finalExchangeRate = NormalizeExchangeRate(exchangeRate ?? invoice.ExchangeRate);

                var finalCostTreatment = costTreatment ?? tax.DefaultCostTreatment;
                var finalIncludedInCost = isIncludedInCost ?? tax.IsIncludedInCostByDefault;
                var finalRecoverable = isRecoverable ?? tax.IsRecoverableByDefault;

                var finalShouldAllocate = shouldAllocateToItems ?? finalIncludedInCost;
                var finalAllocationMethod = allocationMethod ?? CostAllocationMethod.ByAmount;

                return await AddTaxAsync(
                    invoiceId: invoiceId,
                    taxType: tax.TaxType,
                    taxName: tax.Name,
                    calculationSource: calculationSource ?? tax.DefaultCalculationSource,
                    costTreatment: finalCostTreatment,
                    ratePercent: finalRate,
                    taxBaseAmount: taxBaseAmount,
                    manualTaxAmount: manualTaxAmount,
                    isIncludedInCost: finalIncludedInCost,
                    isRecoverable: finalRecoverable,
                    currency: finalCurrency,
                    exchangeRate: finalExchangeRate,
                    shouldAllocateToItems: finalShouldAllocate,
                    allocationMethod: finalAllocationMethod,
                    note: note,
                    saveChanges: saveChanges);
            }
            catch (Exception ex)
            {
                return Result<InvoiceTax>.Fail($"Vergi qeydi əlavə olunmadı: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceTax>> AddTaxAsync(
            int invoiceId,
            TaxType taxType,
            decimal ratePercent,
            decimal taxBaseAmount,
            bool isIncludedInCost,
            bool isRecoverable,
            string? note = null,
            bool saveChanges = true)
        {
            return await AddTaxAsync(
                invoiceId: invoiceId,
                taxType: taxType,
                taxName: GetDefaultTaxName(taxType),
                calculationSource: TaxCalculationSource.Manual,
                costTreatment: ResolveCostTreatment(isRecoverable, isIncludedInCost),
                ratePercent: ratePercent,
                taxBaseAmount: taxBaseAmount,
                manualTaxAmount: null,
                isIncludedInCost: isIncludedInCost,
                isRecoverable: isRecoverable,
                currency: CurrencyType.AZN,
                exchangeRate: 1,
                shouldAllocateToItems: isIncludedInCost,
                allocationMethod: CostAllocationMethod.ByAmount,
                note: note,
                saveChanges: saveChanges);
        }

        public async Task<Result<InvoiceTax>> AddTaxAsync(
            int invoiceId,
            TaxType taxType,
            string taxName,
            TaxCalculationSource calculationSource,
            TaxCostTreatment costTreatment,
            decimal ratePercent,
            decimal taxBaseAmount,
            decimal? manualTaxAmount,
            bool isIncludedInCost,
            bool isRecoverable,
            CurrencyType currency,
            decimal exchangeRate,
            bool shouldAllocateToItems,
            CostAllocationMethod allocationMethod,
            string? note = null,
            bool saveChanges = true)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<InvoiceTax>.Fail("Qaimə düzgün seçilməyib.");

                if (string.IsNullOrWhiteSpace(taxName))
                    return Result<InvoiceTax>.Fail("Vergi adı boş ola bilməz.");

                if (ratePercent < 0 || ratePercent > 100)
                    return Result<InvoiceTax>.Fail("Vergi faizi 0-100 aralığında olmalıdır.");

                if (taxBaseAmount < 0)
                    return Result<InvoiceTax>.Fail("Vergi bazası mənfi ola bilməz.");

                if (manualTaxAmount.HasValue && manualTaxAmount.Value < 0)
                    return Result<InvoiceTax>.Fail("Vergi məbləği mənfi ola bilməz.");

                exchangeRate = NormalizeExchangeRate(exchangeRate);

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<InvoiceTax>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<InvoiceTax>.Fail("Yalnız draft qaiməyə vergi əlavə etmək olar.");

                if (invoice.IsLocked)
                    return Result<InvoiceTax>.Fail("Qaimə kilidlidir. Vergi əlavə etmək olmaz.");

                var taxAmount = manualTaxAmount ?? Math.Round(taxBaseAmount * ratePercent / 100m, 2);
                var localTaxAmount = Math.Round(taxAmount * exchangeRate, 2);

                var tax = new InvoiceTax
                {
                    InvoiceId = invoiceId,
                    TaxType = taxType,
                    TaxName = taxName.Trim(),

                    CalculationSource = calculationSource,
                    CostTreatment = costTreatment,

                    RatePercent = ratePercent,
                    TaxBaseAmount = taxBaseAmount,
                    TaxAmount = taxAmount,

                    IsIncludedInCost = isIncludedInCost,
                    IsRecoverable = isRecoverable,

                    Currency = currency,
                    ExchangeRate = exchangeRate,
                    LocalTaxAmount = localTaxAmount,

                    ShouldAllocateToItems = shouldAllocateToItems,
                    AllocationMethod = allocationMethod,

                    Note = NormalizeNullableText(note),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.InvoiceTaxes.AddAsync(tax);

                await RecalculateTaxTotalsForInvoiceInternalAsync(invoice, saveChanges: false);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<InvoiceTax>.Success(tax, "Vergi qeydi əlavə olundu.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceTax>.Fail($"Vergi qeydi əlavə olunmadı: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceTax>> AddImportVatAsync(
            int invoiceId,
            decimal taxBaseAmount,
            decimal ratePercent = 18,
            bool isRecoverable = true,
            bool includeInCost = false,
            string? note = null,
            bool saveChanges = true)
        {
            return await AddTaxAsync(
                invoiceId: invoiceId,
                taxType: TaxType.ImportVAT,
                taxName: "İdxal ƏDV",
                calculationSource: TaxCalculationSource.Import,
                costTreatment: includeInCost ? TaxCostTreatment.IncludedInCost : TaxCostTreatment.Recoverable,
                ratePercent: ratePercent,
                taxBaseAmount: taxBaseAmount,
                manualTaxAmount: null,
                isIncludedInCost: includeInCost,
                isRecoverable: isRecoverable,
                currency: CurrencyType.AZN,
                exchangeRate: 1,
                shouldAllocateToItems: includeInCost,
                allocationMethod: CostAllocationMethod.ByAmount,
                note: note,
                saveChanges: saveChanges);
        }

        public async Task<Result<InvoiceTax>> AddCustomsDutyAsync(
            int invoiceId,
            decimal taxBaseAmount,
            decimal ratePercent,
            string? note = null,
            bool saveChanges = true)
        {
            return await AddTaxAsync(
                invoiceId: invoiceId,
                taxType: TaxType.CustomsDuty,
                taxName: "Gömrük rüsumu",
                calculationSource: TaxCalculationSource.Import,
                costTreatment: TaxCostTreatment.IncludedInCost,
                ratePercent: ratePercent,
                taxBaseAmount: taxBaseAmount,
                manualTaxAmount: null,
                isIncludedInCost: true,
                isRecoverable: false,
                currency: CurrencyType.AZN,
                exchangeRate: 1,
                shouldAllocateToItems: true,
                allocationMethod: CostAllocationMethod.ByAmount,
                note: note,
                saveChanges: saveChanges);
        }

        public async Task<Result<InvoiceTax>> AddExciseAsync(
            int invoiceId,
            decimal taxBaseAmount,
            decimal ratePercent,
            string? note = null,
            bool saveChanges = true)
        {
            return await AddTaxAsync(
                invoiceId: invoiceId,
                taxType: TaxType.Excise,
                taxName: "Aksiz",
                calculationSource: TaxCalculationSource.Import,
                costTreatment: TaxCostTreatment.IncludedInCost,
                ratePercent: ratePercent,
                taxBaseAmount: taxBaseAmount,
                manualTaxAmount: null,
                isIncludedInCost: true,
                isRecoverable: false,
                currency: CurrencyType.AZN,
                exchangeRate: 1,
                shouldAllocateToItems: true,
                allocationMethod: CostAllocationMethod.ByAmount,
                note: note,
                saveChanges: saveChanges);
        }

        public async Task<Result<InvoiceTax>> AddManualAmountTaxAsync(
            int invoiceId,
            TaxType taxType,
            string taxName,
            decimal taxAmount,
            bool isIncludedInCost,
            bool isRecoverable,
            TaxCalculationSource calculationSource = TaxCalculationSource.Manual,
            CostAllocationMethod allocationMethod = CostAllocationMethod.ByAmount,
            string? note = null,
            bool saveChanges = true)
        {
            return await AddTaxAsync(
                invoiceId: invoiceId,
                taxType: taxType,
                taxName: taxName,
                calculationSource: calculationSource,
                costTreatment: ResolveCostTreatment(isRecoverable, isIncludedInCost),
                ratePercent: 0,
                taxBaseAmount: 0,
                manualTaxAmount: taxAmount,
                isIncludedInCost: isIncludedInCost,
                isRecoverable: isRecoverable,
                currency: CurrencyType.AZN,
                exchangeRate: 1,
                shouldAllocateToItems: isIncludedInCost,
                allocationMethod: allocationMethod,
                note: note,
                saveChanges: saveChanges);
        }

        public async Task<Result<InvoiceTax>> UpdateAsync(
            int id,
            decimal ratePercent,
            decimal taxBaseAmount,
            bool isIncludedInCost,
            bool isRecoverable,
            bool shouldAllocateToItems,
            CostAllocationMethod allocationMethod,
            string? note = null,
            bool saveChanges = true)
        {
            try
            {
                if (id <= 0)
                    return Result<InvoiceTax>.Fail("Vergi qeydi düzgün seçilməyib.");

                if (ratePercent < 0 || ratePercent > 100)
                    return Result<InvoiceTax>.Fail("Vergi faizi 0-100 aralığında olmalıdır.");

                if (taxBaseAmount < 0)
                    return Result<InvoiceTax>.Fail("Vergi bazası mənfi ola bilməz.");

                var tax = await _context.InvoiceTaxes
                    .Include(x => x.Invoice)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (tax == null)
                    return Result<InvoiceTax>.Fail("Qaimə vergi qeydi tapılmadı.");

                if (tax.Invoice.Status != InvoiceStatus.Draft)
                    return Result<InvoiceTax>.Fail("Yalnız draft qaimənin vergisi dəyişdirilə bilər.");

                if (tax.Invoice.IsLocked)
                    return Result<InvoiceTax>.Fail("Qaimə kilidlidir. Vergi dəyişdirilə bilməz.");

                tax.RatePercent = ratePercent;
                tax.TaxBaseAmount = taxBaseAmount;
                tax.TaxAmount = Math.Round(taxBaseAmount * ratePercent / 100m, 2);
                tax.ExchangeRate = NormalizeExchangeRate(tax.ExchangeRate);
                tax.LocalTaxAmount = Math.Round(tax.TaxAmount * tax.ExchangeRate, 2);

                tax.IsIncludedInCost = isIncludedInCost;
                tax.IsRecoverable = isRecoverable;
                tax.CostTreatment = ResolveCostTreatment(isRecoverable, isIncludedInCost);

                tax.ShouldAllocateToItems = shouldAllocateToItems;
                tax.AllocationMethod = allocationMethod;

                tax.Note = NormalizeNullableText(note);
                tax.UpdatedAt = DateTime.Now;

                await RecalculateTaxTotalsForInvoiceInternalAsync(tax.Invoice, saveChanges: false);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<InvoiceTax>.Success(tax, "Qaimə vergi qeydi yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceTax>.Fail($"Qaimə vergi qeydi yenilənmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateAsync(int id, bool saveChanges = true)
        {
            try
            {
                if (id <= 0)
                    return Result<bool>.Fail("Vergi qeydi düzgün seçilməyib.");

                var tax = await _context.InvoiceTaxes
                    .Include(x => x.Invoice)
                    .Include(x => x.Allocations.Where(a => a.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (tax == null)
                    return Result<bool>.Fail("Qaimə vergi qeydi tapılmadı.");

                if (tax.Invoice.Status != InvoiceStatus.Draft)
                    return Result<bool>.Fail("Yalnız draft qaimənin vergisi silinə bilər.");

                if (tax.Invoice.IsLocked)
                    return Result<bool>.Fail("Qaimə kilidlidir. Vergi silinə bilməz.");

                tax.IsActive = false;
                tax.UpdatedAt = DateTime.Now;

                foreach (var allocation in tax.Allocations.Where(x => x.IsActive))
                {
                    allocation.IsActive = false;
                    allocation.UpdatedAt = DateTime.Now;
                }

                await RecalculateTaxTotalsForInvoiceInternalAsync(tax.Invoice, saveChanges: false);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Qaimə vergi qeydi passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Qaimə vergi qeydi passiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> RemoveTaxAsync(int id, bool saveChanges = true)
        {
            return await DeactivateAsync(id, saveChanges);
        }

        public async Task<Result<bool>> ClearInvoiceTaxesAsync(int invoiceId, bool saveChanges = true)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<bool>.Fail("Qaimə düzgün seçilməyib.");

                var invoice = await _context.Invoices
                    .Include(x => x.Taxes.Where(t => t.IsActive))
                    .ThenInclude(x => x.Allocations.Where(a => a.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<bool>.Fail("Yalnız draft qaimənin vergiləri təmizlənə bilər.");

                if (invoice.IsLocked)
                    return Result<bool>.Fail("Qaimə kilidlidir. Vergilər təmizlənə bilməz.");

                foreach (var tax in invoice.Taxes.Where(x => x.IsActive))
                {
                    tax.IsActive = false;
                    tax.UpdatedAt = DateTime.Now;

                    foreach (var allocation in tax.Allocations.Where(x => x.IsActive))
                    {
                        allocation.IsActive = false;
                        allocation.UpdatedAt = DateTime.Now;
                    }
                }

                await RecalculateTaxTotalsForInvoiceInternalAsync(invoice, saveChanges: false);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Qaimənin vergi qeydləri təmizləndi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Qaimənin vergi qeydləri təmizlənmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> RecalculateTaxTotalsForInvoiceAsync(int invoiceId, bool saveChanges = true)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(x => x.Taxes.Where(t => t.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə tapılmadı.");

                await RecalculateTaxTotalsForInvoiceInternalAsync(invoice, saveChanges: false);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Qaimə vergi yekunları yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Qaimə vergi yekunları yenilənmədi: {ex.Message}");
            }
        }

        private static Task RecalculateTaxTotalsForInvoiceInternalAsync(Invoice invoice, bool saveChanges = false)
        {
            var activeTaxes = invoice.Taxes
                .Where(x => x.IsActive)
                .ToList();

            invoice.CostIncludedTaxAmount = activeTaxes
                .Where(x => x.IsIncludedInCost || x.CostTreatment == TaxCostTreatment.IncludedInCost)
                .Sum(x => x.LocalTaxAmount > 0 ? x.LocalTaxAmount : x.TaxAmount);

            invoice.RecoverableTaxAmount = activeTaxes
                .Where(x => x.IsRecoverable || x.CostTreatment == TaxCostTreatment.Recoverable)
                .Sum(x => x.LocalTaxAmount > 0 ? x.LocalTaxAmount : x.TaxAmount);

            invoice.CostExcludedTaxAmount = activeTaxes
                .Where(x =>
                    !x.IsIncludedInCost &&
                    !x.IsRecoverable &&
                    x.CostTreatment == TaxCostTreatment.ExcludedFromCost)
                .Sum(x => x.LocalTaxAmount > 0 ? x.LocalTaxAmount : x.TaxAmount);

            invoice.UpdatedAt = DateTime.Now;

            return Task.CompletedTask;
        }

        private static TaxCostTreatment ResolveCostTreatment(bool isRecoverable, bool isIncludedInCost)
        {
            if (isRecoverable)
                return TaxCostTreatment.Recoverable;

            if (isIncludedInCost)
                return TaxCostTreatment.IncludedInCost;

            return TaxCostTreatment.ExcludedFromCost;
        }

        private static decimal NormalizeExchangeRate(decimal exchangeRate)
        {
            return exchangeRate <= 0 ? 1 : exchangeRate;
        }

        private static string GetDefaultTaxName(TaxType taxType)
        {
            return taxType switch
            {
                TaxType.VAT => "ƏDV",
                TaxType.ImportVAT => "İdxal ƏDV",
                TaxType.CustomsDuty => "Gömrük rüsumu",
                TaxType.Excise => "Aksiz",
                TaxType.ProfitTax => "Mənfəət vergisi",
                TaxType.SimplifiedTax => "Sadələşdirilmiş vergi",
                TaxType.Other => "Digər vergi",
                _ => "Vergi"
            };
        }

        private static string? NormalizeNullableText(string? value)
        {
            value = value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}