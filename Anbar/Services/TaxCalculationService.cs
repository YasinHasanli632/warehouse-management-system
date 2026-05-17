using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    public class TaxCalculationService
    {
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;

        public TaxCalculationService(AppDbContext context)
        {
            _context = context;
            _settingsService = new SettingsService(context);
        }

        public TaxCalculationService(AppDbContext context, SettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<Result<bool>> RebuildInvoiceItemTaxesAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.ProductTaxes.Where(t => t.IsActive))
                            .ThenInclude(x => x.Tax)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (!invoice.Items.Any())
                return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

            await ClearInvoiceItemTaxesAsync(invoiceId);

            foreach (var item in invoice.Items.Where(x => x.IsActive))
            {
                CalculateBaseAmounts(invoice, item);

                var itemTaxes = await CalculateItemTaxesAsync(invoice, item);

                if (!itemTaxes.IsSuccess)
                    return Result<bool>.Fail(itemTaxes.Message);

                if (itemTaxes.Data != null && itemTaxes.Data.Any())
                    await _context.InvoiceItemTaxes.AddRangeAsync(itemTaxes.Data);
            }

            await _context.SaveChangesAsync();

            var summaryResult = await RecalculateInvoiceTaxSummaryAsync(invoiceId);

            if (!summaryResult.IsSuccess)
                return Result<bool>.Fail(summaryResult.Message);

            return Result<bool>.Success(true, "Qaimə item vergiləri yenidən hesablandı.");
        }

        public async Task<Result<List<InvoiceItemTax>>> CalculateItemTaxesAsync(Invoice invoice, InvoiceItem item)
        {
            var result = new List<InvoiceItemTax>();

            if (item.Product == null)
                return Result<List<InvoiceItemTax>>.Fail("Məhsul məlumatı yüklənməyib.");

            var taxSetting = await _context.TaxSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            var productTaxes = item.Product.ProductTaxes
                .Where(x =>
                    x.IsActive &&
                    x.IsApplicable &&
                    (!x.ValidFrom.HasValue || x.ValidFrom.Value.Date <= invoice.InvoiceDate.Date) &&
                    (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= invoice.InvoiceDate.Date))
                .ToList();

            if (productTaxes.Any())
            {
                foreach (var productTax in productTaxes)
                {
                    var tax = BuildInvoiceItemTaxFromProductTax(invoice, item, productTax);

                    if (tax.TaxAmount > 0 || tax.LocalTaxAmount > 0)
                        result.Add(tax);
                }

                return Result<List<InvoiceItemTax>>.Success(result);
            }

            // YENI:
            // ProductTax yoxdursa və qaimə yerli alışdırsa, LocalPurchaseSetting fallback kimi işləyir.
            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var localVatEnabled = await _settingsService.GetLocalPurchaseBoolAsync(
                    "EnableVATCalculation",
                    defaultValue: false);

                if (!localVatEnabled.IsSuccess)
                    return Result<List<InvoiceItemTax>>.Fail(localVatEnabled.Message);

                if (localVatEnabled.Data)
                {
                    var vatPercentResult = await _settingsService.GetLocalPurchaseDecimalAsync(
                        "VATPercent",
                        defaultValue: 18);

                    if (!vatPercentResult.IsSuccess)
                        return Result<List<InvoiceItemTax>>.Fail(vatPercentResult.Message);

                    var includedInPriceResult = await _settingsService.GetLocalPurchaseBoolAsync(
                        "PurchaseVATIncludedInPrice",
                        defaultValue: false);

                    if (!includedInPriceResult.IsSuccess)
                        return Result<List<InvoiceItemTax>>.Fail(includedInPriceResult.Message);

                    var includedInCostResult = await _settingsService.GetLocalPurchaseBoolAsync(
                        "PurchaseVATIncludedInCost",
                        defaultValue: false);

                    if (!includedInCostResult.IsSuccess)
                        return Result<List<InvoiceItemTax>>.Fail(includedInCostResult.Message);

                    var vatTax = await _context.Taxes
                        .FirstOrDefaultAsync(x => x.IsActive && x.TaxType == TaxType.VAT);

                    var fallbackTax = BuildLocalPurchaseVatTax(
                        invoice,
                        item,
                        vatPercentResult.Data,
                        includedInPriceResult.Data,
                        includedInCostResult.Data,
                        vatTax);

                    if (fallbackTax.TaxAmount > 0 || fallbackTax.LocalTaxAmount > 0)
                        result.Add(fallbackTax);

                    return Result<List<InvoiceItemTax>>.Success(result);
                }
            }

            // KOHNE FALLBACK:
            // ProductTax yoxdursa, Product field-lərindən ƏDV hesablayırıq.
            if (item.Product.IsVatApplicable && (taxSetting == null || taxSetting.EnableVAT))
            {
                var vatRate = item.Product.VatRate > 0
                    ? item.Product.VatRate
                    : taxSetting?.VATPercent ?? 18;

                var vatTax = await _context.Taxes
                    .FirstOrDefaultAsync(x => x.IsActive && x.TaxType == TaxType.VAT);

                var fallbackTax = BuildFallbackVatTax(invoice, item, vatRate, vatTax, taxSetting);

                if (fallbackTax.TaxAmount > 0 || fallbackTax.LocalTaxAmount > 0)
                    result.Add(fallbackTax);
            }

            return Result<List<InvoiceItemTax>>.Success(result);
        }

        public async Task<Result<bool>> ClearInvoiceItemTaxesAsync(int invoiceId)
        {
            var oldTaxes = await _context.InvoiceItemTaxes
                .Where(x => x.InvoiceId == invoiceId && x.IsActive)
                .ToListAsync();

            foreach (var tax in oldTaxes)
            {
                tax.IsActive = false;
                tax.UpdatedAt = DateTime.Now;
            }

            return Result<bool>.Success(true, "Köhnə item vergi snapshot-ları passiv edildi.");
        }

        public async Task<Result<bool>> RecalculateInvoiceTaxSummaryAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Taxes.Where(t => t.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            RecalculateInvoiceTaxSummary(invoice);

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə vergi summary yeniləndi.");
        }

        public async Task<Result<bool>> CalculateImportTaxesAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (!invoice.IsImport)
                return Result<bool>.Success(true, "Qaimə idxal qaiməsi deyil, idxal vergisi hesablanmadı.");

            return Result<bool>.Success(true, "İdxal vergiləri InvoiceTax üzərindən idarə olunacaq.");
        }

        private static void CalculateBaseAmounts(Invoice invoice, InvoiceItem item)
        {
            var exchangeRate = invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;

            if (item.ExchangeRate <= 0)
                item.ExchangeRate = exchangeRate;

            if (item.Currency == 0)
                item.Currency = invoice.Currency;

            if (item.OriginalUnitPrice <= 0)
                item.OriginalUnitPrice = item.Price;

            item.OriginalTotalAmount = item.OriginalUnitPrice * item.Quantity;

            item.LocalUnitPrice = item.Currency == CurrencyType.AZN
                ? item.OriginalUnitPrice
                : item.OriginalUnitPrice * item.ExchangeRate;

            item.LocalTotalAmount = item.LocalUnitPrice * item.Quantity;

            item.Price = item.OriginalUnitPrice;
            item.Total = item.OriginalTotalAmount;
        }

        private static InvoiceItemTax BuildInvoiceItemTaxFromProductTax(
            Invoice invoice,
            InvoiceItem item,
            ProductTax productTax)
        {
            var rate = productTax.RatePercent;

            var amounts = CalculateTaxAmounts(
                item.LocalTotalAmount,
                rate,
                productTax.IsIncludedInPrice);

            var originalBase = item.Currency == CurrencyType.AZN
                ? amounts.BaseAmount
                : amounts.BaseAmount / item.ExchangeRate;

            var originalTax = item.Currency == CurrencyType.AZN
                ? amounts.TaxAmount
                : amounts.TaxAmount / item.ExchangeRate;

            ApplyItemVatCompatibilityFields(
                item,
                productTax.TaxType,
                rate,
                productTax.IsIncludedInPrice,
                productTax.IsIncludedInCost,
                amounts);

            return new InvoiceItemTax
            {
                InvoiceId = invoice.Id,
                InvoiceItemId = item.Id,
                ProductId = item.ProductId,
                TaxId = productTax.TaxId,
                TaxType = productTax.TaxType,
                TaxName = productTax.Tax?.Name ?? productTax.TaxType.ToString(),
                CalculationSource = TaxCalculationSource.Product,
                CostTreatment = productTax.CostTreatment,
                RatePercent = rate,
                TaxBaseAmount = Math.Round(originalBase, 2),
                TaxAmount = Math.Round(originalTax, 2),
                Currency = item.Currency,
                ExchangeRate = item.ExchangeRate <= 0 ? 1 : item.ExchangeRate,
                LocalTaxBaseAmount = Math.Round(amounts.BaseAmount, 2),
                LocalTaxAmount = Math.Round(amounts.TaxAmount, 2),
                IsRecoverable = productTax.IsRecoverable,
                IsIncludedInCost = productTax.IsIncludedInCost,
                IsIncludedInPrice = productTax.IsIncludedInPrice,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private static InvoiceItemTax BuildLocalPurchaseVatTax(
            Invoice invoice,
            InvoiceItem item,
            decimal vatRate,
            bool includedInPrice,
            bool includedInCost,
            Tax? vatTax)
        {
            var recoverable = !includedInCost;

            var costTreatment = includedInCost
                ? TaxCostTreatment.IncludedInCost
                : TaxCostTreatment.Recoverable;

            var amounts = CalculateTaxAmounts(
                item.LocalTotalAmount,
                vatRate,
                includedInPrice);

            var originalBase = item.Currency == CurrencyType.AZN
                ? amounts.BaseAmount
                : amounts.BaseAmount / item.ExchangeRate;

            var originalTax = item.Currency == CurrencyType.AZN
                ? amounts.TaxAmount
                : amounts.TaxAmount / item.ExchangeRate;

            ApplyItemVatCompatibilityFields(
                item,
                TaxType.VAT,
                vatRate,
                includedInPrice,
                includedInCost,
                amounts);

            return new InvoiceItemTax
            {
                InvoiceId = invoice.Id,
                InvoiceItemId = item.Id,
                ProductId = item.ProductId,
                TaxId = vatTax?.Id,
                TaxType = TaxType.VAT,
                TaxName = vatTax?.Name ?? "ƏDV",
                CalculationSource = TaxCalculationSource.Invoice,
                CostTreatment = costTreatment,
                RatePercent = vatRate,
                TaxBaseAmount = Math.Round(originalBase, 2),
                TaxAmount = Math.Round(originalTax, 2),
                Currency = item.Currency,
                ExchangeRate = item.ExchangeRate <= 0 ? 1 : item.ExchangeRate,
                LocalTaxBaseAmount = Math.Round(amounts.BaseAmount, 2),
                LocalTaxAmount = Math.Round(amounts.TaxAmount, 2),
                IsRecoverable = recoverable,
                IsIncludedInCost = includedInCost,
                IsIncludedInPrice = includedInPrice,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private static InvoiceItemTax BuildFallbackVatTax(
            Invoice invoice,
            InvoiceItem item,
            decimal vatRate,
            Tax? vatTax,
            TaxSetting? taxSetting)
        {
            var includedInPrice = item.Product.IsPurchasePriceVatIncluded;
            var recoverable = item.Product.IsVatRecoverable || (taxSetting?.VATRecoverableByDefault ?? false);
            var includedInCost = !recoverable;

            var costTreatment = recoverable
                ? TaxCostTreatment.Recoverable
                : TaxCostTreatment.IncludedInCost;

            var amounts = CalculateTaxAmounts(
                item.LocalTotalAmount,
                vatRate,
                includedInPrice);

            var originalBase = item.Currency == CurrencyType.AZN
                ? amounts.BaseAmount
                : amounts.BaseAmount / item.ExchangeRate;

            var originalTax = item.Currency == CurrencyType.AZN
                ? amounts.TaxAmount
                : amounts.TaxAmount / item.ExchangeRate;

            ApplyItemVatCompatibilityFields(item, TaxType.VAT, vatRate, includedInPrice, includedInCost, amounts);

            return new InvoiceItemTax
            {
                InvoiceId = invoice.Id,
                InvoiceItemId = item.Id,
                ProductId = item.ProductId,
                TaxId = vatTax?.Id,
                TaxType = TaxType.VAT,
                TaxName = vatTax?.Name ?? "ƏDV",
                CalculationSource = TaxCalculationSource.Product,
                CostTreatment = costTreatment,
                RatePercent = vatRate,
                TaxBaseAmount = Math.Round(originalBase, 2),
                TaxAmount = Math.Round(originalTax, 2),
                Currency = item.Currency,
                ExchangeRate = item.ExchangeRate <= 0 ? 1 : item.ExchangeRate,
                LocalTaxBaseAmount = Math.Round(amounts.BaseAmount, 2),
                LocalTaxAmount = Math.Round(amounts.TaxAmount, 2),
                IsRecoverable = recoverable,
                IsIncludedInCost = includedInCost,
                IsIncludedInPrice = includedInPrice,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private static (decimal BaseAmount, decimal TaxAmount, decimal GrossAmount) CalculateTaxAmounts(
            decimal localAmount,
            decimal ratePercent,
            bool taxIncludedInPrice)
        {
            if (localAmount <= 0 || ratePercent <= 0)
                return (localAmount, 0, localAmount);

            if (taxIncludedInPrice)
            {
                var baseAmount = localAmount / (1 + (ratePercent / 100));
                var taxAmount = localAmount - baseAmount;

                return (
                    Math.Round(baseAmount, 2),
                    Math.Round(taxAmount, 2),
                    Math.Round(localAmount, 2));
            }

            var tax = localAmount * ratePercent / 100;

            return (
                Math.Round(localAmount, 2),
                Math.Round(tax, 2),
                Math.Round(localAmount + tax, 2));
        }

        private static void ApplyItemVatCompatibilityFields(
            InvoiceItem item,
            TaxType taxType,
            decimal rate,
            bool includedInPrice,
            bool includedInCost,
            (decimal BaseAmount, decimal TaxAmount, decimal GrossAmount) amounts)
        {
            if (taxType != TaxType.VAT && taxType != TaxType.ImportVAT)
                return;

            item.IsVatApplicable = true;
            item.VatRate = rate;
            item.IsVatIncludedInPrice = includedInPrice;
            item.IsVatIncludedInCost = includedInCost;

            item.NetAmount = amounts.BaseAmount;
            item.VatAmount = amounts.TaxAmount;
            item.GrossAmount = amounts.GrossAmount;
        }

        private static void RecalculateInvoiceTaxSummary(Invoice invoice)
        {
            var items = invoice.Items.Where(x => x.IsActive).ToList();

            invoice.LocalItemsTotalAmount = items.Sum(x => x.LocalTotalAmount);
            invoice.OriginalItemsTotalAmount = items.Sum(x => x.OriginalTotalAmount);
            invoice.ItemsTotalAmount = invoice.LocalItemsTotalAmount;

            invoice.NetItemsAmount = items.Sum(x => x.NetAmount > 0 ? x.NetAmount : x.LocalTotalAmount);
            invoice.NetAmount = invoice.NetItemsAmount;

            invoice.VatAmount = items.Sum(x => x.VatAmount);

            invoice.GrossItemsAmount = items.Sum(x => x.GrossAmount > 0 ? x.GrossAmount : x.LocalTotalAmount);
            invoice.GrossAmount = invoice.GrossItemsAmount;

            var itemTaxes = items.SelectMany(x => x.Taxes.Where(t => t.IsActive)).ToList();

            invoice.RecoverableTaxAmount = itemTaxes
                .Where(x => x.IsRecoverable || x.CostTreatment == TaxCostTreatment.Recoverable)
                .Sum(x => x.LocalTaxAmount);

            invoice.CostIncludedTaxAmount = itemTaxes
                .Where(x => x.IsIncludedInCost || x.CostTreatment == TaxCostTreatment.IncludedInCost)
                .Sum(x => x.LocalTaxAmount);

            invoice.CostExcludedTaxAmount = itemTaxes
                .Where(x =>
                    !x.IsIncludedInCost &&
                    !x.IsRecoverable &&
                    x.CostTreatment == TaxCostTreatment.ExcludedFromCost)
                .Sum(x => x.LocalTaxAmount);

            invoice.TotalAmount = invoice.GrossAmount + invoice.ExtraExpenseAmount - invoice.DiscountAmount;
            invoice.LocalTotalAmount = invoice.TotalAmount;
            invoice.OriginalTotalAmount = invoice.Currency == CurrencyType.AZN
                ? invoice.LocalTotalAmount
                : invoice.LocalTotalAmount / (invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate);

            invoice.UpdatedAt = DateTime.Now;
        }
    }
}