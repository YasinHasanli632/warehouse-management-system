using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Qaimə xərclərini məhsul item-lərinə paylayan servisdir.
    // InvoiceExpense -> InvoiceExpenseAllocation burada yaranır.
    // AffectStockCost=true olan xərc item.ExpenseUnitShare dəyərinə düşür.
    public class ExpenseAllocationService
    {
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;

        public ExpenseAllocationService(AppDbContext context)
        {
            _context = context;
            _settingsService = new SettingsService(context);
        }

        public ExpenseAllocationService(AppDbContext context, SettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<Result<bool>> RebuildExpenseAllocationsAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                .Include(x => x.Expenses.Where(e => e.IsActive))
                    .ThenInclude(x => x.Allocations.Where(a => a.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (!invoice.Items.Any())
                return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

            await ClearExpenseAllocationsAsync(invoiceId);

            foreach (var item in invoice.Items.Where(x => x.IsActive))
                item.ExpenseUnitShare = 0;

            var ignoreZeroAmountExpenses = await ShouldIgnoreZeroAmountExpensesAsync(invoice);

            var costExpenses = invoice.Expenses
                .Where(x =>
                    x.IsActive &&
                    x.ShouldAllocateToItems &&
                    x.AffectStockCost &&
                    (!ignoreZeroAmountExpenses || x.LocalAmount != 0 || x.IncludeZeroAmountInCost))
                .ToList();

            foreach (var expense in costExpenses)
            {
                NormalizeExpenseAmount(expense);

                expense.AllocationMethod = await ResolveAllocationMethodAsync(invoice, expense);

                var allocations = AllocateExpense(invoice, expense);

                if (allocations.Any())
                    await _context.InvoiceExpenseAllocations.AddRangeAsync(allocations);

                foreach (var allocation in allocations)
                {
                    var item = invoice.Items.FirstOrDefault(x => x.Id == allocation.InvoiceItemId);

                    if (item != null)
                        item.ExpenseUnitShare += allocation.UnitAllocatedAmount;
                }
            }

            invoice.CostIncludedExpenseAmount = invoice.Items
                .Where(x => x.IsActive)
                .Sum(x => x.ExpenseUnitShare * x.Quantity);

            invoice.ExtraExpenseAmount = invoice.Expenses
                .Where(x => x.IsActive && x.Direction == ExpenseDirection.Plus)
                .Sum(x => GetSignedLocalAmount(x));

            invoice.DiscountAmount = Math.Abs(invoice.Expenses
                .Where(x => x.IsActive && x.Direction == ExpenseDirection.Minus)
                .Sum(x => GetSignedLocalAmount(x)));

            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə xərcləri item-lərə paylandı.");
        }

        public async Task<Result<bool>> ClearExpenseAllocationsAsync(int invoiceId)
        {
            var allocations = await _context.InvoiceExpenseAllocations
                .Include(x => x.InvoiceExpense)
                .Where(x =>
                    x.InvoiceExpense.InvoiceId == invoiceId &&
                    x.IsActive)
                .ToListAsync();

            foreach (var allocation in allocations)
            {
                allocation.IsActive = false;
                allocation.UpdatedAt = DateTime.Now;
            }

            return Result<bool>.Success(true, "Köhnə xərc paylamaları passiv edildi.");
        }

        private async Task<bool> ShouldIgnoreZeroAmountExpensesAsync(Invoice invoice)
        {
            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var result = await _settingsService.GetLocalPurchaseBoolAsync(
                    "IgnoreZeroAmountExpenses",
                    defaultValue: true);

                if (result.IsSuccess)
                    return result.Data;
            }

            var costSetting = await _context.CostSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            return costSetting?.ExcludeZeroAmountExpenses ?? true;
        }

        private async Task<CostAllocationMethod> ResolveAllocationMethodAsync(
            Invoice invoice,
            InvoiceExpense expense)
        {
            if (expense.AllocationMethod != 0)
                return expense.AllocationMethod;

            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var localMethod = await _settingsService.GetLocalPurchaseAllocationMethodAsync();

                if (localMethod.IsSuccess)
                    return localMethod.Data;
            }

            var costSetting = await _context.CostSettings
                .FirstOrDefaultAsync(x => x.IsActive);

            return costSetting?.DefaultAllocationMethod ?? CostAllocationMethod.ByAmount;
        }

        private static List<InvoiceExpenseAllocation> AllocateExpense(
            Invoice invoice,
            InvoiceExpense expense)
        {
            var items = invoice.Items
                .Where(x => x.IsActive && x.Quantity > 0)
                .ToList();

            var amount = GetSignedLocalAmount(expense);

            if (amount == 0 || !items.Any())
                return new List<InvoiceExpenseAllocation>();

            var bases = BuildAllocationBases(items, expense.AllocationMethod);
            var totalBase = bases.Sum(x => x.Value);

            if (totalBase <= 0)
                bases = items.ToDictionary(x => x.Id, x => x.LocalTotalAmount > 0 ? x.LocalTotalAmount : x.Quantity);

            totalBase = bases.Sum(x => x.Value);

            if (totalBase <= 0)
                return new List<InvoiceExpenseAllocation>();

            var result = new List<InvoiceExpenseAllocation>();
            decimal allocatedTotal = 0;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isLast = i == items.Count - 1;

                var allocated = isLast
                    ? amount - allocatedTotal
                    : Math.Round(amount * bases[item.Id] / totalBase, 2);

                allocatedTotal += allocated;

                result.Add(new InvoiceExpenseAllocation
                {
                    InvoiceExpenseId = expense.Id,
                    InvoiceItemId = item.Id,
                    ProductId = item.ProductId,
                    AllocationMethod = expense.AllocationMethod,
                    AllocationBaseValue = bases[item.Id],
                    AllocatedAmount = allocated,
                    UnitAllocatedAmount = item.Quantity > 0
                        ? Math.Round(allocated / item.Quantity, 4)
                        : 0,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                });
            }

            return result;
        }

        private static Dictionary<int, decimal> BuildAllocationBases(
            List<InvoiceItem> items,
            CostAllocationMethod method)
        {
            return method switch
            {
                CostAllocationMethod.ByQuantity => items.ToDictionary(x => x.Id, x => x.Quantity),
                CostAllocationMethod.ByWeight => items.ToDictionary(x => x.Id, x => (x.Product?.Weight ?? 0) * x.Quantity),
                CostAllocationMethod.ByVolume => items.ToDictionary(x => x.Id, x => (x.Product?.Volume ?? 0) * x.Quantity),
                CostAllocationMethod.ByAmount => items.ToDictionary(x => x.Id, x => x.LocalTotalAmount),
                CostAllocationMethod.Manual => items.ToDictionary(x => x.Id, x => x.LocalTotalAmount),
                _ => items.ToDictionary(x => x.Id, x => x.LocalTotalAmount)
            };
        }

        private static void NormalizeExpenseAmount(InvoiceExpense expense)
        {
            if (expense.ExchangeRate <= 0)
                expense.ExchangeRate = 1;

            if (expense.OriginalAmount <= 0 && expense.Amount > 0)
                expense.OriginalAmount = expense.Amount;

            expense.LocalAmount = expense.Currency == CurrencyType.AZN
                ? expense.OriginalAmount
                : expense.OriginalAmount * expense.ExchangeRate;

            if (expense.Amount <= 0)
                expense.Amount = expense.OriginalAmount;

            expense.UpdatedAt = DateTime.Now;
        }

        private static decimal GetSignedLocalAmount(InvoiceExpense expense)
        {
            NormalizeExpenseAmount(expense);

            return expense.Direction == ExpenseDirection.Minus
                ? -Math.Abs(expense.LocalAmount)
                : Math.Abs(expense.LocalAmount);
        }
    }
}