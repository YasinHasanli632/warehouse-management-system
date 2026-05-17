using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Enterprise hesabat servisi.
    // Bu servis yalnız UI/report üçün data hazırlayır.
    // Stok dəyişmir, qaimə təsdiqləmir, cost hesablamır.
    // Excel çıxarışı ayrıca ExcelExportService tərəfindən edilməlidir.
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<StockReportDto>>> GetStockReportAsync(
            string? search = null,
            int? categoryId = null,
            int? warehouseId = null,
            bool onlyCritical = false,
            bool onlyInStock = false)
        {
            try
            {
                var query = _context.Products
                    .Include(x => x.Category)
                    .Include(x => x.Attributes.Where(a => a.IsActive))
                        .ThenInclude(x => x.AttributeValue)
                            .ThenInclude(x => x.AttributeDefinition)
                    .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                        .ThenInclude(x => x.Shelf)
                            .ThenInclude(x => x.Warehouse)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.Name.ToLower().Contains(search) ||
                        x.Code.ToLower().Contains(search) ||
                        (x.Barcode != null && x.Barcode.ToLower().Contains(search)));
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                    query = query.Where(x => x.CategoryId == categoryId.Value);

                var products = await query.OrderBy(x => x.Name).ToListAsync();

                var result = products.Select(product =>
                {
                    var activeShelfStocks = product.ShelfStocks
                        .Where(x => x.IsActive && (!warehouseId.HasValue || x.Shelf.WarehouseId == warehouseId.Value))
                        .ToList();

                    var totalQuantity = activeShelfStocks.Sum(x => x.Quantity);

                    return new StockReportDto
                    {
                        ProductId = product.Id,
                        ProductCode = product.Code,
                        ProductName = product.Name,
                        Barcode = product.Barcode,
                        CategoryName = product.Category?.Name ?? "",
                        AttributesText = GetProductAttributesText(product),
                        Unit = product.Unit,
                        TotalQuantity = totalQuantity,
                        MinStockQuantity = product.MinStockQuantity,
                        ShelfCount = activeShelfStocks.Count(x => x.Quantity > 0),
                        IsCritical = totalQuantity <= product.MinStockQuantity,
                        PurchasePrice = product.PurchasePrice,
                        SalePrice = product.SalePrice,
                        LastCostPrice = product.LastCostPrice,
                        AverageCostPrice = product.AverageCostPrice,
                        StockValueByLastCost = Math.Round(totalQuantity * product.LastCostPrice, 2),
                        StockValueByAverageCost = Math.Round(totalQuantity * product.AverageCostPrice, 2),
                        ShelvesText = string.Join(", ", activeShelfStocks.Where(x => x.Quantity > 0).Select(x => x.Shelf.Code).Distinct()),
                        WarehousesText = string.Join(", ", activeShelfStocks.Where(x => x.Quantity > 0).Select(x => x.Shelf.Warehouse.Name).Distinct())
                    };
                }).ToList();

                if (onlyCritical)
                    result = result.Where(x => x.IsCritical).ToList();

                if (onlyInStock)
                    result = result.Where(x => x.TotalQuantity > 0).ToList();

                return Result<List<StockReportDto>>.Success(result, "Stok hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<StockReportDto>>.Fail($"Stok hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<StockReportDto>>> GetCriticalStockReportAsync()
        {
            var result = await GetStockReportAsync(onlyCritical: true);

            if (!result.IsSuccess || result.Data == null)
                return Result<List<StockReportDto>>.Fail(result.Message);

            return Result<List<StockReportDto>>.Success(
                result.Data.OrderBy(x => x.TotalQuantity).ToList(),
                "Kritik stok hesabatı yükləndi.");
        }

        public async Task<Result<List<InvoiceReportDto>>> GetInvoiceReportAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoiceType? type = null,
            InvoiceStatus? status = null,
            string? search = null,
            bool? isImport = null)
        {
            try
            {
                var query = _context.Invoices
                    .Include(x => x.Supplier)
                    .Include(x => x.Customer)
                    .Include(x => x.Items.Where(i => i.IsActive))
                        .ThenInclude(x => x.Product)
                            .ThenInclude(x => x.Category)
                    .Include(x => x.Items.Where(i => i.IsActive))
                        .ThenInclude(x => x.Product)
                            .ThenInclude(x => x.Attributes.Where(a => a.IsActive))
                                .ThenInclude(x => x.AttributeValue)
                                    .ThenInclude(x => x.AttributeDefinition)
                    .Include(x => x.Items.Where(i => i.IsActive))
                        .ThenInclude(x => x.Shelf)
                    .Include(x => x.CostSummary)
                    .Include(x => x.Taxes.Where(t => t.IsActive))
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(x => x.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.InvoiceDate.Date <= toDate.Value.Date);

                if (type.HasValue)
                    query = query.Where(x => x.Type == type.Value);

                if (status.HasValue)
                    query = query.Where(x => x.Status == status.Value);

                if (isImport.HasValue)
                    query = query.Where(x => x.IsImport == isImport.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.InvoiceNumber.ToLower().Contains(search) ||
                        (x.Supplier != null && x.Supplier.Name.ToLower().Contains(search)) ||
                        (x.Customer != null && x.Customer.Name.ToLower().Contains(search)) ||
                        (x.Note != null && x.Note.ToLower().Contains(search)));
                }

                var invoices = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

                var result = invoices.Select(x => new InvoiceReportDto
                {
                    InvoiceId = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.InvoiceDate,
                    Type = x.Type,
                    Status = x.Status,
                    IsImport = x.IsImport,
                    Currency = x.Currency,
                    ExchangeRate = x.ExchangeRate,
                    PartyName = ResolvePartyName(x),

                    TotalAmount = x.TotalAmount,
                    PaidAmount = x.PaidAmount,
                    DebtAmount = x.DebtAmount,
                    PaymentStatus = x.PaymentStatus,

                    ItemsTotalAmount = x.ItemsTotalAmount,
                    LocalItemsTotalAmount = x.LocalItemsTotalAmount,
                    OriginalItemsTotalAmount = x.OriginalItemsTotalAmount,

                    DiscountAmount = x.DiscountAmount,
                    NetItemsAmount = x.NetItemsAmount,
                    VatAmount = x.VatAmount,
                    GrossItemsAmount = x.GrossItemsAmount,

                    ExtraExpenseAmount = x.ExtraExpenseAmount,
                    CostIncludedExpenseAmount = x.CostIncludedExpenseAmount,
                    CostIncludedTaxAmount = x.CostIncludedTaxAmount,
                    RecoverableTaxAmount = x.RecoverableTaxAmount,
                    CostExcludedTaxAmount = x.CostExcludedTaxAmount,
                    FinalCostAmount = x.FinalCostAmount,
                    CostStatus = x.CostStatus,

                    ProductsText = string.Join(", ", x.Items
                        .Where(i => i.IsActive && i.Product != null)
                        .Select(i => i.Product!.Name)
                        .Distinct()),

                    CategoriesText = string.Join(", ", x.Items
                        .Where(i => i.IsActive && i.Product?.Category != null)
                        .Select(i => i.Product!.Category!.Name)
                        .Distinct()),

                    AttributesText = string.Join(" | ", x.Items
                        .Where(i => i.IsActive && i.Product != null)
                        .Select(i => GetProductAttributesText(i.Product!))
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .Distinct()),

                    ShelvesText = string.Join(", ", x.Items
                        .Where(i => i.IsActive && i.Shelf != null)
                        .Select(i => i.Shelf!.Code)
                        .Distinct()),

                    QuantityTotal = x.Items.Where(i => i.IsActive).Sum(i => i.Quantity),
                    TaxTotal = x.Taxes.Where(t => t.IsActive).Sum(t => t.LocalTaxAmount > 0 ? t.LocalTaxAmount : t.TaxAmount),
                    Note = x.Note
                }).ToList();

                return Result<List<InvoiceReportDto>>.Success(result, "Qaimə hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceReportDto>>.Fail($"Qaimə hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<MovementReportDto>>> GetMovementReportAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            StockMovementType? movementType = null,
            int? productId = null,
            int? shelfId = null,
            int? invoiceId = null)
        {
            try
            {
                var query = _context.StockMovements
                    .Include(x => x.Product)
                        .ThenInclude(x => x.Category)
                    .Include(x => x.Product)
                        .ThenInclude(x => x.Attributes.Where(a => a.IsActive))
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.AttributeDefinition)
                    .Include(x => x.Invoice)
                    .Include(x => x.StockBatch)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(x => x.CreatedAt.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.CreatedAt.Date <= toDate.Value.Date);

                if (movementType.HasValue)
                    query = query.Where(x => x.MovementType == movementType.Value);

                if (productId.HasValue && productId.Value > 0)
                    query = query.Where(x => x.ProductId == productId.Value);

                if (shelfId.HasValue && shelfId.Value > 0)
                    query = query.Where(x => x.FromShelfId == shelfId.Value || x.ToShelfId == shelfId.Value);

                if (invoiceId.HasValue && invoiceId.Value > 0)
                    query = query.Where(x => x.InvoiceId == invoiceId.Value);

                var movements = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

                var result = movements.Select(x => new MovementReportDto
                {
                    MovementId = x.Id,
                    Date = x.CreatedAt,
                    ProductId = x.ProductId,
                    ProductCode = x.Product.Code,
                    ProductName = x.Product.Name,
                    CategoryName = x.Product.Category?.Name ?? "",
                    AttributesText = GetProductAttributesText(x.Product),
                    MovementType = x.MovementType,
                    Quantity = x.Quantity,
                    FromShelfId = x.FromShelfId,
                    ToShelfId = x.ToShelfId,
                    InvoiceNumber = x.Invoice?.InvoiceNumber ?? "",
                    BatchNumber = x.StockBatch?.BatchNumber ?? "",
                    UnitCost = x.UnitCost,
                    TotalCost = x.TotalCost,
                    Currency = x.Currency,
                    ExchangeRate = x.ExchangeRate,
                    Note = x.Note
                }).ToList();

                return Result<List<MovementReportDto>>.Success(result, "Stok hərəkətləri hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<MovementReportDto>>.Fail($"Stok hərəkətləri hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<BatchReportDto>>> GetBatchReportAsync(
            int? productId = null,
            int? shelfId = null,
            bool onlyOpen = false,
            bool onlyWithRemaining = false,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.StockBatches
                    .Include(x => x.Product)
                        .ThenInclude(x => x.Category)
                    .Include(x => x.Product)
                        .ThenInclude(x => x.Attributes.Where(a => a.IsActive))
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.AttributeDefinition)
                    .Include(x => x.Shelf)
                        .ThenInclude(x => x.Warehouse)
                    .Include(x => x.SourceInvoice)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (productId.HasValue && productId.Value > 0)
                    query = query.Where(x => x.ProductId == productId.Value);

                if (shelfId.HasValue && shelfId.Value > 0)
                    query = query.Where(x => x.ShelfId == shelfId.Value);

                if (onlyOpen)
                    query = query.Where(x => !x.IsClosed);

                if (onlyWithRemaining)
                    query = query.Where(x => x.RemainingQuantity > 0);

                if (fromDate.HasValue)
                    query = query.Where(x => x.EntryDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.EntryDate.Date <= toDate.Value.Date);

                var batches = await query
                    .OrderBy(x => x.FifoDate)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

                var result = batches.Select(x => new BatchReportDto
                {
                    BatchId = x.Id,
                    BatchNumber = x.BatchNumber,
                    ProductId = x.ProductId,
                    ProductCode = x.Product.Code,
                    ProductName = x.Product.Name,
                    CategoryName = x.Product.Category?.Name ?? "",
                    AttributesText = GetProductAttributesText(x.Product),
                    ShelfCode = x.Shelf.Code,
                    WarehouseName = x.Shelf.Warehouse.Name,
                    SourceInvoiceNumber = x.SourceInvoice?.InvoiceNumber ?? "",
                    EntryDate = x.EntryDate,
                    FifoDate = x.FifoDate,
                    ProductionDate = x.ProductionDate,
                    ExpiryDate = x.ExpiryDate,
                    InitialQuantity = x.InitialQuantity,
                    RemainingQuantity = x.RemainingQuantity,
                    ConsumedQuantity = x.InitialQuantity - x.RemainingQuantity,
                    PurchaseUnitPrice = x.PurchaseUnitPrice,
                    LocalUnitPrice = x.LocalUnitPrice,
                    ExpenseUnitShare = x.ExpenseUnitShare,
                    TaxUnitShare = x.TaxUnitShare,
                    DiscountUnitShare = x.DiscountUnitShare,
                    FinalUnitCost = x.FinalUnitCost,
                    FinalTotalCost = x.FinalTotalCost,
                    RemainingCostValue = Math.Round(x.RemainingQuantity * x.FinalUnitCost, 2),
                    Currency = x.Currency,
                    ExchangeRate = x.ExchangeRate,
                    IsImportBatch = x.IsImportBatch,
                    IsClosed = x.IsClosed,
                    Note = x.Note
                }).ToList();

                return Result<List<BatchReportDto>>.Success(result, "FIFO batch hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<BatchReportDto>>.Fail($"FIFO batch hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<BatchProfitReportDto>>> GetBatchProfitReportAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? productId = null)
        {
            try
            {
                var query = _context.StockMovements
                    .Include(x => x.Product)
                        .ThenInclude(x => x.Category)
                    .Include(x => x.Invoice)
                        .ThenInclude(x => x.Items.Where(i => i.IsActive))
                    .Include(x => x.StockBatch)
                    .Where(x =>
                        x.IsActive &&
                        x.MovementType == StockMovementType.StockOut &&
                        x.Invoice != null &&
                        x.Invoice.Type == InvoiceType.StockOut)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(x => x.CreatedAt.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.CreatedAt.Date <= toDate.Value.Date);

                if (productId.HasValue && productId.Value > 0)
                    query = query.Where(x => x.ProductId == productId.Value);

                var movements = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

                var result = movements.Select(x =>
                {
                    var saleUnitPrice = ResolveSaleUnitPriceFromInvoice(x.Invoice!, x.ProductId);
                    var saleTotal = Math.Round(saleUnitPrice * x.Quantity, 2);
                    var costTotal = x.TotalCost > 0 ? x.TotalCost : Math.Round(x.UnitCost * x.Quantity, 2);
                    var profit = saleTotal - costTotal;

                    return new BatchProfitReportDto
                    {
                        Date = x.CreatedAt,
                        InvoiceNumber = x.Invoice?.InvoiceNumber ?? "",
                        BatchNumber = x.StockBatch?.BatchNumber ?? "",
                        ProductCode = x.Product.Code,
                        ProductName = x.Product.Name,
                        CategoryName = x.Product.Category?.Name ?? "",
                        Quantity = x.Quantity,
                        SaleUnitPrice = saleUnitPrice,
                        SaleTotal = saleTotal,
                        UnitCost = x.UnitCost,
                        CostTotal = costTotal,
                        Profit = profit,
                        ProfitPercent = saleTotal <= 0 ? 0 : Math.Round(profit / saleTotal * 100m, 2)
                    };
                }).ToList();

                return Result<List<BatchProfitReportDto>>.Success(result, "Batch profit hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<BatchProfitReportDto>>.Fail($"Batch profit hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<CostSummaryReportDto>>> GetCostSummaryReportAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoiceType? type = null)
        {
            try
            {
                var query = _context.Invoices
                    .Include(x => x.Supplier)
                    .Include(x => x.Customer)
                    .Include(x => x.CostSummary)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(x => x.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.InvoiceDate.Date <= toDate.Value.Date);

                if (type.HasValue)
                    query = query.Where(x => x.Type == type.Value);

                var invoices = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

                var result = invoices.Select(x => new CostSummaryReportDto
                {
                    InvoiceId = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.InvoiceDate,
                    Type = x.Type,
                    Status = x.Status,
                    PartyName = ResolvePartyName(x),
                    BaseItemsAmount = x.CostSummary?.BaseItemsAmount ?? x.ItemsTotalAmount,
                    NetItemsAmount = x.CostSummary?.NetItemsAmount ?? x.NetItemsAmount,
                    VatAmount = x.CostSummary?.VatAmount ?? x.VatAmount,
                    GrossItemsAmount = x.CostSummary?.GrossItemsAmount ?? x.GrossItemsAmount,
                    CostIncludedExpenseAmount = x.CostSummary?.CostIncludedExpenseAmount ?? x.CostIncludedExpenseAmount,
                    CostExcludedExpenseAmount = x.CostSummary?.CostExcludedExpenseAmount ?? 0,
                    CostIncludedTaxAmount = x.CostSummary?.CostIncludedTaxAmount ?? x.CostIncludedTaxAmount,
                    RecoverableTaxAmount = x.CostSummary?.RecoverableTaxAmount ?? x.RecoverableTaxAmount,
                    CostExcludedTaxAmount = x.CostSummary?.CostExcludedTaxAmount ?? x.CostExcludedTaxAmount,
                    DiscountAmount = x.CostSummary?.DiscountAmount ?? x.DiscountAmount,
                    FinalCostAmount = x.CostSummary?.FinalCostAmount ?? x.FinalCostAmount,
                    CostStatus = x.CostStatus,
                    CalculatedAt = x.CostSummary?.CalculatedAt
                }).ToList();

                return Result<List<CostSummaryReportDto>>.Success(result, "Cost summary hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<CostSummaryReportDto>>.Fail($"Cost summary hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<SupplierDebtReportDto>>> GetSupplierDebtReportAsync(bool onlyWithDebt = false)
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Include(x => x.BalanceTransactions.Where(t => t.IsActive))
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var result = suppliers.Select(x =>
                {
                    var lastTransaction = x.BalanceTransactions
                        .Where(t => t.IsActive)
                        .OrderByDescending(t => t.TransactionDate)
                        .ThenByDescending(t => t.Id)
                        .FirstOrDefault();

                    return new SupplierDebtReportDto
                    {
                        SupplierId = x.Id,
                        Name = x.Name,
                        CompanyName = x.CompanyName,
                        Phone = x.Phone,
                        OriginType = x.OriginType,
                        Currency = x.Currency,
                        DebtAmount = x.DebtAmount,
                        DebtAmountLocal = x.DebtAmountLocal,
                        DebtAmountOriginal = x.DebtAmountOriginal,
                        CreditLimit = x.CreditLimit,
                        IsOverLimit = x.CreditLimit > 0 && x.DebtAmountLocal > x.CreditLimit,
                        LastTransactionDate = lastTransaction?.TransactionDate,
                        LastTransactionNote = lastTransaction?.Note
                    };
                }).ToList();

                if (onlyWithDebt)
                    result = result.Where(x => x.DebtAmount > 0 || x.DebtAmountLocal > 0 || x.DebtAmountOriginal > 0).ToList();

                return Result<List<SupplierDebtReportDto>>.Success(result, "Təchizatçı borc hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<SupplierDebtReportDto>>.Fail($"Təchizatçı borc hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<CustomerDebtReportDto>>> GetCustomerDebtReportAsync(bool onlyWithDebt = false)
        {
            try
            {
                var customers = await _context.Customers
                    .Include(x => x.BalanceTransactions.Where(t => t.IsActive))
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var result = customers.Select(x =>
                {
                    var lastTransaction = x.BalanceTransactions
                        .Where(t => t.IsActive)
                        .OrderByDescending(t => t.TransactionDate)
                        .ThenByDescending(t => t.Id)
                        .FirstOrDefault();

                    return new CustomerDebtReportDto
                    {
                        CustomerId = x.Id,
                        Name = x.Name,
                        CompanyName = x.CompanyName,
                        Phone = x.Phone,
                        Currency = x.Currency,
                        DebtAmount = x.DebtAmount,
                        DebtAmountLocal = x.DebtAmountLocal,
                        DebtAmountOriginal = x.DebtAmountOriginal,
                        CreditLimit = x.CreditLimit,
                        IsOverLimit = x.CreditLimit > 0 && x.DebtAmountLocal > x.CreditLimit,
                        LastTransactionDate = lastTransaction?.TransactionDate,
                        LastTransactionNote = lastTransaction?.Note
                    };
                }).ToList();

                if (onlyWithDebt)
                    result = result.Where(x => x.DebtAmount > 0 || x.DebtAmountLocal > 0 || x.DebtAmountOriginal > 0).ToList();

                return Result<List<CustomerDebtReportDto>>.Success(result, "Müştəri borc hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<CustomerDebtReportDto>>.Fail($"Müştəri borc hesabatı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<ShelfOccupancyReportDto>>> GetShelfOccupancyReportAsync(int? warehouseId = null)
        {
            try
            {
                var query = _context.Shelves
                    .Include(x => x.Warehouse)
                    .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                        .ThenInclude(x => x.Product)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                if (warehouseId.HasValue && warehouseId.Value > 0)
                    query = query.Where(x => x.WarehouseId == warehouseId.Value);

                var shelves = await query
                    .OrderBy(x => x.Warehouse.Name)
                    .ThenBy(x => x.Zone)
                    .ThenBy(x => x.RowNumber)
                    .ThenBy(x => x.Code)
                    .ToListAsync();

                var result = shelves.Select(x =>
                {
                    var totalQuantity = x.ShelfStocks.Where(s => s.IsActive).Sum(s => s.Quantity);

                    return new ShelfOccupancyReportDto
                    {
                        ShelfId = x.Id,
                        ShelfCode = x.Code,
                        Zone = x.Zone,
                        RowNumber = x.RowNumber,
                        WarehouseName = x.Warehouse.Name,
                        Capacity = x.Capacity,
                        CurrentQuantity = totalQuantity,
                        OccupancyPercent = x.Capacity <= 0 ? x.OccupancyPercent : Math.Round(totalQuantity / x.Capacity * 100m, 2),
                        Status = x.Status,
                        ProductCount = x.ShelfStocks.Count(s => s.IsActive && s.Quantity > 0),
                        ProductsText = string.Join(", ", x.ShelfStocks
                            .Where(s => s.IsActive && s.Quantity > 0)
                            .Select(s => s.Product.Name)
                            .Distinct())
                    };
                }).ToList();

                return Result<List<ShelfOccupancyReportDto>>.Success(result, "Rəf doluluq hesabatı yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ShelfOccupancyReportDto>>.Fail($"Rəf doluluq hesabatı yüklənmədi: {ex.Message}");
            }
        }

        private static string ResolvePartyName(Invoice invoice)
        {
            return invoice.Type == InvoiceType.StockIn || invoice.Type == InvoiceType.SupplierReturnOut
                ? invoice.Supplier?.Name ?? ""
                : invoice.Customer?.Name ?? "";
        }

        private static decimal ResolveSaleUnitPriceFromInvoice(Invoice invoice, int productId)
        {
            var item = invoice.Items
                .Where(x => x.IsActive && x.ProductId == productId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (item == null || item.Quantity <= 0)
                return 0;

            if (item.NetAmount > 0)
                return Math.Round(item.NetAmount / item.Quantity, 4);

            if (item.GrossAmount > 0)
                return Math.Round(item.GrossAmount / item.Quantity, 4);

            if (item.LocalUnitPrice > 0)
                return item.LocalUnitPrice;

            if (item.Price > 0)
                return item.Price;

            return item.Total <= 0 ? 0 : Math.Round(item.Total / item.Quantity, 4);
        }

        private static string GetProductAttributesText(Product product)
        {
            if (product.Attributes == null || !product.Attributes.Any())
                return "";

            var values = product.Attributes
                .Where(x =>
                    x.IsActive &&
                    x.AttributeValue != null &&
                    x.AttributeValue.IsActive &&
                    x.AttributeValue.AttributeDefinition != null &&
                    x.AttributeValue.AttributeDefinition.IsActive)
                .OrderBy(x => x.AttributeValue.AttributeDefinition.Name)
                .Select(x => $"{x.AttributeValue.AttributeDefinition.Name}: {x.AttributeValue.Value}")
                .ToList();

            return string.Join(" / ", values);
        }
    }

    public class StockReportDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string? Barcode { get; set; }
        public string CategoryName { get; set; } = "";
        public string AttributesText { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal TotalQuantity { get; set; }
        public decimal MinStockQuantity { get; set; }
        public int ShelfCount { get; set; }
        public bool IsCritical { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LastCostPrice { get; set; }
        public decimal AverageCostPrice { get; set; }
        public decimal StockValueByLastCost { get; set; }
        public decimal StockValueByAverageCost { get; set; }
        public string ShelvesText { get; set; } = "";
        public string WarehousesText { get; set; } = "";
    }

    public class InvoiceReportDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public bool IsImport { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public string PartyName { get; set; } = "";

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public decimal ItemsTotalAmount { get; set; }
        public decimal LocalItemsTotalAmount { get; set; }
        public decimal OriginalItemsTotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }
        public decimal NetItemsAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossItemsAmount { get; set; }

        public decimal ExtraExpenseAmount { get; set; }
        public decimal CostIncludedExpenseAmount { get; set; }
        public decimal CostIncludedTaxAmount { get; set; }
        public decimal RecoverableTaxAmount { get; set; }
        public decimal CostExcludedTaxAmount { get; set; }
        public decimal FinalCostAmount { get; set; }
        public CostRecalculationStatus CostStatus { get; set; }

        public string ProductsText { get; set; } = "";
        public string CategoriesText { get; set; } = "";
        public string AttributesText { get; set; } = "";
        public string ShelvesText { get; set; } = "";
        public decimal QuantityTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public string? Note { get; set; }
    }

    public class MovementReportDto
    {
        public int MovementId { get; set; }
        public DateTime Date { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string AttributesText { get; set; } = "";
        public StockMovementType MovementType { get; set; }
        public decimal Quantity { get; set; }
        public int? FromShelfId { get; set; }
        public int? ToShelfId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string BatchNumber { get; set; } = "";
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public string? Note { get; set; }
    }

    public class BatchReportDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = "";
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string AttributesText { get; set; } = "";
        public string ShelfCode { get; set; } = "";
        public string WarehouseName { get; set; } = "";
        public string SourceInvoiceNumber { get; set; } = "";
        public DateTime EntryDate { get; set; }
        public DateTime FifoDate { get; set; }
        public DateTime? ProductionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal InitialQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal ConsumedQuantity { get; set; }
        public decimal PurchaseUnitPrice { get; set; }
        public decimal LocalUnitPrice { get; set; }
        public decimal ExpenseUnitShare { get; set; }
        public decimal TaxUnitShare { get; set; }
        public decimal DiscountUnitShare { get; set; }
        public decimal FinalUnitCost { get; set; }
        public decimal FinalTotalCost { get; set; }
        public decimal RemainingCostValue { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public bool IsImportBatch { get; set; }
        public bool IsClosed { get; set; }
        public string? Note { get; set; }
    }

    public class BatchProfitReportDto
    {
        public DateTime Date { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string BatchNumber { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal SaleUnitPrice { get; set; }
        public decimal SaleTotal { get; set; }
        public decimal UnitCost { get; set; }
        public decimal CostTotal { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
    }

    public class CostSummaryReportDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public string PartyName { get; set; } = "";
        public decimal BaseItemsAmount { get; set; }
        public decimal NetItemsAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossItemsAmount { get; set; }
        public decimal CostIncludedExpenseAmount { get; set; }
        public decimal CostExcludedExpenseAmount { get; set; }
        public decimal CostIncludedTaxAmount { get; set; }
        public decimal RecoverableTaxAmount { get; set; }
        public decimal CostExcludedTaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalCostAmount { get; set; }
        public CostRecalculationStatus CostStatus { get; set; }
        public DateTime? CalculatedAt { get; set; }
    }

    public class SupplierDebtReportDto
    {
        public int SupplierId { get; set; }
        public string Name { get; set; } = "";
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public SupplierOriginType OriginType { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal DebtAmount { get; set; }
        public decimal DebtAmountLocal { get; set; }
        public decimal DebtAmountOriginal { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsOverLimit { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public string? LastTransactionNote { get; set; }
    }

    public class CustomerDebtReportDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = "";
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal DebtAmount { get; set; }
        public decimal DebtAmountLocal { get; set; }
        public decimal DebtAmountOriginal { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsOverLimit { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public string? LastTransactionNote { get; set; }
    }

    public class ShelfOccupancyReportDto
    {
        public int ShelfId { get; set; }
        public string ShelfCode { get; set; } = "";
        public string Zone { get; set; } = "";
        public int RowNumber { get; set; }
        public string WarehouseName { get; set; } = "";
        public decimal Capacity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal OccupancyPercent { get; set; }
        public ShelfStatus Status { get; set; }
        public int ProductCount { get; set; }
        public string ProductsText { get; set; } = "";
    }
}