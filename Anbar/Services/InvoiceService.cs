using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // Bu servis giriş, çıxış və geri qaytarma qaimələrini idarə edir.
    // YENI ENTERPRISE MƏNTİQ:
    // InvoiceService artıq vergi/xərc/maya hesablamasını özü etmir.
    // O sadəcə prosesi idarə edir:
    // TaxCalculationService -> TaxAllocationService -> ExpenseAllocationService -> CostCalculationService -> StockService.
    public class InvoiceService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly TaxCalculationService _taxCalculationService;
        private readonly TaxAllocationService _taxAllocationService;
        private readonly SupplierBalanceService _supplierBalanceService;
        private readonly CustomerBalanceService _customerBalanceService;
        private readonly SettingsService _settingsService;
        private readonly ExpenseAllocationService _expenseAllocationService;
        private readonly CostCalculationService _costCalculationService;

        public InvoiceService(
            AppDbContext context,
            StockService stockService,
            TaxCalculationService taxCalculationService,
            TaxAllocationService taxAllocationService,
            ExpenseAllocationService expenseAllocationService,
            CostCalculationService costCalculationService,
            SupplierBalanceService supplierBalanceService,
            CustomerBalanceService customerBalanceService,
            SettingsService settingsService)
        {
            _context = context;
            _stockService = stockService;
            _taxCalculationService = taxCalculationService;
            _taxAllocationService = taxAllocationService;
            _expenseAllocationService = expenseAllocationService;
            _costCalculationService = costCalculationService;
            _supplierBalanceService = supplierBalanceService;
            _customerBalanceService = customerBalanceService;
            _settingsService = settingsService;
        }

        // Köhnə WPF View-lar qırılmasın deyə saxlanıldı.
        public InvoiceService(AppDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;

            _taxCalculationService = new TaxCalculationService(context);
            _taxAllocationService = new TaxAllocationService(context);
            _expenseAllocationService = new ExpenseAllocationService(context);
            _costCalculationService = new CostCalculationService(context);

            var currencyService = new CurrencyService(context);
            _settingsService = new SettingsService(context);
            _supplierBalanceService = new SupplierBalanceService(context, currencyService);
            _customerBalanceService = new CustomerBalanceService(context, currencyService);
        }

        public async Task<Result<List<Invoice>>> GetAllAsync(InvoiceType? type = null, InvoiceStatus? status = null)
        {
            var query = _context.Invoices
                .Include(x => x.Supplier)
                .Include(x => x.Customer)
                .Include(x => x.ParentInvoice)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Shelf)
                .Include(x => x.Expenses.Where(e => e.IsActive))
                    .ThenInclude(x => x.ExpenseType)
                .Include(x => x.Expenses.Where(e => e.IsActive))
                    .ThenInclude(x => x.FieldValues.Where(f => f.IsActive))
                .Include(x => x.Taxes.Where(t => t.IsActive))
                .Where(x => x.IsActive)
                .AsQueryable();

            if (type.HasValue)
                query = query.Where(x => x.Type == type.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            var invoices = await query
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();

            return Result<List<Invoice>>.Success(invoices);
        }

        public async Task<Result<Invoice>> GetByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Supplier)
                .Include(x => x.Customer)
                .Include(x => x.ParentInvoice)
                .Include(x => x.ReturnInvoices.Where(r => r.IsActive))
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeDefinition)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Attributes)
                            .ThenInclude(x => x.AttributeValue)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.ProductTaxes.Where(t => t.IsActive))
                            .ThenInclude(x => x.Tax)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Taxes.Where(t => t.IsActive))
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.ExpenseAllocations.Where(a => a.IsActive))
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.TaxAllocations.Where(a => a.IsActive))
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Shelf)
                .Include(x => x.Expenses.Where(e => e.IsActive))
                    .ThenInclude(x => x.ExpenseType)
                .Include(x => x.Expenses.Where(e => e.IsActive))
                    .ThenInclude(x => x.FieldValues.Where(f => f.IsActive))
                .Include(x => x.Taxes.Where(t => t.IsActive))
                    .ThenInclude(x => x.Allocations.Where(a => a.IsActive))
                .Include(x => x.ItemTaxes.Where(t => t.IsActive))
                .Include(x => x.DynamicFieldValues.Where(f => f.IsActive))
                .Include(x => x.CostSummary)
                .Include(x => x.StockMovements.Where(m => m.IsActive))
                    .ThenInclude(x => x.StockBatch)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (invoice == null)
                return Result<Invoice>.Fail("Qaimə tapılmadı.");

            return Result<Invoice>.Success(invoice);
        }

        public async Task<Result<Invoice>> CreateDraftAsync(
            InvoiceType type,
            int? supplierId = null,
            int? customerId = null,
            decimal paidAmount = 0,
            string? note = null,
            int? parentInvoiceId = null)
        {
            if (type == InvoiceType.StockIn)
            {
                if (!supplierId.HasValue)
                    return Result<Invoice>.Fail("Giriş qaiməsi üçün təchizatçı seçilməlidir.");

                var supplierExists = await _context.Suppliers.AnyAsync(x => x.Id == supplierId.Value && x.IsActive);

                if (!supplierExists)
                    return Result<Invoice>.Fail("Təchizatçı tapılmadı.");
            }

            if (type == InvoiceType.StockOut)
            {
                if (!customerId.HasValue)
                    return Result<Invoice>.Fail("Çıxış qaiməsi üçün müştəri seçilməlidir.");

                var customerExists = await _context.Customers.AnyAsync(x => x.Id == customerId.Value && x.IsActive);

                if (!customerExists)
                    return Result<Invoice>.Fail("Müştəri tapılmadı.");
            }

            if (type == InvoiceType.CustomerReturnIn)
            {
                if (!parentInvoiceId.HasValue || parentInvoiceId.Value <= 0)
                    return Result<Invoice>.Fail("Müştəridən geri qaytarma üçün əsas satış qaiməsi seçilməlidir.");

                var parentInvoice = await _context.Invoices.FirstOrDefaultAsync(x =>
                    x.Id == parentInvoiceId.Value &&
                    x.IsActive &&
                    x.Type == InvoiceType.StockOut &&
                    x.Status == InvoiceStatus.Confirmed);

                if (parentInvoice == null)
                    return Result<Invoice>.Fail("Əsas satış qaiməsi tapılmadı və ya təsdiqlənmiş satış qaiməsi deyil.");

                customerId = parentInvoice.CustomerId;
                supplierId = null;
            }

            if (type == InvoiceType.SupplierReturnOut)
            {
                if (!parentInvoiceId.HasValue || parentInvoiceId.Value <= 0)
                    return Result<Invoice>.Fail("Təchizatçıya geri qaytarma üçün əsas giriş qaiməsi seçilməlidir.");

                var parentInvoice = await _context.Invoices.FirstOrDefaultAsync(x =>
                    x.Id == parentInvoiceId.Value &&
                    x.IsActive &&
                    x.Type == InvoiceType.StockIn &&
                    x.Status == InvoiceStatus.Confirmed);

                if (parentInvoice == null)
                    return Result<Invoice>.Fail("Əsas giriş qaiməsi tapılmadı və ya təsdiqlənmiş giriş qaiməsi deyil.");

                supplierId = parentInvoice.SupplierId;
                customerId = null;
            }

            if (paidAmount < 0)
                return Result<Invoice>.Fail("Ödənilən məbləğ mənfi ola bilməz.");

            if (type == InvoiceType.StockIn && !parentInvoiceId.HasValue)
            {
                var localPurchaseEnabled = await _settingsService.GetLocalPurchaseBoolAsync(
                    "EnableLocalPurchaseInvoice",
                    defaultValue: true);

                if (!localPurchaseEnabled.IsSuccess)
                    return Result<Invoice>.Fail(localPurchaseEnabled.Message);

                if (!localPurchaseEnabled.Data)
                    return Result<Invoice>.Fail("Yerli alış qaiməsi ayarlardan deaktiv edilib.");
            }

            var invoiceNumberResult = await GenerateInvoiceNumberAsync(type);

            if (!invoiceNumberResult.IsSuccess || string.IsNullOrWhiteSpace(invoiceNumberResult.Data))
                return Result<Invoice>.Fail(invoiceNumberResult.Message);

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumberResult.Data,
                Type = type,
                Status = InvoiceStatus.Draft,
                SupplierId = type == InvoiceType.StockIn || type == InvoiceType.SupplierReturnOut ? supplierId : null,
                CustomerId = type == InvoiceType.StockOut || type == InvoiceType.CustomerReturnIn ? customerId : null,
                ParentInvoiceId = parentInvoiceId,
                InvoiceDate = DateTime.Now,

                Currency = CurrencyType.AZN,
                ExchangeRate = 1,

                TotalAmount = 0,
                ItemsTotalAmount = 0,
                ExtraExpenseAmount = 0,
                DiscountAmount = 0,
                NetItemsAmount = 0,
                NetAmount = 0,
                VatAmount = 0,
                GrossItemsAmount = 0,
                GrossAmount = 0,
                CostIncludedExpenseAmount = 0,
                CostIncludedTaxAmount = 0,
                RecoverableTaxAmount = 0,
                CostExcludedTaxAmount = 0,
                FinalCostAmount = 0,

                PaidAmount = paidAmount,
                DebtAmount = 0,

                PaymentStatus = paidAmount > 0 ? PaymentStatus.PartialPaid : PaymentStatus.Unpaid,
                PaymentType = PaymentType.Cash,

                CostStatus = CostRecalculationStatus.NotCalculated,
                IsLocked = false,

                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();

            return Result<Invoice>.Success(invoice, "Draft qaimə yaradıldı.");
        }

        public async Task<Result<InvoiceItem>> AddItemAsync(
            int invoiceId,
            int productId,
            int shelfId,
            decimal quantity,
            decimal price,
            decimal discountPercent = 0,
            decimal discountAmount = 0)
        {
            if (invoiceId <= 0)
                return Result<InvoiceItem>.Fail("Qaimə düzgün seçilməyib.");

            if (productId <= 0)
                return Result<InvoiceItem>.Fail("Məhsul düzgün seçilməyib.");

            if (shelfId <= 0)
                return Result<InvoiceItem>.Fail("Rəf düzgün seçilməyib.");

            if (quantity <= 0)
                return Result<InvoiceItem>.Fail("Miqdar 0-dan böyük olmalıdır.");

            if (price < 0)
                return Result<InvoiceItem>.Fail("Qiymət mənfi ola bilməz.");

            if (discountPercent < 0 || discountPercent > 100)
                return Result<InvoiceItem>.Fail("Endirim faizi 0-100 aralığında olmalıdır.");

            if (discountAmount < 0)
                return Result<InvoiceItem>.Fail("Endirim məbləği mənfi ola bilməz.");

            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<InvoiceItem>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<InvoiceItem>.Fail("Yalnız draft qaiməyə məhsul əlavə etmək olar.");

            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);

            if (product == null)
                return Result<InvoiceItem>.Fail("Məhsul tapılmadı.");

            var shelfExists = await _context.Shelves.AnyAsync(x => x.Id == shelfId && x.IsActive);

            if (!shelfExists)
                return Result<InvoiceItem>.Fail("Rəf tapılmadı.");

            var existingItem = invoice.Items
                .FirstOrDefault(x => x.ProductId == productId && x.ShelfId == shelfId && x.IsActive);

            var newTotalQuantity = existingItem == null
                ? quantity
                : existingItem.Quantity + quantity;

            if (invoice.Type == InvoiceType.StockOut)
            {
                var stockCheck = await _stockService.HasEnoughStockAsync(productId, shelfId, newTotalQuantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                    return Result<InvoiceItem>.Fail(stockCheck.Message);

                var fifoCheck = await _stockService.HasEnoughFifoBatchStockAsync(productId, shelfId, newTotalQuantity);

                if (!fifoCheck.IsSuccess || !fifoCheck.Data)
                    return Result<InvoiceItem>.Fail(fifoCheck.Message);
            }

            if (invoice.Type == InvoiceType.StockIn)
            {
                var simulatedItems = invoice.Items
                    .Where(x => x.IsActive)
                    .Select(x => new InvoiceItem
                    {
                        ProductId = x.ProductId,
                        ShelfId = x.ShelfId,
                        Quantity = x.ProductId == productId && x.ShelfId == shelfId ? newTotalQuantity : x.Quantity,
                        IsActive = true
                    })
                    .ToList();

                if (existingItem == null)
                {
                    simulatedItems.Add(new InvoiceItem
                    {
                        ProductId = productId,
                        ShelfId = shelfId,
                        Quantity = quantity,
                        IsActive = true
                    });
                }

                var capacityCheck = await _stockService.HasEnoughShelfCapacityForItemsAsync(simulatedItems);

                if (!capacityCheck.IsSuccess || !capacityCheck.Data)
                    return Result<InvoiceItem>.Fail(capacityCheck.Message);
            }

            if (invoice.Type == InvoiceType.CustomerReturnIn)
            {
                if (!invoice.ParentInvoiceId.HasValue)
                    return Result<InvoiceItem>.Fail("Geri qaytarma üçün əsas satış qaiməsi seçilməyib.");

                var productWasSold = await _context.StockMovements.AnyAsync(x =>
                    x.InvoiceId == invoice.ParentInvoiceId.Value &&
                    x.ProductId == productId &&
                    x.MovementType == StockMovementType.StockOut &&
                    x.IsActive);

                if (!productWasSold)
                    return Result<InvoiceItem>.Fail("Bu məhsul əsas satış qaiməsində tapılmadı.");
            }

            if (invoice.Type == InvoiceType.SupplierReturnOut)
            {
                if (!invoice.ParentInvoiceId.HasValue)
                    return Result<InvoiceItem>.Fail("Geri qaytarma üçün əsas giriş qaiməsi seçilməyib.");

                var parentBatchExists = await _context.StockBatches.AnyAsync(x =>
                    x.SourceInvoiceId == invoice.ParentInvoiceId.Value &&
                    x.ProductId == productId &&
                    x.ShelfId == shelfId &&
                    x.RemainingQuantity > 0 &&
                    x.IsActive);

                if (!parentBatchExists)
                    return Result<InvoiceItem>.Fail("Bu məhsul/rəf üzrə əsas giriş qaiməsinə aid qalan partiya tapılmadı.");

                var stockCheck = await _stockService.HasEnoughStockAsync(productId, shelfId, newTotalQuantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                    return Result<InvoiceItem>.Fail(stockCheck.Message);
            }

            var exchangeRate = invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;
            var currency = invoice.Currency == 0 ? CurrencyType.AZN : invoice.Currency;

            if (existingItem != null)
            {
                existingItem.ProductBarcode = product.Barcode;
                existingItem.ProductNameSnapshot = product.Name;
                existingItem.ProductCodeSnapshot = product.Code;

                existingItem.Currency = currency;
                existingItem.ExchangeRate = exchangeRate;

                ApplyItemAmounts(
                    invoice,
                    existingItem,
                    price,
                    newTotalQuantity,
                    discountPercent,
                    discountAmount);

                existingItem.CostStatusReset();
                existingItem.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await RecalculateInvoiceTotalsAsync(invoiceId);

                return Result<InvoiceItem>.Success(existingItem, "Məhsul qaimədə mövcud idi, miqdar artırıldı.");
            }

            var item = new InvoiceItem
            {
                InvoiceId = invoiceId,
                ProductId = productId,
                ShelfId = shelfId,
                ProductBarcode = product.Barcode,
                ProductNameSnapshot = product.Name,
                ProductCodeSnapshot = product.Code,

                Quantity = quantity,
                Price = price,

                Currency = currency,
                ExchangeRate = exchangeRate,
                OriginalUnitPrice = price,

                IsVatApplicable = false,
                VatRate = 0,
                IsVatIncludedInPrice = false,
                IsVatIncludedInCost = false,

                ExpenseUnitShare = 0,
                TaxUnitShare = 0,
                DiscountUnitShare = 0,
                FinalUnitCost = 0,
                FinalTotalCost = 0,

                CreatedAt = DateTime.Now,
                IsActive = true
            };

            ApplyItemAmounts(
                invoice,
                item,
                price,
                quantity,
                discountPercent,
                discountAmount);

            await _context.InvoiceItems.AddAsync(item);
            await _context.SaveChangesAsync();

            await RecalculateInvoiceTotalsAsync(invoiceId);

            return Result<InvoiceItem>.Success(item, "Məhsul qaiməyə əlavə edildi.");
        }

        public async Task<Result<bool>> RemoveItemAsync(int invoiceId, int itemId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<bool>.Fail("Yalnız draft qaimədən məhsul silmək olar.");

            var item = await _context.InvoiceItems
                .Include(x => x.Taxes.Where(t => t.IsActive))
                .FirstOrDefaultAsync(x => x.Id == itemId && x.InvoiceId == invoiceId && x.IsActive);

            if (item == null)
                return Result<bool>.Fail("Qaimə məhsulu tapılmadı.");

            item.IsActive = false;
            item.UpdatedAt = DateTime.Now;

            foreach (var tax in item.Taxes)
            {
                tax.IsActive = false;
                tax.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await RecalculateInvoiceTotalsAsync(invoiceId);

            return Result<bool>.Success(true, "Məhsul qaimədən silindi.");
        }

        public async Task<Result<bool>> ConfirmAsync(int invoiceId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.Now;

                var invoice = await _context.Invoices
                    .Include(x => x.Items.Where(i => i.IsActive))
                    .Include(x => x.Expenses.Where(e => e.IsActive))
                    .Include(x => x.Taxes.Where(t => t.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<bool>.Fail("Yalnız draft qaimə təsdiqlənə bilər.");

                if (invoice.Items == null || !invoice.Items.Any())
                    return Result<bool>.Fail("Qaimədə məhsul yoxdur.");

                var alreadyApplied = await _context.StockMovements.AnyAsync(x => x.InvoiceId == invoice.Id && x.IsActive);

                if (alreadyApplied)
                    return Result<bool>.Fail("Bu qaimə üzrə stok əməliyyatı artıq icra olunub.");

                var validation = await ValidateInvoiceBeforeConfirmAsync(invoice);

                if (!validation.IsSuccess)
                    return Result<bool>.Fail(validation.Message);

                foreach (var item in invoice.Items)
                {
                    NormalizeInvoiceItemAmounts(invoice, item);
                    item.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();

                var taxResult = await _taxCalculationService.RebuildInvoiceItemTaxesAsync(invoice.Id);

                if (!taxResult.IsSuccess)
                    return Result<bool>.Fail(taxResult.Message);

                var taxAllocationResult = await _taxAllocationService.RebuildTaxAllocationsAsync(invoice.Id);

                if (!taxAllocationResult.IsSuccess)
                    return Result<bool>.Fail(taxAllocationResult.Message);

                var expenseAllocationResult = await _expenseAllocationService.RebuildExpenseAllocationsAsync(invoice.Id);

                if (!expenseAllocationResult.IsSuccess)
                    return Result<bool>.Fail(expenseAllocationResult.Message);

                if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
                {
                    var autoCostResult = await _settingsService.GetLocalPurchaseBoolAsync(
                        "AutoCalculateCostOnConfirm",
                        defaultValue: true);

                    if (!autoCostResult.IsSuccess)
                        return Result<bool>.Fail(autoCostResult.Message);

                    if (!autoCostResult.Data)
                        return Result<bool>.Fail("Yerli alışda təsdiq zamanı avtomatik maya hesablanması deaktivdir. Əvvəl maya dəyərini manual hesablayın.");
                }

                var costResult = await _costCalculationService.RecalculateInvoiceCostAsync(invoice.Id);

                if (!costResult.IsSuccess)
                    return Result<bool>.Fail(costResult.Message);

                invoice = await _context.Invoices
                    .Include(x => x.Items.Where(i => i.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə yenidən yüklənmədi.");

                if (invoice.PaidAmount < 0)
                    return Result<bool>.Fail("Ödənilən məbləğ mənfi ola bilməz.");

                if (invoice.PaidAmount > invoice.TotalAmount)
                    invoice.PaidAmount = invoice.TotalAmount;

                invoice.DebtAmount = invoice.TotalAmount - invoice.PaidAmount;
                UpdatePaymentStatus(invoice);

                if (invoice.Type == InvoiceType.StockIn)
                {
                    var stockInResult = await ConfirmStockInAsync(invoice, now);

                    if (!stockInResult.IsSuccess)
                        return Result<bool>.Fail(stockInResult.Message);
                }
                else if (invoice.Type == InvoiceType.StockOut)
                {
                    var stockOutResult = await ConfirmStockOutAsync(invoice, now);

                    if (!stockOutResult.IsSuccess)
                        return Result<bool>.Fail(stockOutResult.Message);
                }
                else if (invoice.Type == InvoiceType.CustomerReturnIn)
                {
                    var customerReturnResult = await ConfirmCustomerReturnInAsync(invoice, now);

                    if (!customerReturnResult.IsSuccess)
                        return Result<bool>.Fail(customerReturnResult.Message);
                }
                else if (invoice.Type == InvoiceType.SupplierReturnOut)
                {
                    var supplierReturnResult = await ConfirmSupplierReturnOutAsync(invoice, now);

                    if (!supplierReturnResult.IsSuccess)
                        return Result<bool>.Fail(supplierReturnResult.Message);
                }
                else
                {
                    return Result<bool>.Fail("Qaimə tipi düzgün deyil.");
                }

                invoice.Status = InvoiceStatus.Confirmed;
                invoice.CostStatus = CostRecalculationStatus.Locked;
                invoice.IsLocked = true;
                invoice.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<bool>.Success(true, "Qaimə təsdiqləndi və enterprise vergi/maya/stok əməliyyatı uğurla icra olundu.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Qaimə təsdiqlənmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CancelAsync(int invoiceId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.Now;

                var invoice = await _context.Invoices
                    .Include(x => x.Supplier)
                    .Include(x => x.Customer)
                    .Include(x => x.Items.Where(i => i.IsActive))
                        .ThenInclude(x => x.Product)
                    .Include(x => x.Items.Where(i => i.IsActive))
                        .ThenInclude(x => x.Shelf)
                    .Include(x => x.StockMovements.Where(m => m.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə tapılmadı.");

                if (invoice.Status == InvoiceStatus.Cancelled)
                    return Result<bool>.Fail("Bu qaimə artıq ləğv edilib.");

                if (invoice.Status == InvoiceStatus.Draft)
                {
                    invoice.Status = InvoiceStatus.Cancelled;
                    invoice.UpdatedAt = now;
                    invoice.Note = AppendCancelNote(invoice.Note, "Draft qaimə ləğv edildi.");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<bool>.Success(true, "Draft qaimə ləğv edildi.");
                }

                if (invoice.Status == InvoiceStatus.Confirmed)
                {
                    return Result<bool>.Fail(
                        "FIFO aktiv olduğu üçün təsdiqlənmiş qaimə birbaşa ləğv edilmir. Geri qaytarma qaiməsi yaradın: satış üçün CustomerReturnIn, alış üçün SupplierReturnOut.");
                }

                return Result<bool>.Fail("Yalnız Draft statusunda olan qaimə birbaşa ləğv edilə bilər.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Qaimə ləğv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceExpense>> AddExpenseAsync(
            int invoiceId,
            int? expenseTypeId,
            string? customName,
            decimal amount,
            ExpenseDirection direction,
            bool affectStockCost,
            string? note,
            Dictionary<string, string?>? fieldValues = null)
        {
            if (invoiceId <= 0)
                return Result<InvoiceExpense>.Fail("Qaimə düzgün seçilməyib.");

            if (amount <= 0)
                return Result<InvoiceExpense>.Fail("Xərc məbləği 0-dan böyük olmalıdır.");

            var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<InvoiceExpense>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<InvoiceExpense>.Fail("Yalnız draft qaiməyə xərc əlavə etmək olar.");

            if (invoice.Type == InvoiceType.CustomerReturnIn || invoice.Type == InvoiceType.SupplierReturnOut)
                return Result<InvoiceExpense>.Fail("Geri qaytarma qaiməsinə əlavə xərc əlavə edilmir.");

            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var expensesEnabled = await _settingsService.GetLocalPurchaseBoolAsync(
                    "EnableAdditionalExpenses",
                    defaultValue: true);

                if (!expensesEnabled.IsSuccess)
                    return Result<InvoiceExpense>.Fail(expensesEnabled.Message);

                if (!expensesEnabled.Data)
                    return Result<InvoiceExpense>.Fail("Yerli alışda əlavə xərclər ayarlardan deaktiv edilib.");
            }

            ExpenseType? expenseType = null;

            if (expenseTypeId.HasValue && expenseTypeId.Value > 0)
            {
                expenseType = await _context.ExpenseTypes
                    .Include(x => x.FieldDefinitions.Where(f => f.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == expenseTypeId.Value && x.IsActive);

                if (expenseType == null)
                    return Result<InvoiceExpense>.Fail("Xərc növü tapılmadı.");

                if (invoice.Type == InvoiceType.StockIn && !expenseType.UseForStockIn)
                    return Result<InvoiceExpense>.Fail("Bu xərc növü giriş qaiməsi üçün aktiv deyil.");

                if (invoice.Type == InvoiceType.StockOut && !expenseType.UseForStockOut)
                    return Result<InvoiceExpense>.Fail("Bu xərc növü çıxış qaiməsi üçün aktiv deyil.");
            }

            var expenseName = expenseType?.Name ?? (customName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(expenseName))
                return Result<InvoiceExpense>.Fail("Xərc adı boş ola bilməz.");

            var exchangeRate = invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;
            var currency = invoice.Currency == 0 ? CurrencyType.AZN : invoice.Currency;

            var expense = new InvoiceExpense
            {
                InvoiceId = invoiceId,
                ExpenseTypeId = expenseType?.Id,
                Name = expenseName,
                Amount = amount,
                OriginalAmount = amount,
                Currency = currency,
                ExchangeRate = exchangeRate,
                LocalAmount = currency == CurrencyType.AZN ? amount : amount * exchangeRate,

                Direction = direction,
                AffectStockCost = affectStockCost || (expenseType?.AffectStockCost ?? false),
                IsImportExpense = invoice.IsImport || (expenseType?.UseForImport ?? false),
                AllocationMethod = expenseType?.DefaultAllocationMethod ?? await GetDefaultLocalPurchaseAllocationMethodAsync(invoice),
                ShouldAllocateToItems = affectStockCost || (expenseType?.AffectStockCost ?? false),
                IsTaxRelated = expenseType?.IsTaxRelated ?? false,
                IncludeZeroAmountInCost = expenseType?.IncludeZeroAmountInCost ?? false,

                Note = note?.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            if (fieldValues != null && fieldValues.Any())
                AddExpenseFieldValues(expense, expenseType, fieldValues);

            await _context.InvoiceExpenses.AddAsync(expense);
            await _context.SaveChangesAsync();

            await RecalculateInvoiceTotalsAsync(invoiceId);

            return Result<InvoiceExpense>.Success(expense, "Xərc qaiməyə əlavə edildi.");
        }

        public async Task<Result<bool>> UpdateExpenseFieldsAsync(
            int invoiceId,
            int expenseId,
            Dictionary<string, string?> fieldValues)
        {
            var expense = await _context.InvoiceExpenses
                .Include(x => x.Invoice)
                .Include(x => x.ExpenseType)
                    .ThenInclude(x => x.FieldDefinitions.Where(f => f.IsActive))
                .Include(x => x.FieldValues.Where(f => f.IsActive))
                .FirstOrDefaultAsync(x => x.Id == expenseId && x.InvoiceId == invoiceId && x.IsActive);

            if (expense == null)
                return Result<bool>.Fail("Xərc tapılmadı.");

            if (expense.Invoice.Status != InvoiceStatus.Draft)
                return Result<bool>.Fail("Yalnız draft qaimədə xərc detalları dəyişdirilə bilər.");

            foreach (var oldValue in expense.FieldValues.Where(x => x.IsActive))
            {
                oldValue.IsActive = false;
                oldValue.UpdatedAt = DateTime.Now;
            }

            AddExpenseFieldValues(expense, expense.ExpenseType, fieldValues);

            expense.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Xərc detalları yadda saxlanıldı.");
        }

        public async Task<Result<bool>> RemoveExpenseAsync(int invoiceId, int expenseId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<bool>.Fail("Yalnız draft qaimədən xərc silmək olar.");

            var expense = await _context.InvoiceExpenses
                .Include(x => x.FieldValues.Where(f => f.IsActive))
                .FirstOrDefaultAsync(x => x.Id == expenseId && x.InvoiceId == invoiceId && x.IsActive);

            if (expense == null)
                return Result<bool>.Fail("Xərc tapılmadı.");

            expense.IsActive = false;
            expense.UpdatedAt = DateTime.Now;

            foreach (var field in expense.FieldValues)
            {
                field.IsActive = false;
                field.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await RecalculateInvoiceTotalsAsync(invoiceId);

            return Result<bool>.Success(true, "Xərc qaimədən silindi.");
        }

        private async Task<Result<bool>> ValidateInvoiceBeforeConfirmAsync(Invoice invoice)
        {
            foreach (var item in invoice.Items)
            {
                if (item.ProductId <= 0)
                    return Result<bool>.Fail("Qaimədə məhsul düzgün seçilməyib.");

                if (item.ShelfId <= 0)
                    return Result<bool>.Fail("Qaimədə rəf düzgün seçilməyib.");

                if (item.Quantity <= 0)
                    return Result<bool>.Fail("Qaimədə miqdar 0-dan böyük olmalıdır.");

                if (item.Price < 0)
                    return Result<bool>.Fail("Qaimədə qiymət mənfi ola bilməz.");

                if (item.DiscountPercent < 0 || item.DiscountPercent > 100)
                    return Result<bool>.Fail("Qaimə item endirim faizi 0-100 aralığında olmalıdır.");

                if (item.DiscountAmount < 0)
                    return Result<bool>.Fail("Qaimə item endirim məbləği mənfi ola bilməz.");
            }

            return await Task.FromResult(Result<bool>.Success(true));
        }

        private async Task<Result<bool>> ConfirmStockInAsync(Invoice invoice, DateTime now)
        {
            if (!invoice.SupplierId.HasValue)
                return Result<bool>.Fail("Giriş qaiməsi üçün təchizatçı seçilməlidir.");

            var supplierExists = await _context.Suppliers
                .AnyAsync(x => x.Id == invoice.SupplierId.Value && x.IsActive);

            if (!supplierExists)
                return Result<bool>.Fail("Təchizatçı tapılmadı.");

            foreach (var item in invoice.Items)
            {
                var capacityCheck = await _stockService.HasEnoughShelfCapacityAsync(
                    item.ProductId,
                    item.ShelfId,
                    item.Quantity);

                if (!capacityCheck.IsSuccess || !capacityCheck.Data)
                    return Result<bool>.Fail(capacityCheck.Message);
            }

            var stockInResult = await _stockService.ApplyStockInAsync(invoice);

            if (!stockInResult.IsSuccess)
                return Result<bool>.Fail(stockInResult.Message);

            var balanceResult = await _supplierBalanceService.ApplyInvoiceDebtAsync(
                invoice.Id,
                saveChanges: false);

            if (!balanceResult.IsSuccess)
                return Result<bool>.Fail(balanceResult.Message);

            return Result<bool>.Success(true);
        }

        private async Task<Result<bool>> ConfirmStockOutAsync(Invoice invoice, DateTime now)
        {
            if (!invoice.CustomerId.HasValue)
                return Result<bool>.Fail("Çıxış qaiməsi üçün müştəri seçilməlidir.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == invoice.CustomerId.Value && x.IsActive);

            if (customer == null)
                return Result<bool>.Fail("Müştəri tapılmadı.");

            foreach (var item in invoice.Items)
            {
                var stockCheck = await _stockService.HasEnoughStockAsync(
                    item.ProductId,
                    item.ShelfId,
                    item.Quantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                    return Result<bool>.Fail(stockCheck.Message);

                var fifoCheck = await _stockService.HasEnoughFifoBatchStockAsync(
                    item.ProductId,
                    item.ShelfId,
                    item.Quantity);

                if (!fifoCheck.IsSuccess || !fifoCheck.Data)
                    return Result<bool>.Fail(fifoCheck.Message);
            }

            if (invoice.DebtAmount > 0)
            {
                var newDebt = customer.DebtAmount + invoice.DebtAmount;

                if (customer.CreditLimit > 0 && newDebt > customer.CreditLimit)
                {
                    return Result<bool>.Fail(
                        $"Müştərinin kredit limiti aşılır. Limit: {customer.CreditLimit:N2} AZN, cari borc: {customer.DebtAmount:N2} AZN, bu qaimə borcu: {invoice.DebtAmount:N2} AZN, yeni borc: {newDebt:N2} AZN.");
                }
            }

            var stockOutResult = await _stockService.ApplyStockOutAsync(invoice);

            if (!stockOutResult.IsSuccess)
                return Result<bool>.Fail(stockOutResult.Message);

            var balanceResult = await _customerBalanceService.ApplyInvoiceDebtAsync(
                invoice.Id,
                saveChanges: false);

            if (!balanceResult.IsSuccess)
                return Result<bool>.Fail(balanceResult.Message);

            return Result<bool>.Success(true);
        }

        private async Task<Result<bool>> ConfirmCustomerReturnInAsync(Invoice invoice, DateTime now)
        {
            if (!invoice.ParentInvoiceId.HasValue)
                return Result<bool>.Fail("Müştəridən geri qaytarma üçün əsas satış qaiməsi seçilməlidir.");

            var parentSale = await _context.Invoices.FirstOrDefaultAsync(x =>
                x.Id == invoice.ParentInvoiceId.Value &&
                x.IsActive &&
                x.Type == InvoiceType.StockOut &&
                x.Status == InvoiceStatus.Confirmed);

            if (parentSale == null)
                return Result<bool>.Fail("Əsas satış qaiməsi tapılmadı.");

            if (parentSale.CustomerId.HasValue)
                invoice.CustomerId = parentSale.CustomerId;

            var returnResult = await _stockService.ApplyCustomerReturnInAsync(invoice);

            if (!returnResult.IsSuccess)
                return Result<bool>.Fail(returnResult.Message);

            var balanceResult = await _customerBalanceService.ApplyInvoiceDebtAsync(
                invoice.Id,
                saveChanges: false);

            if (!balanceResult.IsSuccess)
                return Result<bool>.Fail(balanceResult.Message);

            return Result<bool>.Success(true);
        }

        private async Task<Result<bool>> ConfirmSupplierReturnOutAsync(Invoice invoice, DateTime now)
        {
            if (!invoice.ParentInvoiceId.HasValue)
                return Result<bool>.Fail("Təchizatçıya geri qaytarma üçün əsas giriş qaiməsi seçilməlidir.");

            var parentStockIn = await _context.Invoices.FirstOrDefaultAsync(x =>
                x.Id == invoice.ParentInvoiceId.Value &&
                x.IsActive &&
                x.Type == InvoiceType.StockIn &&
                x.Status == InvoiceStatus.Confirmed);

            if (parentStockIn == null)
                return Result<bool>.Fail("Əsas giriş qaiməsi tapılmadı.");

            if (parentStockIn.SupplierId.HasValue)
                invoice.SupplierId = parentStockIn.SupplierId;

            var supplierReturnResult = await _stockService.ApplySupplierReturnOutAsync(invoice);

            if (!supplierReturnResult.IsSuccess)
                return Result<bool>.Fail(supplierReturnResult.Message);

            var balanceResult = await _supplierBalanceService.ApplyInvoiceDebtAsync(
                invoice.Id,
                saveChanges: false);

            if (!balanceResult.IsSuccess)
                return Result<bool>.Fail(balanceResult.Message);

            return Result<bool>.Success(true);
        }

        private async Task<Result<string>> GenerateInvoiceNumberAsync(InvoiceType type)
        {
            try
            {
                var prefix = type switch
                {
                    InvoiceType.StockIn => "GIN",
                    InvoiceType.StockOut => "CIX",
                    InvoiceType.CustomerReturnIn => "MQR",
                    InvoiceType.SupplierReturnOut => "TQR",
                    _ => "QAI"
                };

                var datePart = DateTime.Now.ToString("yyyyMMdd");

                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(1);

                var todayCount = await _context.Invoices
                    .Where(x => x.Type == type && x.CreatedAt >= startDate && x.CreatedAt < endDate)
                    .CountAsync();

                var nextNumber = todayCount + 1;

                while (true)
                {
                    var invoiceNumber = $"{prefix}-{datePart}-{nextNumber:0000}";

                    var exists = await _context.Invoices.AnyAsync(x => x.InvoiceNumber == invoiceNumber);

                    if (!exists)
                        return Result<string>.Success(invoiceNumber, "Qaimə nömrəsi yaradıldı.");

                    nextNumber++;
                }
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Qaimə nömrəsi yaradılmadı: {ex.Message}");
            }
        }

        private static string AppendCancelNote(string? oldNote, string cancelMessage)
        {
            var nowText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var newNote = $"[{nowText}] {cancelMessage}";

            if (string.IsNullOrWhiteSpace(oldNote))
                return newNote;

            return oldNote + Environment.NewLine + newNote;
        }

        private async Task<CostAllocationMethod> GetDefaultLocalPurchaseAllocationMethodAsync(Invoice invoice)
        {
            if (invoice.Type == InvoiceType.StockIn && !invoice.IsImport)
            {
                var methodResult = await _settingsService.GetLocalPurchaseAllocationMethodAsync();

                if (methodResult.IsSuccess)
                    return methodResult.Data;
            }

            return CostAllocationMethod.ByAmount;
        }

        private async Task RecalculateInvoiceTotalsAsync(int invoiceId, bool saveChanges = true)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                .Include(x => x.Expenses.Where(e => e.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return;

            foreach (var item in invoice.Items)
                NormalizeInvoiceItemAmounts(invoice, item);

            invoice.ItemsTotalAmount = invoice.Items.Sum(x => x.LocalTotalAmount);
            invoice.LocalItemsTotalAmount = invoice.ItemsTotalAmount;
            invoice.OriginalItemsTotalAmount = invoice.Items.Sum(x => x.OriginalTotalAmount);

            invoice.DiscountAmount =
                invoice.Items.Sum(x => x.DiscountAmount)
                + invoice.Expenses
                    .Where(x => x.Direction == ExpenseDirection.Minus)
                    .Sum(x => x.LocalAmount > 0 ? x.LocalAmount : x.Amount);

            invoice.NetItemsAmount = invoice.Items.Sum(x => x.NetAmount);
            invoice.NetAmount = invoice.NetItemsAmount;

            invoice.VatAmount = invoice.Items.Sum(x => x.VatAmount);

            invoice.GrossItemsAmount = invoice.Items.Sum(x => x.GrossAmount);
            invoice.GrossAmount = invoice.GrossItemsAmount;

            invoice.ExtraExpenseAmount = invoice.Expenses
                .Where(x => x.Direction == ExpenseDirection.Plus)
                .Sum(x => x.LocalAmount > 0 ? x.LocalAmount : x.Amount);

            invoice.TotalAmount = invoice.GrossItemsAmount + invoice.ExtraExpenseAmount;

            if (invoice.TotalAmount < 0)
                invoice.TotalAmount = 0;

            if (invoice.PaidAmount < 0)
                invoice.PaidAmount = 0;

            if (invoice.PaidAmount > invoice.TotalAmount)
                invoice.PaidAmount = invoice.TotalAmount;

            invoice.DebtAmount = invoice.TotalAmount - invoice.PaidAmount;
            invoice.LocalTotalAmount = invoice.TotalAmount;

            invoice.OriginalTotalAmount = invoice.Currency == CurrencyType.AZN
                ? invoice.LocalTotalAmount
                : invoice.LocalTotalAmount / (invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate);

            UpdatePaymentStatus(invoice);

            invoice.CostStatus = CostRecalculationStatus.NeedsRecalculation;
            invoice.UpdatedAt = DateTime.Now;

            if (saveChanges)
                await _context.SaveChangesAsync();
        }

        private static void NormalizeInvoiceItemAmounts(Invoice invoice, InvoiceItem item)
        {
            var exchangeRate = item.ExchangeRate > 0
                ? item.ExchangeRate
                : invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;

            var currency = item.Currency == 0
                ? invoice.Currency
                : item.Currency;

            if (currency == 0)
                currency = CurrencyType.AZN;

            item.Currency = currency;
            item.ExchangeRate = exchangeRate;

            if (item.OriginalUnitPrice <= 0)
                item.OriginalUnitPrice = item.Price;

            ApplyItemAmounts(
                invoice,
                item,
                item.OriginalUnitPrice,
                item.Quantity,
                item.DiscountPercent,
                item.DiscountAmount);
        }

        private static void ApplyItemAmounts(
            Invoice invoice,
            InvoiceItem item,
            decimal unitPrice,
            decimal quantity,
            decimal discountPercent,
            decimal discountAmount)
        {
            var exchangeRate = item.ExchangeRate > 0
                ? item.ExchangeRate
                : invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;

            var currency = item.Currency == 0
                ? invoice.Currency
                : item.Currency;

            if (currency == 0)
                currency = CurrencyType.AZN;

            if (quantity <= 0)
                quantity = item.Quantity;

            if (unitPrice < 0)
                unitPrice = 0;

            if (discountPercent < 0)
                discountPercent = 0;

            if (discountPercent > 100)
                discountPercent = 100;

            if (discountAmount < 0)
                discountAmount = 0;

            var originalGrossAmount = Math.Round(unitPrice * quantity, 2);
            var percentDiscountAmount = Math.Round(originalGrossAmount * discountPercent / 100m, 2);

            var finalOriginalDiscountAmount = discountAmount > 0
                ? discountAmount
                : percentDiscountAmount;

            if (finalOriginalDiscountAmount > originalGrossAmount)
                finalOriginalDiscountAmount = originalGrossAmount;

            var originalNetAmount = originalGrossAmount - finalOriginalDiscountAmount;

            var localUnitPrice = currency == CurrencyType.AZN
                ? unitPrice
                : Math.Round(unitPrice * exchangeRate, 4);

            var localGrossAmount = Math.Round(localUnitPrice * quantity, 2);

            var localDiscountAmount = currency == CurrencyType.AZN
                ? finalOriginalDiscountAmount
                : Math.Round(finalOriginalDiscountAmount * exchangeRate, 2);

            if (localDiscountAmount > localGrossAmount)
                localDiscountAmount = localGrossAmount;

            var localNetAmount = localGrossAmount - localDiscountAmount;

            item.Quantity = quantity;
            item.Price = unitPrice;

            item.Currency = currency;
            item.ExchangeRate = exchangeRate;

            item.OriginalUnitPrice = unitPrice;
            item.OriginalTotalAmount = originalGrossAmount;

            item.LocalUnitPrice = localUnitPrice;
            item.LocalTotalAmount = localGrossAmount;

            item.DiscountPercent = discountPercent;
            item.DiscountAmount = localDiscountAmount;

            item.DiscountUnitShare = quantity > 0
                ? Math.Round(localDiscountAmount / quantity, 4)
                : 0;

            item.NetAmount = localNetAmount;
            item.VatAmount = 0;
            item.GrossAmount = localNetAmount;

            item.Total = originalNetAmount;
        }

        private static void UpdatePaymentStatus(Invoice invoice)
        {
            if (invoice.TotalAmount <= 0)
            {
                invoice.PaymentStatus = PaymentStatus.Paid;
                invoice.DebtAmount = 0;
                return;
            }

            if (invoice.PaidAmount <= 0)
            {
                invoice.PaymentStatus = PaymentStatus.Unpaid;
                invoice.DebtAmount = invoice.TotalAmount;
                return;
            }

            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.PaymentStatus = PaymentStatus.Paid;
                invoice.PaidAmount = invoice.TotalAmount;
                invoice.DebtAmount = 0;
                return;
            }

            invoice.PaymentStatus = PaymentStatus.PartialPaid;
            invoice.DebtAmount = invoice.TotalAmount - invoice.PaidAmount;
        }

        private static void AddExpenseFieldValues(
            InvoiceExpense expense,
            ExpenseType? expenseType,
            Dictionary<string, string?> fieldValues)
        {
            var definitions = expenseType?.FieldDefinitions?.Where(x => x.IsActive).ToList()
                ?? new List<ExpenseTypeFieldDefinition>();

            var sortOrder = 1;

            foreach (var pair in fieldValues)
            {
                var key = (pair.Key ?? string.Empty).Trim();
                var value = pair.Value?.Trim();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    continue;

                var definition = definitions.FirstOrDefault(x =>
                    x.FieldKey.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                    x.Label.Equals(key, StringComparison.OrdinalIgnoreCase));

                expense.FieldValues.Add(new InvoiceExpenseFieldValue
                {
                    ExpenseTypeFieldDefinitionId = definition?.Id,
                    FieldKey = definition?.FieldKey ?? key,
                    Label = definition?.Label ?? key,
                    Value = value,
                    IsCustom = definition == null,
                    SortOrder = definition?.SortOrder ?? sortOrder,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                });

                sortOrder++;
            }
        }
    }

    internal static class InvoiceItemCostResetExtensions
    {
        public static void CostStatusReset(this InvoiceItem item)
        {
            item.ExpenseUnitShare = 0;
            item.TaxUnitShare = 0;
            item.DiscountUnitShare = 0;
            item.FinalUnitCost = 0;
            item.FinalTotalCost = 0;
        }
    }
}