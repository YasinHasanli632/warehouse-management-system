using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // YENI:
    // Qaimə başlığı/import/manual vergilərini item-lərə paylayan servisdir.
    // InvoiceTax -> InvoiceTaxAllocation burada yaranır.
    // CostTreatment=IncludedInCost olan vergilər item.TaxUnitShare dəyərinə düşür.
    public class TaxAllocationService
    {
        private readonly AppDbContext _context;

        public TaxAllocationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> RebuildTaxAllocationsAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                .Include(x => x.Taxes.Where(t => t.IsActive))
    .ThenInclude(x => x.Allocations.Where(a => a.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (!invoice.Items.Any())
                return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

            await ClearTaxAllocationsAsync(invoiceId);

            foreach (var item in invoice.Items.Where(x => x.IsActive))
                item.TaxUnitShare = 0;

            var taxes = invoice.Taxes
                .Where(x => x.IsActive && x.ShouldAllocateToItems)
                .ToList();

            foreach (var tax in taxes)
            {
                var allocations = AllocateInvoiceTax(invoice, tax);

                if (allocations.Any())
                    await _context.InvoiceTaxAllocations.AddRangeAsync(allocations);

                if (tax.IsIncludedInCost || tax.CostTreatment == TaxCostTreatment.IncludedInCost)
                {
                    foreach (var allocation in allocations)
                    {
                        var item = invoice.Items.FirstOrDefault(x => x.Id == allocation.InvoiceItemId);

                        if (item != null)
                            item.TaxUnitShare += allocation.UnitAllocatedAmount;
                    }
                }
            }

            invoice.CostIncludedTaxAmount = invoice.Items
                .Where(x => x.IsActive)
                .Sum(x => x.TaxUnitShare * x.Quantity);

            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə vergiləri item-lərə paylandı.");
        }

        public async Task<Result<bool>> ClearTaxAllocationsAsync(int invoiceId)
        {
            var allocations = await _context.InvoiceTaxAllocations
                .Include(x => x.InvoiceTax)
                .Where(x =>
                    x.InvoiceTax.InvoiceId == invoiceId &&
                    x.IsActive)
                .ToListAsync();

            if (allocations.Any())
            {
                foreach (var allocation in allocations)
                {
                    // YENI:
                    // Hard delete yox, soft delete.
                    // Audit üçün əvvəlki paylama tarixçəsi qalır.
                    allocation.IsActive = false;
                    allocation.UpdatedAt = DateTime.Now;
                }
            }

            return Result<bool>.Success(true, "Köhnə vergi paylamaları passiv edildi.");
        }

        private static List<InvoiceTaxAllocation> AllocateInvoiceTax(Invoice invoice, InvoiceTax tax)
        {
            var items = invoice.Items
                .Where(x => x.IsActive && x.Quantity > 0)
                .ToList();

            var amount = tax.LocalTaxAmount > 0
                ? tax.LocalTaxAmount
                : tax.TaxAmount * (tax.ExchangeRate <= 0 ? 1 : tax.ExchangeRate);

            if (amount <= 0 || !items.Any())
                return new List<InvoiceTaxAllocation>();

            var bases = BuildAllocationBases(items, tax.AllocationMethod);
            var totalBase = bases.Sum(x => x.Value);

            if (totalBase <= 0)
                bases = items.ToDictionary(x => x.Id, x => x.LocalTotalAmount > 0 ? x.LocalTotalAmount : x.Quantity);

            totalBase = bases.Sum(x => x.Value);

            if (totalBase <= 0)
                return new List<InvoiceTaxAllocation>();

            var result = new List<InvoiceTaxAllocation>();
            decimal allocatedTotal = 0;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isLast = i == items.Count - 1;

                var allocated = isLast
                    ? amount - allocatedTotal
                    : Math.Round(amount * bases[item.Id] / totalBase, 2);

                allocatedTotal += allocated;

                result.Add(new InvoiceTaxAllocation
                {
                    InvoiceTaxId = tax.Id,
                    InvoiceItemId = item.Id,
                    ProductId = item.ProductId,
                    AllocatedAmount = allocated,
                    UnitAllocatedAmount = item.Quantity > 0
                        ? Math.Round(allocated / item.Quantity, 4)
                        : 0,
                    IsActive = true
                });
            }

            return result;
        }

        private static Dictionary<int, decimal> BuildAllocationBases(List<InvoiceItem> items, CostAllocationMethod method)
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
    }
}
