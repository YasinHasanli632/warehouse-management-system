using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // Bu servis anbar stok məntiqini idarə edir.
    // YENI ENTERPRISE:
    // StockService artıq cost hesablamır.
    // CostCalculationService tərəfindən hesablanmış InvoiceItem.FinalUnitCost dəyərini StockBatch və StockMovement snapshot kimi saxlayır.
    public class StockService
    {
        private readonly AppDbContext _context;

        public StockService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> ApplyStockInAsync(Invoice invoice)
        {
            if (invoice == null)
                return Result<bool>.Fail("Qaimə məlumatı boş ola bilməz.");

            if (invoice.Type != InvoiceType.StockIn)
                return Result<bool>.Fail("Bu qaimə giriş qaiməsi deyil.");

            var activeItems = invoice.Items?
                .Where(x => x.IsActive)
                .ToList();

            if (activeItems == null || !activeItems.Any())
                return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

            foreach (var item in activeItems)
            {
                if (item.ProductId <= 0)
                    return Result<bool>.Fail("Qaimədə məhsul düzgün seçilməyib.");

                if (item.ShelfId <= 0)
                    return Result<bool>.Fail("Qaimədə rəf düzgün seçilməyib.");

                if (item.Quantity <= 0)
                    return Result<bool>.Fail("Qaimədə miqdar 0-dan böyük olmalıdır.");

                if (item.FinalUnitCost <= 0)
                    return Result<bool>.Fail($"Məhsulun maya dəyəri hesablanmayıb. MəhsulId: {item.ProductId}");
            }

            var batchCapacityCheck = await HasEnoughShelfCapacityForItemsAsync(activeItems);

            if (!batchCapacityCheck.IsSuccess || !batchCapacityCheck.Data)
                return Result<bool>.Fail(batchCapacityCheck.Message);

            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync()
                : null;

            try
            {
                foreach (var item in activeItems)
                {
                    var increaseResult = await IncreaseShelfStockAsync(
                        item.ProductId,
                        item.ShelfId,
                        item.Quantity,
                        saveChanges: false);

                    if (!increaseResult.IsSuccess)
                        return Result<bool>.Fail(increaseResult.Message);

                    var finalUnitCost = GetFinalUnitCost(item);
                    var finalTotalCost = Math.Round(finalUnitCost * item.Quantity, 2);

                    var batch = new StockBatch
                    {
                        ProductId = item.ProductId,
                        ShelfId = item.ShelfId,
                        SourceInvoiceId = invoice.Id,
                        SourceInvoiceItemId = item.Id,

                        BatchNumber = $"BATCH-{invoice.Id}-{item.Id}-{DateTime.Now:yyyyMMddHHmmssfff}",

                        EntryDate = invoice.InvoiceDate,

                        Currency = item.Currency,
                        ExchangeRate = item.ExchangeRate <= 0 ? 1 : item.ExchangeRate,

                        PurchaseUnitPrice = item.OriginalUnitPrice,
                        LocalUnitPrice = item.LocalUnitPrice,

                        ExpenseUnitShare = item.ExpenseUnitShare,
                        TaxUnitShare = item.TaxUnitShare,
                        DiscountUnitShare = item.DiscountUnitShare,

                        FinalUnitCost = finalUnitCost,
                        FinalTotalCost = finalTotalCost,

                        PurchasePrice = item.Price,
                        InitialQuantity = item.Quantity,
                        RemainingQuantity = item.Quantity,

                        Note = $"Giriş qaiməsindən yaranan FIFO partiya. Qaimə №: {invoice.InvoiceNumber}",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.StockBatches.AddAsync(batch);

                    var movement = new StockMovement
                    {
                        ProductId = item.ProductId,
                        StockBatch = batch,
                        MovementType = StockMovementType.StockIn,
                        Quantity = item.Quantity,

                        UnitCost = finalUnitCost,
                        TotalCost = finalTotalCost,

                        FromShelfId = null,
                        ToShelfId = item.ShelfId,
                        InvoiceId = invoice.Id,
                        Note = $"Giriş qaiməsi ilə stok artırıldı. Qaimə №: {invoice.InvoiceNumber}",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.StockMovements.AddAsync(movement);
                }

                await _context.SaveChangesAsync();

                foreach (var shelfId in activeItems.Select(x => x.ShelfId).Distinct())
                {
                    var statusResult = await UpdateShelfStatusAsync(shelfId, saveChanges: false);

                    if (!statusResult.IsSuccess)
                        return Result<bool>.Fail(statusResult.Message);
                }

                await _context.SaveChangesAsync();

                if (transaction != null)
                    await transaction.CommitAsync();

                return Result<bool>.Success(true, "Giriş stoklara uğurla tətbiq edildi.");
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return Result<bool>.Fail($"Giriş stoklara tətbiq edilərkən xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ApplyStockOutAsync(Invoice invoice)
        {
            if (invoice == null)
                return Result<bool>.Fail("Qaimə məlumatı boş ola bilməz.");

            if (invoice.Type != InvoiceType.StockOut)
                return Result<bool>.Fail("Bu qaimə çıxış qaiməsi deyil.");

            var activeItems = invoice.Items?
                .Where(x => x.IsActive)
                .ToList();

            if (activeItems == null || !activeItems.Any())
                return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

            foreach (var item in activeItems)
            {
                if (item.ProductId <= 0)
                    return Result<bool>.Fail("Qaimədə məhsul düzgün seçilməyib.");

                if (item.ShelfId <= 0)
                    return Result<bool>.Fail("Qaimədə rəf düzgün seçilməyib.");

                if (item.Quantity <= 0)
                    return Result<bool>.Fail("Qaimədə miqdar 0-dan böyük olmalıdır.");

                var stockCheck = await HasEnoughStockAsync(item.ProductId, item.ShelfId, item.Quantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                    return Result<bool>.Fail(stockCheck.Message);

                var fifoCheck = await HasEnoughFifoBatchStockAsync(item.ProductId, item.ShelfId, item.Quantity);

                if (!fifoCheck.IsSuccess || !fifoCheck.Data)
                    return Result<bool>.Fail(fifoCheck.Message);
            }

            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync()
                : null;

            try
            {
                foreach (var item in activeItems)
                {
                    var fifoResult = await ApplyFifoOutAsync(
                        invoice: invoice,
                        productId: item.ProductId,
                        shelfId: item.ShelfId,
                        quantity: item.Quantity,
                        movementType: StockMovementType.StockOut,
                        notePrefix: "Çıxış qaiməsi ilə FIFO stok azaldıldı");

                    if (!fifoResult.IsSuccess)
                        return Result<bool>.Fail(fifoResult.Message);
                }

                await _context.SaveChangesAsync();

                foreach (var shelfId in activeItems.Select(x => x.ShelfId).Distinct())
                {
                    var statusResult = await UpdateShelfStatusAsync(shelfId, saveChanges: false);

                    if (!statusResult.IsSuccess)
                        return Result<bool>.Fail(statusResult.Message);
                }

                await _context.SaveChangesAsync();

                if (transaction != null)
                    await transaction.CommitAsync();

                return Result<bool>.Success(true, "Çıxış stoklara FIFO ilə uğurla tətbiq edildi.");
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return Result<bool>.Fail($"Çıxış stoklara tətbiq edilərkən xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ApplyCustomerReturnInAsync(Invoice invoice)
        {
            if (invoice == null)
                return Result<bool>.Fail("Qaimə məlumatı boş ola bilməz.");

            if (invoice.Type != InvoiceType.CustomerReturnIn)
                return Result<bool>.Fail("Bu qaimə müştəridən geri qaytarma qaiməsi deyil.");

            if (!invoice.ParentInvoiceId.HasValue || invoice.ParentInvoiceId.Value <= 0)
                return Result<bool>.Fail("Geri qaytarma üçün əsas satış qaiməsi seçilməlidir.");

            var activeItems = invoice.Items?
                .Where(x => x.IsActive)
                .ToList();

            if (activeItems == null || !activeItems.Any())
                return Result<bool>.Fail("Geri qaytarma qaiməsində məhsul yoxdur.");

            var parentInvoice = await _context.Invoices
                .FirstOrDefaultAsync(x =>
                    x.Id == invoice.ParentInvoiceId.Value &&
                    x.IsActive &&
                    x.Type == InvoiceType.StockOut &&
                    x.Status == InvoiceStatus.Confirmed);

            if (parentInvoice == null)
                return Result<bool>.Fail("Əsas satış qaiməsi tapılmadı və ya təsdiqlənmiş satış qaiməsi deyil.");

            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync()
                : null;

            try
            {
                foreach (var item in activeItems)
                {
                    if (item.ProductId <= 0)
                        return Result<bool>.Fail("Geri qaytarma qaiməsində məhsul düzgün seçilməyib.");

                    if (item.Quantity <= 0)
                        return Result<bool>.Fail("Geri qaytarma miqdarı 0-dan böyük olmalıdır.");

                    var returnResult = await ApplyCustomerReturnForItemAsync(
                        invoice,
                        parentInvoice.Id,
                        item.ProductId,
                        item.Quantity);

                    if (!returnResult.IsSuccess)
                        return Result<bool>.Fail(returnResult.Message);
                }

                await _context.SaveChangesAsync();

                var affectedShelfIds = await _context.StockMovements
                    .Where(x => x.InvoiceId == invoice.Id && x.IsActive && x.ToShelfId.HasValue)
                    .Select(x => x.ToShelfId!.Value)
                    .Distinct()
                    .ToListAsync();

                foreach (var shelfId in affectedShelfIds)
                {
                    var statusResult = await UpdateShelfStatusAsync(shelfId, saveChanges: false);

                    if (!statusResult.IsSuccess)
                        return Result<bool>.Fail(statusResult.Message);
                }

                await _context.SaveChangesAsync();

                if (transaction != null)
                    await transaction.CommitAsync();

                return Result<bool>.Success(true, "Müştəridən geri qaytarma FIFO ilə uğurla tətbiq edildi.");
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return Result<bool>.Fail($"Müştəridən geri qaytarma zamanı xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ApplySupplierReturnOutAsync(Invoice invoice)
        {
            if (invoice == null)
                return Result<bool>.Fail("Qaimə məlumatı boş ola bilməz.");

            if (invoice.Type != InvoiceType.SupplierReturnOut)
                return Result<bool>.Fail("Bu qaimə təchizatçıya geri qaytarma qaiməsi deyil.");

            if (!invoice.ParentInvoiceId.HasValue || invoice.ParentInvoiceId.Value <= 0)
                return Result<bool>.Fail("Təchizatçıya geri qaytarma üçün əsas giriş qaiməsi seçilməlidir.");

            var activeItems = invoice.Items?
                .Where(x => x.IsActive)
                .ToList();

            if (activeItems == null || !activeItems.Any())
                return Result<bool>.Fail("Təchizatçıya geri qaytarma qaiməsində məhsul yoxdur.");

            var parentInvoice = await _context.Invoices
                .FirstOrDefaultAsync(x =>
                    x.Id == invoice.ParentInvoiceId.Value &&
                    x.IsActive &&
                    x.Type == InvoiceType.StockIn &&
                    x.Status == InvoiceStatus.Confirmed);

            if (parentInvoice == null)
                return Result<bool>.Fail("Əsas giriş qaiməsi tapılmadı və ya təsdiqlənmiş giriş qaiməsi deyil.");

            foreach (var item in activeItems)
            {
                if (item.ProductId <= 0)
                    return Result<bool>.Fail("Qaimədə məhsul düzgün seçilməyib.");

                if (item.ShelfId <= 0)
                    return Result<bool>.Fail("Qaimədə rəf düzgün seçilməyib.");

                if (item.Quantity <= 0)
                    return Result<bool>.Fail("Qaimədə miqdar 0-dan böyük olmalıdır.");

                var stockCheck = await HasEnoughStockAsync(item.ProductId, item.ShelfId, item.Quantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                    return Result<bool>.Fail(stockCheck.Message);

                var parentBatchCheck = await HasEnoughParentStockInBatchAsync(
                    parentInvoice.Id,
                    item.ProductId,
                    item.ShelfId,
                    item.Quantity);

                if (!parentBatchCheck.IsSuccess || !parentBatchCheck.Data)
                    return Result<bool>.Fail(parentBatchCheck.Message);
            }

            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync()
                : null;

            try
            {
                foreach (var item in activeItems)
                {
                    var supplierReturnResult = await ApplySupplierReturnForItemAsync(
                        invoice,
                        parentInvoice.Id,
                        item.ProductId,
                        item.ShelfId,
                        item.Quantity);

                    if (!supplierReturnResult.IsSuccess)
                        return Result<bool>.Fail(supplierReturnResult.Message);
                }

                await _context.SaveChangesAsync();

                foreach (var shelfId in activeItems.Select(x => x.ShelfId).Distinct())
                {
                    var statusResult = await UpdateShelfStatusAsync(shelfId, saveChanges: false);

                    if (!statusResult.IsSuccess)
                        return Result<bool>.Fail(statusResult.Message);
                }

                await _context.SaveChangesAsync();

                if (transaction != null)
                    await transaction.CommitAsync();

                return Result<bool>.Success(true, "Təchizatçıya geri qaytarma FIFO ilə uğurla tətbiq edildi.");
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return Result<bool>.Fail($"Təchizatçıya geri qaytarma zamanı xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> HasEnoughShelfCapacityForItemsAsync(IEnumerable<InvoiceItem> items)
        {
            if (items == null)
                return Result<bool>.Fail("Yoxlanacaq məhsul siyahısı boşdur.");

            var activeItems = items.Where(x => x.IsActive).ToList();

            if (!activeItems.Any())
                return Result<bool>.Fail("Yoxlanacaq aktiv məhsul yoxdur.");

            foreach (var item in activeItems)
            {
                if (item.ProductId <= 0)
                    return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

                if (item.ShelfId <= 0)
                    return Result<bool>.Fail("Rəf düzgün seçilməyib.");

                if (item.Quantity <= 0)
                    return Result<bool>.Fail("Miqdar 0-dan böyük olmalıdır.");
            }

            var shelfIds = activeItems.Select(x => x.ShelfId).Distinct().ToList();

            var shelves = await _context.Shelves
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                .Where(x => shelfIds.Contains(x.Id) && x.IsActive)
                .ToListAsync();

            foreach (var shelfGroup in activeItems.GroupBy(x => x.ShelfId))
            {
                var shelf = shelves.FirstOrDefault(x => x.Id == shelfGroup.Key);

                if (shelf == null)
                    return Result<bool>.Fail("Rəf tapılmadı.");

                if (shelf.Capacity <= 0)
                    continue;

                var currentQuantity = shelf.ShelfStocks.Where(x => x.IsActive).Sum(x => x.Quantity);
                var incomingQuantity = shelfGroup.Sum(x => x.Quantity);
                var newTotalQuantity = currentQuantity + incomingQuantity;

                if (newTotalQuantity > shelf.Capacity)
                {
                    var availableQuantity = shelf.Capacity - currentQuantity;

                    if (availableQuantity < 0)
                        availableQuantity = 0;

                    return Result<bool>.Fail(
                        $"Rəfin say tutumu aşılır. Rəf: {shelf.Code}, Tutum: {shelf.Capacity:0.##}, Mövcud: {currentQuantity:0.##}, Boş yer: {availableQuantity:0.##}, Qaimə ilə əlavə edilən ümumi miqdar: {incomingQuantity:0.##}");
                }
            }

            return Result<bool>.Success(true, "Rəflərdə kifayət qədər boş yer var.");
        }

        public async Task<Result<bool>> TransferShelfAsync(
            int productId,
            int fromShelfId,
            int toShelfId,
            decimal quantity,
            string? note = null)
        {
            if (productId <= 0)
                return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

            if (fromShelfId <= 0 || toShelfId <= 0)
                return Result<bool>.Fail("Rəf düzgün seçilməyib.");

            if (fromShelfId == toShelfId)
                return Result<bool>.Fail("Eyni rəfə transfer etmək olmaz.");

            if (quantity <= 0)
                return Result<bool>.Fail("Transfer miqdarı 0-dan böyük olmalıdır.");

            var productExists = await _context.Products.AnyAsync(x => x.Id == productId && x.IsActive);
            if (!productExists)
                return Result<bool>.Fail("Məhsul tapılmadı.");

            var fromShelfExists = await _context.Shelves.AnyAsync(x => x.Id == fromShelfId && x.IsActive);
            if (!fromShelfExists)
                return Result<bool>.Fail("Çıxış rəfi tapılmadı.");

            var toShelfExists = await _context.Shelves.AnyAsync(x => x.Id == toShelfId && x.IsActive);
            if (!toShelfExists)
                return Result<bool>.Fail("Giriş rəfi tapılmadı.");

            var stockCheck = await HasEnoughStockAsync(productId, fromShelfId, quantity);
            if (!stockCheck.IsSuccess || !stockCheck.Data)
                return Result<bool>.Fail(stockCheck.Message);

            var fifoCheck = await HasEnoughFifoBatchStockAsync(productId, fromShelfId, quantity);
            if (!fifoCheck.IsSuccess || !fifoCheck.Data)
                return Result<bool>.Fail(fifoCheck.Message);

            var capacityCheck = await HasEnoughShelfCapacityAsync(productId, toShelfId, quantity);
            if (!capacityCheck.IsSuccess || !capacityCheck.Data)
                return Result<bool>.Fail(capacityCheck.Message);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal need = quantity;

                var batches = await _context.StockBatches
                    .Where(x =>
                        x.ProductId == productId &&
                        x.ShelfId == fromShelfId &&
                        x.RemainingQuantity > 0 &&
                        x.IsActive)
                    .OrderBy(x => x.EntryDate)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

                foreach (var batch in batches)
                {
                    if (need <= 0)
                        break;

                    var take = Math.Min(batch.RemainingQuantity, need);

                    batch.RemainingQuantity -= take;
                    batch.UpdatedAt = DateTime.Now;

                    if (batch.RemainingQuantity <= 0)
                    {
                        batch.RemainingQuantity = 0;
                        batch.IsActive = false;
                    }

                    var newBatch = new StockBatch
                    {
                        ProductId = productId,
                        ShelfId = toShelfId,
                        SourceInvoiceId = batch.SourceInvoiceId,
                        SourceInvoiceItemId = batch.SourceInvoiceItemId,

                        BatchNumber = $"TRF-{batch.Id}-{DateTime.Now:yyyyMMddHHmmssfff}",

                        EntryDate = batch.EntryDate,

                        Currency = batch.Currency,
                        ExchangeRate = batch.ExchangeRate <= 0 ? 1 : batch.ExchangeRate,

                        PurchaseUnitPrice = batch.PurchaseUnitPrice,
                        LocalUnitPrice = batch.LocalUnitPrice,

                        ExpenseUnitShare = batch.ExpenseUnitShare,
                        TaxUnitShare = batch.TaxUnitShare,
                        DiscountUnitShare = batch.DiscountUnitShare,

                        FinalUnitCost = batch.FinalUnitCost,
                        FinalTotalCost = Math.Round(batch.FinalUnitCost * take, 2),

                        PurchasePrice = batch.PurchasePrice,
                        InitialQuantity = take,
                        RemainingQuantity = take,

                        Note = $"Rəf transferi ilə yaranan partiya. Köhnə BatchId: {batch.Id}",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.StockBatches.AddAsync(newBatch);

                    var decreaseResult = await DecreaseShelfStockAsync(productId, fromShelfId, take, saveChanges: false);

                    if (!decreaseResult.IsSuccess)
                    {
                        await transaction.RollbackAsync();
                        return Result<bool>.Fail(decreaseResult.Message);
                    }

                    var increaseResult = await IncreaseShelfStockAsync(productId, toShelfId, take, saveChanges: false);

                    if (!increaseResult.IsSuccess)
                    {
                        await transaction.RollbackAsync();
                        return Result<bool>.Fail(increaseResult.Message);
                    }

                    var movement = new StockMovement
                    {
                        ProductId = productId,
                        StockBatch = newBatch,
                        MovementType = StockMovementType.ShelfTransfer,
                        Quantity = take,

                        UnitCost = batch.FinalUnitCost,
                        TotalCost = Math.Round(batch.FinalUnitCost * take, 2),

                        FromShelfId = fromShelfId,
                        ToShelfId = toShelfId,
                        InvoiceId = null,
                        Note = note ?? $"FIFO ilə rəfdən-rəfə transfer edildi. Köhnə BatchId: {batch.Id}",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.StockMovements.AddAsync(movement);

                    need -= take;
                }

                if (need > 0)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Fail("Transfer üçün FIFO partiya qalığı kifayət deyil.");
                }

                await _context.SaveChangesAsync();

                var fromStatusResult = await UpdateShelfStatusAsync(fromShelfId, saveChanges: false);
                if (!fromStatusResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Fail(fromStatusResult.Message);
                }

                var toStatusResult = await UpdateShelfStatusAsync(toShelfId, saveChanges: false);
                if (!toStatusResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Fail(toStatusResult.Message);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<bool>.Success(true, "Transfer FIFO ilə uğurla tamamlandı.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Transfer zamanı xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> HasEnoughStockAsync(int productId, int shelfId, decimal quantity)
        {
            if (productId <= 0)
                return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

            if (shelfId <= 0)
                return Result<bool>.Fail("Rəf düzgün seçilməyib.");

            if (quantity <= 0)
                return Result<bool>.Fail("Miqdar 0-dan böyük olmalıdır.");

            var shelfStock = await _context.ShelfStocks
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.IsActive);

            if (shelfStock == null)
                return Result<bool>.Fail("Seçilən rəfdə bu məhsul yoxdur.");

            if (shelfStock.Quantity < quantity)
                return Result<bool>.Fail($"Rəfdə kifayət qədər stok yoxdur. Mövcud stok: {shelfStock.Quantity:0.##}");

            return Result<bool>.Success(true, "Rəfdə kifayət qədər stok var.");
        }

        public async Task<Result<bool>> HasEnoughFifoBatchStockAsync(int productId, int shelfId, decimal quantity)
        {
            if (productId <= 0)
                return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

            if (shelfId <= 0)
                return Result<bool>.Fail("Rəf düzgün seçilməyib.");

            if (quantity <= 0)
                return Result<bool>.Fail("Miqdar 0-dan böyük olmalıdır.");

            var totalBatchQuantity = await _context.StockBatches
                .Where(x =>
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.IsActive &&
                    x.RemainingQuantity > 0)
                .SumAsync(x => x.RemainingQuantity);

            if (totalBatchQuantity < quantity)
            {
                return Result<bool>.Fail(
                    $"FIFO partiya qalığı kifayət deyil. Mövcud partiya qalığı: {totalBatchQuantity:0.##}, tələb olunan: {quantity:0.##}");
            }

            return Result<bool>.Success(true, "FIFO partiya qalığı kifayətdir.");
        }

        private async Task<Result<bool>> HasEnoughParentStockInBatchAsync(
            int parentInvoiceId,
            int productId,
            int shelfId,
            decimal quantity)
        {
            var availableQuantity = await _context.StockBatches
                .Where(x =>
                    x.SourceInvoiceId == parentInvoiceId &&
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.IsActive &&
                    x.RemainingQuantity > 0)
                .SumAsync(x => x.RemainingQuantity);

            if (availableQuantity < quantity)
            {
                return Result<bool>.Fail(
                    $"Əsas giriş qaiməsinə aid partiya qalığı kifayət deyil. Mövcud: {availableQuantity:0.##}, tələb olunan: {quantity:0.##}");
            }

            return Result<bool>.Success(true, "Əsas giriş qaiməsinə aid partiya qalığı kifayətdir.");
        }

        public async Task<Result<bool>> HasEnoughShelfCapacityAsync(
            int productId,
            int shelfId,
            decimal incomingQuantity)
        {
            if (productId <= 0)
                return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

            if (shelfId <= 0)
                return Result<bool>.Fail("Rəf düzgün seçilməyib.");

            if (incomingQuantity <= 0)
                return Result<bool>.Fail("Əlavə ediləcək miqdar 0-dan böyük olmalıdır.");

            var shelf = await _context.Shelves
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeDefinition)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeValue)
                .Include(x => x.AttributeValues.Where(v => v.IsActive))
                    .ThenInclude(x => x.ShelfAttributeDefinition)
                .FirstOrDefaultAsync(x => x.Id == shelfId && x.IsActive);

            if (shelf == null)
                return Result<bool>.Fail("Rəf tapılmadı.");

            var product = await _context.Products
                .Include(x => x.Attributes.Where(a => a.IsActive))
                    .ThenInclude(x => x.AttributeDefinition)
                .Include(x => x.Attributes.Where(a => a.IsActive))
                    .ThenInclude(x => x.AttributeValue)
                .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);

            if (product == null)
                return Result<bool>.Fail("Məhsul tapılmadı.");

            if (shelf.Capacity > 0)
            {
                var currentQuantity = shelf.ShelfStocks.Where(x => x.IsActive).Sum(x => x.Quantity);
                var newTotalQuantity = currentQuantity + incomingQuantity;

                if (newTotalQuantity > shelf.Capacity)
                {
                    var availableQuantity = shelf.Capacity - currentQuantity;

                    if (availableQuantity < 0)
                        availableQuantity = 0;

                    return Result<bool>.Fail(
                        $"Rəfin say tutumu aşılır. Rəf: {shelf.Code}, Tutum: {shelf.Capacity:0.##}, Mövcud: {currentQuantity:0.##}, Boş yer: {availableQuantity:0.##}, Əlavə edilən: {incomingQuantity:0.##}");
                }
            }

            foreach (var shelfAttributeValue in shelf.AttributeValues.Where(x => x.IsActive))
            {
                var definition = shelfAttributeValue.ShelfAttributeDefinition;

                if (definition == null || !definition.IsActive)
                    continue;

                if (!definition.IsLimit)
                    continue;

                if (!shelfAttributeValue.NumericValue.HasValue || shelfAttributeValue.NumericValue.Value <= 0)
                    continue;

                var key = definition.Key.Trim();

                if (key.Equals("MaxWeightKg", StringComparison.OrdinalIgnoreCase))
                {
                    var checkResult = await CheckNumericLimitAsync(
                        shelf,
                        product,
                        incomingQuantity,
                        new[] { "çəki", "ceki", "weight", "kg", "kq" },
                        "çəki",
                        definition.Name,
                        shelfAttributeValue.NumericValue.Value,
                        definition.Unit ?? "kg");

                    if (!checkResult.IsSuccess)
                        return checkResult;
                }
                else if (key.Equals("MaxVolumeM3", StringComparison.OrdinalIgnoreCase))
                {
                    var checkResult = await CheckNumericLimitAsync(
                        shelf,
                        product,
                        incomingQuantity,
                        new[] { "həcm", "hecm", "volume", "m3", "m³" },
                        "həcm",
                        definition.Name,
                        shelfAttributeValue.NumericValue.Value,
                        definition.Unit ?? "m³");

                    if (!checkResult.IsSuccess)
                        return checkResult;
                }
            }

            return Result<bool>.Success(true, "Rəfdə kifayət qədər boş yer var.");
        }

        private async Task<Result<bool>> CheckNumericLimitAsync(
            Shelf shelf,
            Product incomingProduct,
            decimal incomingQuantity,
            string[] productAttributeNameKeywords,
            string productAttributeKeyForMessage,
            string shelfLimitName,
            decimal shelfLimitValue,
            string unit)
        {
            var incomingProductSingleValue = GetProductNumericAttributeValue(
                incomingProduct,
                productAttributeNameKeywords);

            if (!incomingProductSingleValue.HasValue)
            {
                return Result<bool>.Fail(
                    $"Bu rəfdə \"{shelfLimitName}\" limiti aktivdir, amma seçilən məhsulun {productAttributeKeyForMessage} dəyəri təyin edilməyib. Əvvəl məhsul kartında bu xüsusiyyəti əlavə edin.");
            }

            var currentTotalValue = 0m;

            foreach (var stock in shelf.ShelfStocks.Where(x => x.IsActive && x.Quantity > 0))
            {
                var stockProduct = stock.Product;

                if (stockProduct == null)
                {
                    stockProduct = await _context.Products
                        .Include(x => x.Attributes.Where(a => a.IsActive))
                            .ThenInclude(x => x.AttributeDefinition)
                        .Include(x => x.Attributes.Where(a => a.IsActive))
                            .ThenInclude(x => x.AttributeValue)
                        .FirstOrDefaultAsync(x => x.Id == stock.ProductId && x.IsActive);
                }

                if (stockProduct == null)
                    continue;

                var productSingleValue = GetProductNumericAttributeValue(
                    stockProduct,
                    productAttributeNameKeywords);

                if (!productSingleValue.HasValue)
                {
                    return Result<bool>.Fail(
                        $"Rəfdə olan \"{stockProduct.Name}\" məhsulunun {productAttributeKeyForMessage} dəyəri təyin edilməyib. Bu rəfdə \"{shelfLimitName}\" limiti aktiv olduğu üçün bütün məhsullarda həmin dəyər olmalıdır.");
                }

                currentTotalValue += productSingleValue.Value * stock.Quantity;
            }

            var incomingTotalValue = incomingProductSingleValue.Value * incomingQuantity;
            var newTotalValue = currentTotalValue + incomingTotalValue;

            if (newTotalValue > shelfLimitValue)
            {
                var availableValue = shelfLimitValue - currentTotalValue;

                if (availableValue < 0)
                    availableValue = 0;

                return Result<bool>.Fail(
                    $"Rəfin \"{shelfLimitName}\" limiti aşılır. Rəf: {shelf.Code}, Limit: {shelfLimitValue:0.##} {unit}, Mövcud: {currentTotalValue:0.##} {unit}, Boş limit: {availableValue:0.##} {unit}, Əlavə edilən: {incomingTotalValue:0.##} {unit}");
            }

            return Result<bool>.Success(true, "Limit uyğundur.");
        }

        private decimal? GetProductNumericAttributeValue(Product product, string[] attributeNameKeywords)
        {
            if (product.Attributes == null || !product.Attributes.Any())
                return null;

            var attribute = product.Attributes
                .Where(x =>
                    x.IsActive &&
                    x.AttributeDefinition != null &&
                    x.AttributeValue != null)
                .FirstOrDefault(x =>
                {
                    var name = x.AttributeDefinition.Name?.Trim().ToLower() ?? string.Empty;

                    return attributeNameKeywords.Any(keyword =>
                        name.Contains(keyword.Trim().ToLower()));
                });

            if (attribute == null)
                return null;

            var rawValue = attribute.AttributeValue.Value?.Trim();

            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            rawValue = rawValue
                .Replace("kg", "", StringComparison.OrdinalIgnoreCase)
                .Replace("kq", "", StringComparison.OrdinalIgnoreCase)
                .Replace("m3", "", StringComparison.OrdinalIgnoreCase)
                .Replace("m³", "", StringComparison.OrdinalIgnoreCase)
                .Replace("sm", "", StringComparison.OrdinalIgnoreCase)
                .Replace("cm", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (decimal.TryParse(rawValue, out var value))
                return value;

            rawValue = rawValue.Replace(",", ".");

            if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return value;

            return null;
        }

        public async Task<Result<bool>> IncreaseShelfStockAsync(
            int productId,
            int shelfId,
            decimal quantity,
            bool saveChanges = true)
        {
            if (productId <= 0)
                return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

            if (shelfId <= 0)
                return Result<bool>.Fail("Rəf düzgün seçilməyib.");

            if (quantity <= 0)
                return Result<bool>.Fail("Miqdar 0-dan böyük olmalıdır.");

            var productExists = await _context.Products.AnyAsync(x => x.Id == productId && x.IsActive);
            if (!productExists)
                return Result<bool>.Fail("Məhsul tapılmadı.");

            var shelfExists = await _context.Shelves.AnyAsync(x => x.Id == shelfId && x.IsActive);
            if (!shelfExists)
                return Result<bool>.Fail("Rəf tapılmadı.");

            var capacityCheck = await HasEnoughShelfCapacityAsync(productId, shelfId, quantity);

            if (!capacityCheck.IsSuccess || !capacityCheck.Data)
                return Result<bool>.Fail(capacityCheck.Message);

            var shelfStock = await _context.ShelfStocks
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId &&
                    x.ShelfId == shelfId);

            if (shelfStock == null)
            {
                shelfStock = new ShelfStock
                {
                    ProductId = productId,
                    ShelfId = shelfId,
                    Quantity = quantity,
                    LastMovementDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.ShelfStocks.AddAsync(shelfStock);
            }
            else
            {
                shelfStock.Quantity += quantity;
                shelfStock.LastMovementDate = DateTime.Now;
                shelfStock.UpdatedAt = DateTime.Now;
                shelfStock.IsActive = true;
            }

            if (saveChanges)
            {
                await _context.SaveChangesAsync();

                var statusResult = await UpdateShelfStatusAsync(shelfId, saveChanges: false);
                if (!statusResult.IsSuccess)
                    return Result<bool>.Fail(statusResult.Message);

                await _context.SaveChangesAsync();
            }

            return Result<bool>.Success(true, "Rəf stoku artırıldı.");
        }

        public async Task<Result<bool>> DecreaseShelfStockAsync(
            int productId,
            int shelfId,
            decimal quantity,
            bool saveChanges = true)
        {
            if (productId <= 0)
                return Result<bool>.Fail("Məhsul düzgün seçilməyib.");

            if (shelfId <= 0)
                return Result<bool>.Fail("Rəf düzgün seçilməyib.");

            if (quantity <= 0)
                return Result<bool>.Fail("Miqdar 0-dan böyük olmalıdır.");

            var shelfStock = await _context.ShelfStocks
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.IsActive);

            if (shelfStock == null)
                return Result<bool>.Fail("Seçilən rəfdə bu məhsul yoxdur.");

            if (shelfStock.Quantity < quantity)
                return Result<bool>.Fail($"Rəfdə kifayət qədər stok yoxdur. Mövcud stok: {shelfStock.Quantity:0.##}");

            shelfStock.Quantity -= quantity;
            shelfStock.LastMovementDate = DateTime.Now;
            shelfStock.UpdatedAt = DateTime.Now;

            if (shelfStock.Quantity <= 0)
            {
                shelfStock.Quantity = 0;
                shelfStock.IsActive = false;
            }

            if (saveChanges)
            {
                await _context.SaveChangesAsync();

                var statusResult = await UpdateShelfStatusAsync(shelfId, saveChanges: false);
                if (!statusResult.IsSuccess)
                    return Result<bool>.Fail(statusResult.Message);

                await _context.SaveChangesAsync();
            }

            return Result<bool>.Success(true, "Rəf stoku azaldıldı.");
        }

        public async Task<Result<StockMovement>> CreateMovementAsync(
            int productId,
            StockMovementType movementType,
            decimal quantity,
            int? fromShelfId = null,
            int? toShelfId = null,
            int? invoiceId = null,
            string? note = null,
            bool saveChanges = true,
            int? stockBatchId = null,
            decimal unitCost = 0)
        {
            if (productId <= 0)
                return Result<StockMovement>.Fail("Məhsul düzgün seçilməyib.");

            if (quantity <= 0)
                return Result<StockMovement>.Fail("Miqdar 0-dan böyük olmalıdır.");

            var productExists = await _context.Products.AnyAsync(x => x.Id == productId && x.IsActive);
            if (!productExists)
                return Result<StockMovement>.Fail("Məhsul tapılmadı.");

            if (stockBatchId.HasValue)
            {
                var batchExists = await _context.StockBatches.AnyAsync(x => x.Id == stockBatchId.Value);
                if (!batchExists)
                    return Result<StockMovement>.Fail("FIFO partiya tapılmadı.");
            }

            if (fromShelfId.HasValue)
            {
                var fromShelfExists = await _context.Shelves.AnyAsync(x => x.Id == fromShelfId.Value && x.IsActive);
                if (!fromShelfExists)
                    return Result<StockMovement>.Fail("Çıxış rəfi tapılmadı.");
            }

            if (toShelfId.HasValue)
            {
                var toShelfExists = await _context.Shelves.AnyAsync(x => x.Id == toShelfId.Value && x.IsActive);
                if (!toShelfExists)
                    return Result<StockMovement>.Fail("Giriş rəfi tapılmadı.");
            }

            var movement = new StockMovement
            {
                ProductId = productId,
                StockBatchId = stockBatchId,
                MovementType = movementType,
                Quantity = quantity,

                UnitCost = unitCost,
                TotalCost = Math.Round(unitCost * quantity, 2),

                FromShelfId = fromShelfId,
                ToShelfId = toShelfId,
                InvoiceId = invoiceId,
                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.StockMovements.AddAsync(movement);

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<StockMovement>.Success(movement, "Stok hərəkəti yaradıldı.");
        }

        public async Task<Result<bool>> UpdateShelfStatusAsync(int shelfId, bool saveChanges = true)
        {
            var shelf = await _context.Shelves
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                .FirstOrDefaultAsync(x => x.Id == shelfId && x.IsActive);

            if (shelf == null)
                return Result<bool>.Fail("Rəf tapılmadı.");

            var totalQuantity = shelf.ShelfStocks.Where(x => x.IsActive).Sum(x => x.Quantity);

            if (shelf.Capacity <= 0 || totalQuantity <= 0)
            {
                shelf.OccupancyPercent = 0;
                shelf.Status = ShelfStatus.Empty;
                shelf.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Rəf statusu yeniləndi.");
            }

            var percent = (totalQuantity / shelf.Capacity) * 100;

            if (percent < 0)
                percent = 0;

            if (percent > 100)
                percent = 100;

            shelf.OccupancyPercent = Math.Round(percent, 2);

            if (shelf.OccupancyPercent < 30)
                shelf.Status = ShelfStatus.Low;
            else if (shelf.OccupancyPercent < 100)
                shelf.Status = ShelfStatus.Normal;
            else
                shelf.Status = ShelfStatus.Full;

            shelf.UpdatedAt = DateTime.Now;

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Rəf statusu yeniləndi.");
        }

        private async Task<Result<bool>> ApplyFifoOutAsync(
            Invoice invoice,
            int productId,
            int shelfId,
            decimal quantity,
            StockMovementType movementType,
            string notePrefix)
        {
            decimal need = quantity;

            var batches = await _context.StockBatches
                .Where(x =>
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.RemainingQuantity > 0 &&
                    x.IsActive)
                .OrderBy(x => x.EntryDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (need <= 0)
                    break;

                var take = Math.Min(batch.RemainingQuantity, need);

                batch.RemainingQuantity -= take;
                batch.UpdatedAt = DateTime.Now;

                if (batch.RemainingQuantity <= 0)
                {
                    batch.RemainingQuantity = 0;
                    batch.IsActive = false;
                }

                var decreaseResult = await DecreaseShelfStockAsync(productId, shelfId, take, saveChanges: false);

                if (!decreaseResult.IsSuccess)
                    return Result<bool>.Fail(decreaseResult.Message);

                var movementResult = await CreateMovementAsync(
                    productId: productId,
                    movementType: movementType,
                    quantity: take,
                    fromShelfId: shelfId,
                    toShelfId: null,
                    invoiceId: invoice.Id,
                    note: $"{notePrefix}. Qaimə №: {invoice.InvoiceNumber}, BatchId: {batch.Id}",
                    saveChanges: false,
                    stockBatchId: batch.Id,
                    unitCost: batch.FinalUnitCost);

                if (!movementResult.IsSuccess)
                    return Result<bool>.Fail(movementResult.Message);

                need -= take;
            }

            if (need > 0)
                return Result<bool>.Fail("FIFO partiya qalığı kifayət deyil.");

            return Result<bool>.Success(true, "FIFO çıxış tətbiq edildi.");
        }

        private async Task<Result<bool>> ApplyCustomerReturnForItemAsync(
            Invoice returnInvoice,
            int parentSaleInvoiceId,
            int productId,
            decimal returnQuantity)
        {
            var saleMovements = await _context.StockMovements
                .Where(x =>
                    x.InvoiceId == parentSaleInvoiceId &&
                    x.ProductId == productId &&
                    x.MovementType == StockMovementType.StockOut &&
                    x.StockBatchId.HasValue &&
                    x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (!saleMovements.Any())
                return Result<bool>.Fail("Əsas satış qaiməsində bu məhsul üçün FIFO çıxış izi tapılmadı.");

            var alreadyReturnedMovements = await _context.StockMovements
                .Include(x => x.Invoice)
                .Where(x =>
                    x.Invoice != null &&
                    x.Invoice.ParentInvoiceId == parentSaleInvoiceId &&
                    x.Invoice.Type == InvoiceType.CustomerReturnIn &&
                    x.Invoice.Status == InvoiceStatus.Confirmed &&
                    x.ProductId == productId &&
                    x.MovementType == StockMovementType.CustomerReturnIn &&
                    x.StockBatchId.HasValue &&
                    x.IsActive)
                .ToListAsync();

            var totalSold = saleMovements.Sum(x => x.Quantity);
            var totalAlreadyReturned = alreadyReturnedMovements.Sum(x => x.Quantity);
            var availableToReturn = totalSold - totalAlreadyReturned;

            if (availableToReturn < returnQuantity)
            {
                return Result<bool>.Fail(
                    $"Qaytarılacaq miqdar satışdan artıqdır. Satılıb: {totalSold:0.##}, əvvəl qaytarılıb: {totalAlreadyReturned:0.##}, qalan qaytarıla bilən: {availableToReturn:0.##}");
            }

            decimal need = returnQuantity;

            foreach (var saleMove in saleMovements)
            {
                if (need <= 0)
                    break;

                var returnedForThisMove = alreadyReturnedMovements
                    .Where(x => x.StockBatchId == saleMove.StockBatchId)
                    .Sum(x => x.Quantity);

                var availableFromThisMove = saleMove.Quantity - returnedForThisMove;

                if (availableFromThisMove <= 0)
                    continue;

                var take = Math.Min(availableFromThisMove, need);

                if (!saleMove.StockBatchId.HasValue)
                    return Result<bool>.Fail("Satış hərəkətində FIFO BatchId yoxdur.");

                var batch = await _context.StockBatches
                    .FirstOrDefaultAsync(x => x.Id == saleMove.StockBatchId.Value);

                if (batch == null)
                    return Result<bool>.Fail("Satışa aid FIFO partiya tapılmadı.");

                var toShelfId = saleMove.FromShelfId ?? batch.ShelfId;

                var increaseResult = await IncreaseShelfStockAsync(productId, toShelfId, take, saveChanges: false);

                if (!increaseResult.IsSuccess)
                    return Result<bool>.Fail(increaseResult.Message);

                batch.RemainingQuantity += take;
                batch.IsActive = true;
                batch.UpdatedAt = DateTime.Now;

                var movementResult = await CreateMovementAsync(
                    productId: productId,
                    movementType: StockMovementType.CustomerReturnIn,
                    quantity: take,
                    fromShelfId: null,
                    toShelfId: toShelfId,
                    invoiceId: returnInvoice.Id,
                    note: $"Müştəridən geri qaytarma. Əsas satış qaiməsi Id: {parentSaleInvoiceId}, BatchId: {batch.Id}",
                    saveChanges: false,
                    stockBatchId: batch.Id,
                    unitCost: batch.FinalUnitCost);

                if (!movementResult.IsSuccess)
                    return Result<bool>.Fail(movementResult.Message);

                need -= take;
            }

            if (need > 0)
                return Result<bool>.Fail("Geri qaytarma miqdarı satış izlərinə görə tam bölünə bilmədi.");

            return Result<bool>.Success(true, "Müştəri geri qaytarması tətbiq edildi.");
        }

        private async Task<Result<bool>> ApplySupplierReturnForItemAsync(
            Invoice supplierReturnInvoice,
            int parentStockInInvoiceId,
            int productId,
            int shelfId,
            decimal returnQuantity)
        {
            decimal need = returnQuantity;

            var batches = await _context.StockBatches
                .Where(x =>
                    x.SourceInvoiceId == parentStockInInvoiceId &&
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.RemainingQuantity > 0 &&
                    x.IsActive)
                .OrderBy(x => x.EntryDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (need <= 0)
                    break;

                var take = Math.Min(batch.RemainingQuantity, need);

                batch.RemainingQuantity -= take;
                batch.UpdatedAt = DateTime.Now;

                if (batch.RemainingQuantity <= 0)
                {
                    batch.RemainingQuantity = 0;
                    batch.IsActive = false;
                }

                var decreaseResult = await DecreaseShelfStockAsync(productId, shelfId, take, saveChanges: false);

                if (!decreaseResult.IsSuccess)
                    return Result<bool>.Fail(decreaseResult.Message);

                var movementResult = await CreateMovementAsync(
                    productId: productId,
                    movementType: StockMovementType.SupplierReturnOut,
                    quantity: take,
                    fromShelfId: shelfId,
                    toShelfId: null,
                    invoiceId: supplierReturnInvoice.Id,
                    note: $"Təchizatçıya geri qaytarma. Əsas giriş qaiməsi Id: {parentStockInInvoiceId}, BatchId: {batch.Id}",
                    saveChanges: false,
                    stockBatchId: batch.Id,
                    unitCost: batch.FinalUnitCost);

                if (!movementResult.IsSuccess)
                    return Result<bool>.Fail(movementResult.Message);

                need -= take;
            }

            if (need > 0)
                return Result<bool>.Fail("Təchizatçıya geri qaytarma üçün əsas giriş qaiməsinə aid partiya qalığı kifayət deyil.");

            return Result<bool>.Success(true, "Təchizatçı geri qaytarması tətbiq edildi.");
        }

        private static decimal GetFinalUnitCost(InvoiceItem item)
        {
            if (item.FinalUnitCost > 0)
                return item.FinalUnitCost;

            if (item.LocalUnitPrice > 0)
                return item.LocalUnitPrice;

            var exchangeRate = item.ExchangeRate <= 0 ? 1 : item.ExchangeRate;

            return Math.Round(item.Price * exchangeRate, 4);
        }
    }
}