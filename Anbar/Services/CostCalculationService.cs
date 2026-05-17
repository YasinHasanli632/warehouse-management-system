using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    public class CostCalculationService
    {
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;

        public CostCalculationService(AppDbContext context)
        {
            _context = context;
            _settingsService = new SettingsService(context);
        }

        public CostCalculationService(AppDbContext context, SettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<Result<bool>> RecalculateInvoiceCostAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.ExpenseAllocations.Where(a => a.IsActive))
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.TaxAllocations.Where(a => a.IsActive))
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Taxes.Where(t => t.IsActive))
                .Include(x => x.Expenses.Where(e => e.IsActive))
                .Include(x => x.Taxes.Where(t => t.IsActive))
                .Include(x => x.CostSummary)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (!invoice.Items.Any())
                return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var autoCost = await _settingsService.GetLocalPurchaseBoolAsync(
                    "AutoCalculateCostOnConfirm",
                    defaultValue: true);

                if (!autoCost.IsSuccess)
                    return Result<bool>.Fail(autoCost.Message);

                if (!autoCost.Data)
                    return Result<bool>.Fail("Yerli alış üçün avtomatik maya hesablaması deaktivdir.");
            }

            var includeLocalVatInCost = await ShouldIncludeLocalPurchaseVatInCostAsync(invoice);

            foreach (var item in invoice.Items.Where(x => x.IsActive))
                CalculateItemFinalCost(item, includeLocalVatInCost);

            UpdateInvoiceCostSummary(invoice);

            invoice.CostStatus = CostRecalculationStatus.Calculated;
            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə maya dəyəri hesablandı.");
        }

        public Result<decimal> CalculateItemFinalCostAsync(InvoiceItem item)
        {
            if (item == null)
                return Result<decimal>.Fail("Qaimə item məlumatı boşdur.");

            CalculateItemFinalCost(item, includeLocalVatInCost: true);

            return Result<decimal>.Success(item.FinalUnitCost);
        }

        public async Task<Result<bool>> MarkNeedsRecalculationAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            invoice.CostStatus = CostRecalculationStatus.NeedsRecalculation;
            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə maya dəyəri yenidən hesablanmalıdır kimi işarələndi.");
        }

        public async Task<Result<bool>> LockCostAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            invoice.CostStatus = CostRecalculationStatus.Locked;
            invoice.IsLocked = true;
            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə maya dəyəri kilidləndi.");
        }

        private async Task<bool> ShouldIncludeLocalPurchaseVatInCostAsync(Invoice invoice)
        {
            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var result = await _settingsService.GetLocalPurchaseBoolAsync(
                    "PurchaseVATIncludedInCost",
                    defaultValue: false);

                if (result.IsSuccess)
                    return result.Data;
            }

            return true;
        }

        private static void CalculateItemFinalCost(
            InvoiceItem item,
            bool includeLocalVatInCost)
        {
            if (item.Quantity <= 0)
            {
                item.FinalUnitCost = 0;
                item.FinalTotalCost = 0;
                item.DiscountUnitShare = 0;
                return;
            }

            var exchangeRate = item.ExchangeRate <= 0 ? 1 : item.ExchangeRate;

            var baseUnitCost = item.LocalUnitPrice > 0
                ? item.LocalUnitPrice
                : item.Price * exchangeRate;

            var itemTaxUnitShare = item.Taxes
                .Where(x =>
                    x.IsActive &&
                    (x.IsIncludedInCost || x.CostTreatment == TaxCostTreatment.IncludedInCost))
                .Where(x =>
                    includeLocalVatInCost ||
                    x.TaxType != TaxType.VAT)
                .Sum(x => item.Quantity > 0 ? x.LocalTaxAmount / item.Quantity : 0);

            var allocatedTaxUnitShare = item.TaxAllocations
                .Where(x => x.IsActive)
                .Sum(x => x.UnitAllocatedAmount);

            var expenseUnitShare = item.ExpenseAllocations
                .Where(x => x.IsActive)
                .Sum(x => x.UnitAllocatedAmount);

            var discountUnitShare = item.DiscountUnitShare > 0
                ? item.DiscountUnitShare
                : item.DiscountAmount > 0
                    ? item.DiscountAmount / item.Quantity
                    : 0;

            item.TaxUnitShare = Math.Round(itemTaxUnitShare + allocatedTaxUnitShare, 4);
            item.ExpenseUnitShare = Math.Round(expenseUnitShare, 4);
            item.DiscountUnitShare = Math.Round(discountUnitShare, 4);

            var finalUnitCost =
                baseUnitCost
                + item.ExpenseUnitShare
                + item.TaxUnitShare
                - item.DiscountUnitShare;

            if (finalUnitCost < 0)
                finalUnitCost = 0;

            item.FinalUnitCost = Math.Round(finalUnitCost, 4);
            item.FinalTotalCost = Math.Round(item.FinalUnitCost * item.Quantity, 2);
            item.UpdatedAt = DateTime.Now;
        }

        private void UpdateInvoiceCostSummary(Invoice invoice)
        {
            var items = invoice.Items.Where(x => x.IsActive).ToList();

            var itemDiscountAmount = items.Sum(x => x.DiscountAmount);

            var expenseDiscountAmount = invoice.Expenses
                .Where(x => x.IsActive && x.Direction == ExpenseDirection.Minus)
                .Sum(x => x.LocalAmount > 0 ? x.LocalAmount : x.Amount);

            invoice.DiscountAmount = itemDiscountAmount + expenseDiscountAmount;

            invoice.FinalCostAmount = items.Sum(x => x.FinalTotalCost);
            invoice.CostIncludedExpenseAmount = items.Sum(x => x.ExpenseUnitShare * x.Quantity);
            invoice.CostIncludedTaxAmount = items.Sum(x => x.TaxUnitShare * x.Quantity);

            invoice.LocalItemsTotalAmount = items.Sum(x => x.LocalTotalAmount);
            invoice.OriginalItemsTotalAmount = items.Sum(x => x.OriginalTotalAmount);
            invoice.ItemsTotalAmount = invoice.LocalItemsTotalAmount;

            invoice.NetItemsAmount = items.Sum(x => x.NetAmount > 0 ? x.NetAmount : x.LocalTotalAmount);
            invoice.NetAmount = invoice.NetItemsAmount;

            invoice.VatAmount = items.Sum(x => x.VatAmount);

            invoice.GrossItemsAmount = items.Sum(x => x.GrossAmount > 0 ? x.GrossAmount : x.LocalTotalAmount);
            invoice.GrossAmount = invoice.GrossItemsAmount;

            invoice.RecoverableTaxAmount = items
                .SelectMany(x => x.Taxes.Where(t => t.IsActive))
                .Where(x => x.IsRecoverable || x.CostTreatment == TaxCostTreatment.Recoverable)
                .Sum(x => x.LocalTaxAmount);

            invoice.CostExcludedTaxAmount = items
                .SelectMany(x => x.Taxes.Where(t => t.IsActive))
                .Where(x =>
                    !x.IsIncludedInCost &&
                    !x.IsRecoverable &&
                    x.CostTreatment == TaxCostTreatment.ExcludedFromCost)
                .Sum(x => x.LocalTaxAmount);

            invoice.TotalAmount =
                invoice.GrossAmount
                + invoice.ExtraExpenseAmount
                - expenseDiscountAmount;

            if (invoice.TotalAmount < 0)
                invoice.TotalAmount = 0;

            invoice.LocalTotalAmount = invoice.TotalAmount;

            invoice.OriginalTotalAmount = invoice.Currency == CurrencyType.AZN
                ? invoice.LocalTotalAmount
                : invoice.LocalTotalAmount / (invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate);

            invoice.DebtAmount = invoice.PaidAmount > 0
                ? Math.Max(0, invoice.TotalAmount - invoice.PaidAmount)
                : invoice.TotalAmount;

            if (invoice.CostSummary == null)
            {
                invoice.CostSummary = new InvoiceCostSummary
                {
                    InvoiceId = invoice.Id,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.InvoiceCostSummaries.Add(invoice.CostSummary);
            }

            invoice.CostSummary.BaseItemsAmount = invoice.LocalItemsTotalAmount;
            invoice.CostSummary.NetItemsAmount = invoice.NetItemsAmount;
            invoice.CostSummary.VatAmount = invoice.VatAmount;
            invoice.CostSummary.GrossItemsAmount = invoice.GrossAmount;
            invoice.CostSummary.CostIncludedExpenseAmount = invoice.CostIncludedExpenseAmount;

            invoice.CostSummary.CostExcludedExpenseAmount =
                Math.Max(0, invoice.ExtraExpenseAmount - invoice.CostIncludedExpenseAmount);

            invoice.CostSummary.CostIncludedTaxAmount = invoice.CostIncludedTaxAmount;
            invoice.CostSummary.RecoverableTaxAmount = invoice.RecoverableTaxAmount;
            invoice.CostSummary.CostExcludedTaxAmount = invoice.CostExcludedTaxAmount;
            invoice.CostSummary.DiscountAmount = invoice.DiscountAmount;
            invoice.CostSummary.FinalCostAmount = invoice.FinalCostAmount;
            invoice.CostSummary.CalculatedAt = DateTime.Now;
            invoice.CostSummary.UpdatedAt = DateTime.Now;
        }
    }
}