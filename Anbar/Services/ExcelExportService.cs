using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

namespace Anbar.Services
{
    public class ExcelExportService
    {
        private readonly AppDbContext _context;
        private readonly ReportService _reportService;

        public ExcelExportService(AppDbContext context, ReportService reportService)
        {
            _context = context;
            _reportService = reportService;

            ExcelPackage.License.SetNonCommercialPersonal("Yasin Hesenli");
        }

        public async Task<Result<string>> ExportStockReportAsync(
            string folderPath,
            string? search = null,
            int? categoryId = null,
            int? warehouseId = null,
            bool onlyCritical = false,
            bool onlyInStock = false)
        {
            try
            {
                var report = await _reportService.GetStockReportAsync(
                    search,
                    categoryId,
                    warehouseId,
                    onlyCritical,
                    onlyInStock);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Stok_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Stok Hesabatı");

                AddTitle(ws, "STOK HESABATI", 17);

                string[] headers =
                {
                    "Kod",
                    "Məhsul",
                    "Barkod",
                    "Kateqoriya",
                    "Xüsusiyyətlər",
                    "Vahid",
                    "Ümumi miqdar",
                    "Minimum stok",
                    "Rəf sayı",
                    "Rəflər",
                    "Anbarlar",
                    "Alış qiyməti",
                    "Satış qiyməti",
                    "Son maya",
                    "Orta maya",
                    "Stok dəyəri",
                    "Status"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.ProductCode;
                    ws.Cells[row, 2].Value = item.ProductName;
                    ws.Cells[row, 3].Value = item.Barcode;
                    ws.Cells[row, 4].Value = item.CategoryName;
                    ws.Cells[row, 5].Value = item.AttributesText;
                    ws.Cells[row, 6].Value = item.Unit;
                    ws.Cells[row, 7].Value = item.TotalQuantity;
                    ws.Cells[row, 8].Value = item.MinStockQuantity;
                    ws.Cells[row, 9].Value = item.ShelfCount;
                    ws.Cells[row, 10].Value = item.ShelvesText;
                    ws.Cells[row, 11].Value = item.WarehousesText;
                    ws.Cells[row, 12].Value = item.PurchasePrice;
                    ws.Cells[row, 13].Value = item.SalePrice;
                    ws.Cells[row, 14].Value = item.LastCostPrice;
                    ws.Cells[row, 15].Value = item.AverageCostPrice;
                    ws.Cells[row, 16].Value = item.StockValueByAverageCost;
                    ws.Cells[row, 17].Value = item.IsCritical ? "Kritik" : "Normal";

                    if (item.IsCritical)
                        MarkDanger(ws.Cells[row, 17]);

                    row++;
                }

                AddSummary(ws, row + 1, 15, "Yekun stok dəyəri:", report.Data.Sum(x => x.StockValueByAverageCost));
                FormatTable(ws, 3, row - 1, headers.Length);
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Stok hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Stok hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportInvoiceAsync(int invoiceId, string folderPath)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(x => x.Supplier)
                    .Include(x => x.Customer)
                    .Include(x => x.CostSummary)
                    .Include(x => x.Taxes.Where(t => t.IsActive))
                    .Include(x => x.Expenses.Where(e => e.IsActive))
                    .Include(x => x.DynamicFieldValues.Where(f => f.IsActive))
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
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<string>.Fail("Qaimə tapılmadı.");

                Directory.CreateDirectory(folderPath);

                var safeNumber = MakeSafeFileName(invoice.InvoiceNumber);
                var filePath = Path.Combine(folderPath, $"Qaime_{safeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Qaimə");

                AddTitle(ws, invoice.Type == InvoiceType.StockIn ? "GİRİŞ QAİMƏSİ" : "ÇIXIŞ QAİMƏSİ", 14);

                ws.Cells["A3"].Value = "Qaimə №:";
                ws.Cells["B3"].Value = invoice.InvoiceNumber;
                ws.Cells["A4"].Value = "Tarix:";
                ws.Cells["B4"].Value = invoice.InvoiceDate.ToString("dd.MM.yyyy HH:mm");
                ws.Cells["A5"].Value = invoice.Type == InvoiceType.StockIn ? "Təchizatçı:" : "Müştəri:";
                ws.Cells["B5"].Value = ResolvePartyName(invoice);
                ws.Cells["A6"].Value = "Status:";
                ws.Cells["B6"].Value = invoice.Status.ToString();
                ws.Cells["A7"].Value = "Valyuta:";
                ws.Cells["B7"].Value = invoice.Currency.ToString();

                ws.Cells["K3"].Value = "Ümumi:";
                ws.Cells["L3"].Value = invoice.TotalAmount;
                ws.Cells["K4"].Value = "Ödənilib:";
                ws.Cells["L4"].Value = invoice.PaidAmount;
                ws.Cells["K5"].Value = "Borc:";
                ws.Cells["L5"].Value = invoice.DebtAmount;
                ws.Cells["K6"].Value = "Final maya:";
                ws.Cells["L6"].Value = invoice.FinalCostAmount;

                StyleKeyValueArea(ws, 3, 1, 7, 2);
                StyleKeyValueArea(ws, 3, 11, 6, 12);

                string[] headers =
                {
                    "№",
                    "Kod",
                    "Məhsul",
                    "Kateqoriya",
                    "Xüsusiyyətlər",
                    "Rəf",
                    "Miqdar",
                    "Vahid",
                    "Qiymət",
                    "Endirim",
                    "Net",
                    "ƏDV",
                    "Maya",
                    "Cəmi"
                };

                AddSection(ws, 9, "Məhsullar", headers.Length);
                AddHeader(ws, 10, headers);

                var row = 11;
                var index = 1;

                foreach (var item in invoice.Items.Where(x => x.IsActive))
                {
                    ws.Cells[row, 1].Value = index++;
                    ws.Cells[row, 2].Value = item.Product?.Code;
                    ws.Cells[row, 3].Value = item.Product?.Name;
                    ws.Cells[row, 4].Value = item.Product?.Category?.Name;
                    ws.Cells[row, 5].Value = item.Product == null ? "" : GetProductAttributesText(item.Product);
                    ws.Cells[row, 6].Value = item.Shelf?.Code;
                    ws.Cells[row, 7].Value = item.Quantity;
                    ws.Cells[row, 8].Value = item.Product?.Unit;
                    ws.Cells[row, 9].Value = item.Price;
                    ws.Cells[row, 10].Value = item.DiscountAmount;
                    ws.Cells[row, 11].Value = item.NetAmount;
                    ws.Cells[row, 12].Value = item.VatAmount;
                    ws.Cells[row, 13].Value = item.FinalUnitCost;
                    ws.Cells[row, 14].Value = item.Total;

                    row++;
                }

                FormatTable(ws, 10, row - 1, headers.Length);

                var summaryRow = row + 2;

                ws.Cells[summaryRow, 11].Value = "Məhsul net:";
                ws.Cells[summaryRow, 12].Value = invoice.NetItemsAmount;

                ws.Cells[summaryRow + 1, 11].Value = "ƏDV:";
                ws.Cells[summaryRow + 1, 12].Value = invoice.VatAmount;

                ws.Cells[summaryRow + 2, 11].Value = "Əlavə xərc:";
                ws.Cells[summaryRow + 2, 12].Value = invoice.ExtraExpenseAmount;

                ws.Cells[summaryRow + 3, 11].Value = "Maya daxil vergi:";
                ws.Cells[summaryRow + 3, 12].Value = invoice.CostIncludedTaxAmount;

                ws.Cells[summaryRow + 4, 11].Value = "Final maya:";
                ws.Cells[summaryRow + 4, 12].Value = invoice.FinalCostAmount;

                ws.Cells[summaryRow + 5, 11].Value = "Qaimə cəmi:";
                ws.Cells[summaryRow + 5, 12].Value = invoice.TotalAmount;

                ws.Cells[summaryRow, 11, summaryRow + 5, 12].Style.Font.Bold = true;
                ws.Cells[summaryRow, 12, summaryRow + 5, 12].Style.Numberformat.Format = "#,##0.00";

                if (invoice.DynamicFieldValues.Any())
                {
                    var dynamicRow = summaryRow + 8;
                    AddSection(ws, dynamicRow, "Dinamik məlumatlar", 4);

                    var dRow = dynamicRow + 1;

                    foreach (var field in invoice.DynamicFieldValues.Where(x => x.IsActive).OrderBy(x => x.FieldName))
                    {
                        ws.Cells[dRow, 1].Value = field.FieldName;
                        ws.Cells[dRow, 2].Value = field.Value;
                        ws.Cells[dRow, 3].Value = field.FieldType.ToString();
                        ws.Cells[dRow, 4].Value = field.Note;
                        dRow++;
                    }

                    FormatTable(ws, dynamicRow + 1, dRow - 1, 4);
                }

                ws.Cells[summaryRow + 14, 1].Value = "Təhvil verdi:";
                ws.Cells[summaryRow + 14, 3].Value = "________________";
                ws.Cells[summaryRow + 14, 8].Value = "Təhvil aldı:";
                ws.Cells[summaryRow + 14, 10].Value = "________________";

                FinishWorksheet(ws, 14);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Qaimə Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Qaimə Excel çıxarışı alınmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportInvoiceReportAsync(
            string folderPath,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoiceType? type = null,
            InvoiceStatus? status = null,
            string? search = null,
            bool? isImport = null)
        {
            try
            {
                var report = await _reportService.GetInvoiceReportAsync(fromDate, toDate, type, status, search, isImport);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Qaime_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Qaimə Hesabatı");

                AddTitle(ws, "QAİMƏ HESABATI", 25);

                string[] headers =
                {
                    "Qaimə №",
                    "Tarix",
                    "Tip",
                    "Status",
                    "İdxal?",
                    "Valyuta",
                    "Məzənnə",
                    "Müştəri/Təchizatçı",
                    "Məhsullar",
                    "Kateqoriyalar",
                    "Xüsusiyyətlər",
                    "Rəflər",
                    "Miqdar",
                    "Net",
                    "ƏDV",
                    "Gross",
                    "Əlavə xərc",
                    "Endirim",
                    "Maya daxil xərc",
                    "Maya daxil vergi",
                    "Recoverable vergi",
                    "Final maya",
                    "Ümumi",
                    "Ödənilib",
                    "Borc"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.InvoiceNumber;
                    ws.Cells[row, 2].Value = item.InvoiceDate.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 3].Value = item.Type.ToString();
                    ws.Cells[row, 4].Value = item.Status.ToString();
                    ws.Cells[row, 5].Value = item.IsImport ? "Bəli" : "Xeyr";
                    ws.Cells[row, 6].Value = item.Currency.ToString();
                    ws.Cells[row, 7].Value = item.ExchangeRate;
                    ws.Cells[row, 8].Value = item.PartyName;
                    ws.Cells[row, 9].Value = item.ProductsText;
                    ws.Cells[row, 10].Value = item.CategoriesText;
                    ws.Cells[row, 11].Value = item.AttributesText;
                    ws.Cells[row, 12].Value = item.ShelvesText;
                    ws.Cells[row, 13].Value = item.QuantityTotal;
                    ws.Cells[row, 14].Value = item.NetItemsAmount;
                    ws.Cells[row, 15].Value = item.VatAmount;
                    ws.Cells[row, 16].Value = item.GrossItemsAmount;
                    ws.Cells[row, 17].Value = item.DiscountAmount;
                    ws.Cells[row, 18].Value = item.ExtraExpenseAmount;
                    ws.Cells[row, 19].Value = item.CostIncludedExpenseAmount;
                    ws.Cells[row, 20].Value = item.CostIncludedTaxAmount;
                    ws.Cells[row, 21].Value = item.RecoverableTaxAmount;
                    ws.Cells[row, 22].Value = item.FinalCostAmount;
                    ws.Cells[row, 23].Value = item.TotalAmount;
                    ws.Cells[row, 24].Value = item.PaidAmount;
                    ws.Cells[row, 25].Value = item.DebtAmount;

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                AddSummary(ws, row + 1, 22, "Yekun:", report.Data.Sum(x => x.TotalAmount));

                ws.Cells[row + 1, 24].Value = report.Data.Sum(x => x.PaidAmount);
                ws.Cells[row + 1, 25].Value = report.Data.Sum(x => x.DebtAmount);

                ws.Cells[row + 1, 22, row + 1, 25].Style.Font.Bold = true;
                ws.Cells[row + 1, 22, row + 1, 25].Style.Numberformat.Format = "#,##0.00";

                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Qaimə hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Qaimə hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportMovementReportAsync(
            string folderPath,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            StockMovementType? movementType = null,
            int? productId = null,
            int? shelfId = null,
            int? invoiceId = null)
        {
            try
            {
                var report = await _reportService.GetMovementReportAsync(
                    fromDate,
                    toDate,
                    movementType,
                    productId,
                    shelfId,
                    invoiceId);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Hereket_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Hərəkət Hesabatı");

                AddTitle(ws, "STOK HƏRƏKƏTLƏRİ HESABATI", 15);

                string[] headers =
                {
                    "Tarix",
                    "Kod",
                    "Məhsul",
                    "Kateqoriya",
                    "Xüsusiyyətlər",
                    "Hərəkət",
                    "Miqdar",
                    "FromShelfId",
                    "ToShelfId",
                    "Qaimə №",
                    "Batch №",
                    "UnitCost",
                    "TotalCost",
                    "Valyuta",
                    "Qeyd"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.Date.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 2].Value = item.ProductCode;
                    ws.Cells[row, 3].Value = item.ProductName;
                    ws.Cells[row, 4].Value = item.CategoryName;
                    ws.Cells[row, 5].Value = item.AttributesText;
                    ws.Cells[row, 6].Value = item.MovementType.ToString();
                    ws.Cells[row, 7].Value = item.Quantity;
                    ws.Cells[row, 8].Value = item.FromShelfId;
                    ws.Cells[row, 9].Value = item.ToShelfId;
                    ws.Cells[row, 10].Value = item.InvoiceNumber;
                    ws.Cells[row, 11].Value = item.BatchNumber;
                    ws.Cells[row, 12].Value = item.UnitCost;
                    ws.Cells[row, 13].Value = item.TotalCost;
                    ws.Cells[row, 14].Value = item.Currency.ToString();
                    ws.Cells[row, 15].Value = item.Note;

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Hərəkət hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Hərəkət hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportBatchReportAsync(
            string folderPath,
            int? productId = null,
            int? shelfId = null,
            bool onlyOpen = false,
            bool onlyWithRemaining = false,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var report = await _reportService.GetBatchReportAsync(
                    productId,
                    shelfId,
                    onlyOpen,
                    onlyWithRemaining,
                    fromDate,
                    toDate);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"FIFO_Batch_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("FIFO Batch");

                AddTitle(ws, "FIFO BATCH HESABATI", 23);

                string[] headers =
                {
                    "Batch №",
                    "Məhsul kodu",
                    "Məhsul",
                    "Kateqoriya",
                    "Xüsusiyyətlər",
                    "Anbar",
                    "Rəf",
                    "Mənbə qaimə",
                    "Giriş tarixi",
                    "FIFO tarixi",
                    "İlkin miqdar",
                    "Qalıq miqdar",
                    "İstifadə olunub",
                    "Alış qiyməti",
                    "Lokal qiymət",
                    "Xərc payı",
                    "Vergi payı",
                    "Endirim payı",
                    "Final maya",
                    "Final cəmi",
                    "Qalıq dəyəri",
                    "Status",
                    "Qeyd"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.BatchNumber;
                    ws.Cells[row, 2].Value = item.ProductCode;
                    ws.Cells[row, 3].Value = item.ProductName;
                    ws.Cells[row, 4].Value = item.CategoryName;
                    ws.Cells[row, 5].Value = item.AttributesText;
                    ws.Cells[row, 6].Value = item.WarehouseName;
                    ws.Cells[row, 7].Value = item.ShelfCode;
                    ws.Cells[row, 8].Value = item.SourceInvoiceNumber;
                    ws.Cells[row, 9].Value = item.EntryDate.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 10].Value = item.FifoDate.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 11].Value = item.InitialQuantity;
                    ws.Cells[row, 12].Value = item.RemainingQuantity;
                    ws.Cells[row, 13].Value = item.ConsumedQuantity;
                    ws.Cells[row, 14].Value = item.PurchaseUnitPrice;
                    ws.Cells[row, 15].Value = item.LocalUnitPrice;
                    ws.Cells[row, 16].Value = item.ExpenseUnitShare;
                    ws.Cells[row, 17].Value = item.TaxUnitShare;
                    ws.Cells[row, 18].Value = item.DiscountUnitShare;
                    ws.Cells[row, 19].Value = item.FinalUnitCost;
                    ws.Cells[row, 20].Value = item.FinalTotalCost;
                    ws.Cells[row, 21].Value = item.RemainingCostValue;
                    ws.Cells[row, 22].Value = item.IsClosed ? "Bağlı" : "Açıq";
                    ws.Cells[row, 23].Value = item.Note;

                    if (item.RemainingQuantity <= 0)
                        MarkMuted(ws.Cells[row, 22]);

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                AddSummary(ws, row + 1, 20, "Qalıq dəyəri:", report.Data.Sum(x => x.RemainingCostValue));
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "FIFO batch hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"FIFO batch hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportBatchProfitReportAsync(
            string folderPath,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? productId = null)
        {
            try
            {
                var report = await _reportService.GetBatchProfitReportAsync(fromDate, toDate, productId);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Batch_Profit_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Batch Profit");

                AddTitle(ws, "BATCH PROFIT HESABATI", 13);

                string[] headers =
                {
                    "Tarix",
                    "Qaimə №",
                    "Batch №",
                    "Məhsul kodu",
                    "Məhsul",
                    "Kateqoriya",
                    "Miqdar",
                    "Satış qiyməti",
                    "Satış cəmi",
                    "UnitCost",
                    "Cost cəmi",
                    "Profit",
                    "Profit %"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.Date.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 2].Value = item.InvoiceNumber;
                    ws.Cells[row, 3].Value = item.BatchNumber;
                    ws.Cells[row, 4].Value = item.ProductCode;
                    ws.Cells[row, 5].Value = item.ProductName;
                    ws.Cells[row, 6].Value = item.CategoryName;
                    ws.Cells[row, 7].Value = item.Quantity;
                    ws.Cells[row, 8].Value = item.SaleUnitPrice;
                    ws.Cells[row, 9].Value = item.SaleTotal;
                    ws.Cells[row, 10].Value = item.UnitCost;
                    ws.Cells[row, 11].Value = item.CostTotal;
                    ws.Cells[row, 12].Value = item.Profit;
                    ws.Cells[row, 13].Value = item.ProfitPercent;

                    if (item.Profit < 0)
                        MarkDanger(ws.Cells[row, 12]);
                    else
                        MarkSuccess(ws.Cells[row, 12]);

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);

                ws.Cells[row + 1, 8].Value = "Yekun:";
                ws.Cells[row + 1, 9].Value = report.Data.Sum(x => x.SaleTotal);
                ws.Cells[row + 1, 11].Value = report.Data.Sum(x => x.CostTotal);
                ws.Cells[row + 1, 12].Value = report.Data.Sum(x => x.Profit);
                ws.Cells[row + 1, 8, row + 1, 12].Style.Font.Bold = true;
                ws.Cells[row + 1, 9, row + 1, 12].Style.Numberformat.Format = "#,##0.00";

                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Batch profit hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Batch profit hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportCostSummaryReportAsync(
            string folderPath,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoiceType? type = null)
        {
            try
            {
                var report = await _reportService.GetCostSummaryReportAsync(fromDate, toDate, type);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Cost_Summary_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Cost Summary");

                AddTitle(ws, "COST SUMMARY HESABATI", 18);

                string[] headers =
                {
                    "Qaimə №",
                    "Tarix",
                    "Tip",
                    "Status",
                    "Tərəf",
                    "Base items",
                    "Net items",
                    "ƏDV",
                    "Gross items",
                    "Maya daxil xərc",
                    "Maya xaric xərc",
                    "Maya daxil vergi",
                    "Recoverable vergi",
                    "Maya xaric vergi",
                    "Endirim",
                    "Final maya",
                    "Cost status",
                    "Hesablanma tarixi"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.InvoiceNumber;
                    ws.Cells[row, 2].Value = item.InvoiceDate.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 3].Value = item.Type.ToString();
                    ws.Cells[row, 4].Value = item.Status.ToString();
                    ws.Cells[row, 5].Value = item.PartyName;
                    ws.Cells[row, 6].Value = item.BaseItemsAmount;
                    ws.Cells[row, 7].Value = item.NetItemsAmount;
                    ws.Cells[row, 8].Value = item.VatAmount;
                    ws.Cells[row, 9].Value = item.GrossItemsAmount;
                    ws.Cells[row, 10].Value = item.CostIncludedExpenseAmount;
                    ws.Cells[row, 11].Value = item.CostExcludedExpenseAmount;
                    ws.Cells[row, 12].Value = item.CostIncludedTaxAmount;
                    ws.Cells[row, 13].Value = item.RecoverableTaxAmount;
                    ws.Cells[row, 14].Value = item.CostExcludedTaxAmount;
                    ws.Cells[row, 15].Value = item.DiscountAmount;
                    ws.Cells[row, 16].Value = item.FinalCostAmount;
                    ws.Cells[row, 17].Value = item.CostStatus.ToString();
                    ws.Cells[row, 18].Value = item.CalculatedAt?.ToString("dd.MM.yyyy HH:mm");

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                AddSummary(ws, row + 1, 15, "Yekun final maya:", report.Data.Sum(x => x.FinalCostAmount));
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Cost summary hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Cost summary hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportSupplierDebtReportAsync(string folderPath, bool onlyWithDebt = false)
        {
            try
            {
                var report = await _reportService.GetSupplierDebtReportAsync(onlyWithDebt);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Techizatci_Borc_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Təchizatçı Borcları");

                AddTitle(ws, "TƏCHİZATÇI BORC HESABATI", 13);

                string[] headers =
                {
                    "Ad",
                    "Şirkət",
                    "Telefon",
                    "Origin",
                    "Valyuta",
                    "Borc",
                    "Borc local",
                    "Borc original",
                    "Kredit limiti",
                    "Limit aşımı",
                    "Son əməliyyat",
                    "Son qeyd",
                    "Status"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.Name;
                    ws.Cells[row, 2].Value = item.CompanyName;
                    ws.Cells[row, 3].Value = item.Phone;
                    ws.Cells[row, 4].Value = item.OriginType.ToString();
                    ws.Cells[row, 5].Value = item.Currency.ToString();
                    ws.Cells[row, 6].Value = item.DebtAmount;
                    ws.Cells[row, 7].Value = item.DebtAmountLocal;
                    ws.Cells[row, 8].Value = item.DebtAmountOriginal;
                    ws.Cells[row, 9].Value = item.CreditLimit;
                    ws.Cells[row, 10].Value = item.IsOverLimit ? "Bəli" : "Xeyr";
                    ws.Cells[row, 11].Value = item.LastTransactionDate?.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 12].Value = item.LastTransactionNote;
                    ws.Cells[row, 13].Value = item.DebtAmountLocal > 0 ? "Borclu" : "Təmiz";

                    if (item.IsOverLimit)
                        MarkDanger(ws.Cells[row, 10]);

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                AddSummary(ws, row + 1, 6, "Yekun borc local:", report.Data.Sum(x => x.DebtAmountLocal));
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Təchizatçı borc hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Təchizatçı borc hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportCustomerDebtReportAsync(string folderPath, bool onlyWithDebt = false)
        {
            try
            {
                var report = await _reportService.GetCustomerDebtReportAsync(onlyWithDebt);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Musteri_Borc_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Müştəri Borcları");

                AddTitle(ws, "MÜŞTƏRİ BORC HESABATI", 13);

                string[] headers =
                {
                    "Ad",
                    "Şirkət",
                    "Telefon",
                    "Valyuta",
                    "Borc",
                    "Borc local",
                    "Borc original",
                    "Kredit limiti",
                    "Limit aşımı",
                    "Son əməliyyat",
                    "Son qeyd",
                    "Status",
                    "Qeyd"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.Name;
                    ws.Cells[row, 2].Value = item.CompanyName;
                    ws.Cells[row, 3].Value = item.Phone;
                    ws.Cells[row, 4].Value = item.Currency.ToString();
                    ws.Cells[row, 5].Value = item.DebtAmount;
                    ws.Cells[row, 6].Value = item.DebtAmountLocal;
                    ws.Cells[row, 7].Value = item.DebtAmountOriginal;
                    ws.Cells[row, 8].Value = item.CreditLimit;
                    ws.Cells[row, 9].Value = item.IsOverLimit ? "Bəli" : "Xeyr";
                    ws.Cells[row, 10].Value = item.LastTransactionDate?.ToString("dd.MM.yyyy HH:mm");
                    ws.Cells[row, 11].Value = item.LastTransactionNote;
                    ws.Cells[row, 12].Value = item.DebtAmountLocal > 0 ? "Borclu" : "Təmiz";
                    ws.Cells[row, 13].Value = "";

                    if (item.IsOverLimit)
                        MarkDanger(ws.Cells[row, 9]);

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                AddSummary(ws, row + 1, 5, "Yekun borc local:", report.Data.Sum(x => x.DebtAmountLocal));
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Müştəri borc hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Müştəri borc hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportShelfOccupancyReportAsync(string folderPath, int? warehouseId = null)
        {
            try
            {
                var report = await _reportService.GetShelfOccupancyReportAsync(warehouseId);

                if (!report.IsSuccess || report.Data == null)
                    return Result<string>.Fail(report.Message);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Ref_Doluluq_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Rəf Doluluğu");

                AddTitle(ws, "RƏF DOLULUQ HESABATI", 11);

                string[] headers =
                {
                    "Anbar",
                    "Zona",
                    "Sıra",
                    "Rəf",
                    "Tutum",
                    "Cari miqdar",
                    "Doluluq %",
                    "Status",
                    "Məhsul sayı",
                    "Məhsullar",
                    "Qeyd"
                };

                AddHeader(ws, 3, headers);

                var row = 4;

                foreach (var item in report.Data)
                {
                    ws.Cells[row, 1].Value = item.WarehouseName;
                    ws.Cells[row, 2].Value = item.Zone;
                    ws.Cells[row, 3].Value = item.RowNumber;
                    ws.Cells[row, 4].Value = item.ShelfCode;
                    ws.Cells[row, 5].Value = item.Capacity;
                    ws.Cells[row, 6].Value = item.CurrentQuantity;
                    ws.Cells[row, 7].Value = item.OccupancyPercent;
                    ws.Cells[row, 8].Value = item.Status.ToString();
                    ws.Cells[row, 9].Value = item.ProductCount;
                    ws.Cells[row, 10].Value = item.ProductsText;
                    ws.Cells[row, 11].Value = item.OccupancyPercent >= 100 ? "Doludur" : "";

                    if (item.OccupancyPercent >= 100)
                        MarkDanger(ws.Cells[row, 7]);
                    else if (item.OccupancyPercent >= 80)
                        MarkWarning(ws.Cells[row, 7]);
                    else
                        MarkSuccess(ws.Cells[row, 7]);

                    row++;
                }

                FormatTable(ws, 3, row - 1, headers.Length);
                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Rəf doluluq hesabatı Excel faylına çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Rəf doluluq hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportDetailedInvoiceReportAsync(
            string folderPath,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoiceType? type = null,
            InvoiceStatus? status = null,
            string? search = null)
        {
            try
            {
                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, $"Etrafli_Qaime_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

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

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.InvoiceNumber.ToLower().Contains(keyword) ||
                        (x.Supplier != null && x.Supplier.Name.ToLower().Contains(keyword)) ||
                        (x.Customer != null && x.Customer.Name.ToLower().Contains(keyword)) ||
                        (x.Supplier != null && x.Supplier.CompanyName != null && x.Supplier.CompanyName.ToLower().Contains(keyword)) ||
                        (x.Customer != null && x.Customer.CompanyName != null && x.Customer.CompanyName.ToLower().Contains(keyword)) ||
                        (x.Note != null && x.Note.ToLower().Contains(keyword)) ||
                        x.Items.Any(i =>
                            i.IsActive &&
                            i.Product != null &&
                            (
                                i.Product.Name.ToLower().Contains(keyword) ||
                                i.Product.Code.ToLower().Contains(keyword) ||
                                (i.Product.Category != null && i.Product.Category.Name.ToLower().Contains(keyword))
                            )));
                }

                var invoices = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Ətraflı Qaimələr");

                AddTitle(ws, "GİRİŞ VƏ ÇIXIŞ QAİMƏLƏRİ ÜZRƏ ƏTRAFLI HESABAT", 24);

                ws.Cells["A2:X2"].Merge = true;
                ws.Cells["A2"].Value =
                    $"Tarix aralığı: {(fromDate.HasValue ? fromDate.Value.ToString("dd.MM.yyyy") : "Hamısı")} - {(toDate.HasValue ? toDate.Value.ToString("dd.MM.yyyy") : "Hamısı")} | " +
                    $"Tip: {(type.HasValue ? type.Value.ToString() : "Hamısı")} | " +
                    $"Status: {(status.HasValue ? status.Value.ToString() : "Hamısı")}";
                ws.Cells["A2"].Style.Font.Italic = true;
                ws.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(71, 85, 105));

                string[] headers =
                {
                    "Qaimə №",
                    "Tarix",
                    "Tip",
                    "Status",
                    "Tərəf növü",
                    "Tərəf adı",
                    "Şirkət",
                    "Telefon",
                    "Məhsul kodu",
                    "Məhsul",
                    "Kateqoriya",
                    "Xüsusiyyətlər",
                    "Rəf",
                    "Miqdar",
                    "Vahid",
                    "Qiymət",
                    "Endirim",
                    "Net",
                    "ƏDV",
                    "Sətir cəmi",
                    "Qaimə cəmi",
                    "Ödənilib",
                    "Borc",
                    "Qeyd"
                };

                AddHeader(ws, 4, headers);

                var row = 5;

                foreach (var invoice in invoices)
                {
                    var isStockIn = invoice.Type == InvoiceType.StockIn || invoice.Type == InvoiceType.CustomerReturnIn;

                    var partyName = ResolvePartyName(invoice);
                    var partyCompany = isStockIn ? invoice.Supplier?.CompanyName : invoice.Customer?.CompanyName;
                    var partyPhone = isStockIn ? invoice.Supplier?.Phone : invoice.Customer?.Phone;

                    var activeItems = invoice.Items.Where(x => x.IsActive).ToList();

                    if (activeItems.Any())
                    {
                        foreach (var item in activeItems)
                        {
                            ws.Cells[row, 1].Value = invoice.InvoiceNumber;
                            ws.Cells[row, 2].Value = invoice.InvoiceDate.ToString("dd.MM.yyyy HH:mm");
                            ws.Cells[row, 3].Value = invoice.Type.ToString();
                            ws.Cells[row, 4].Value = invoice.Status.ToString();
                            ws.Cells[row, 5].Value = isStockIn ? "Təchizatçı" : "Müştəri";
                            ws.Cells[row, 6].Value = partyName;
                            ws.Cells[row, 7].Value = partyCompany;
                            ws.Cells[row, 8].Value = partyPhone;
                            ws.Cells[row, 9].Value = item.Product?.Code;
                            ws.Cells[row, 10].Value = item.Product?.Name;
                            ws.Cells[row, 11].Value = item.Product?.Category?.Name;
                            ws.Cells[row, 12].Value = item.Product == null ? "" : GetProductAttributesText(item.Product);
                            ws.Cells[row, 13].Value = item.Shelf?.Code;
                            ws.Cells[row, 14].Value = item.Quantity;
                            ws.Cells[row, 15].Value = item.Product?.Unit;
                            ws.Cells[row, 16].Value = item.Price;
                            ws.Cells[row, 17].Value = item.DiscountAmount;
                            ws.Cells[row, 18].Value = item.NetAmount;
                            ws.Cells[row, 19].Value = item.VatAmount;
                            ws.Cells[row, 20].Value = item.Total;
                            ws.Cells[row, 21].Value = invoice.TotalAmount;
                            ws.Cells[row, 22].Value = invoice.PaidAmount;
                            ws.Cells[row, 23].Value = invoice.DebtAmount;
                            ws.Cells[row, 24].Value = invoice.Note;

                            row++;
                        }
                    }
                    else
                    {
                        ws.Cells[row, 1].Value = invoice.InvoiceNumber;
                        ws.Cells[row, 2].Value = invoice.InvoiceDate.ToString("dd.MM.yyyy HH:mm");
                        ws.Cells[row, 3].Value = invoice.Type.ToString();
                        ws.Cells[row, 4].Value = invoice.Status.ToString();
                        ws.Cells[row, 5].Value = isStockIn ? "Təchizatçı" : "Müştəri";
                        ws.Cells[row, 6].Value = partyName;
                        ws.Cells[row, 7].Value = partyCompany;
                        ws.Cells[row, 8].Value = partyPhone;
                        ws.Cells[row, 21].Value = invoice.TotalAmount;
                        ws.Cells[row, 22].Value = invoice.PaidAmount;
                        ws.Cells[row, 23].Value = invoice.DebtAmount;
                        ws.Cells[row, 24].Value = invoice.Note;

                        row++;
                    }
                }

                FormatTable(ws, 4, row - 1, headers.Length);

                ws.Cells[row + 1, 20].Value = "Yekun:";
                ws.Cells[row + 1, 21].Value = invoices.Sum(x => x.TotalAmount);
                ws.Cells[row + 1, 22].Value = invoices.Sum(x => x.PaidAmount);
                ws.Cells[row + 1, 23].Value = invoices.Sum(x => x.DebtAmount);
                ws.Cells[row + 1, 20, row + 1, 23].Style.Font.Bold = true;
                ws.Cells[row + 1, 21, row + 1, 23].Style.Numberformat.Format = "#,##0.00";

                FinishWorksheet(ws, headers.Length);

                await package.SaveAsAsync(new FileInfo(filePath));

                return Result<string>.Success(filePath, "Giriş və çıxış qaimələri üzrə ətraflı Excel hesabatı çıxarıldı.");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Ətraflı qaimə hesabatı çıxarılmadı: {ex.Message}");
            }
        }

        private static string ResolvePartyName(Invoice invoice)
        {
            return invoice.Type == InvoiceType.StockIn || invoice.Type == InvoiceType.SupplierReturnOut
                ? invoice.Supplier?.Name ?? ""
                : invoice.Customer?.Name ?? "";
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

        private static void AddTitle(ExcelWorksheet ws, string title, int columnCount)
        {
            ws.Cells[1, 1, 1, columnCount].Merge = true;
            ws.Cells[1, 1].Value = title;
            ws.Cells[1, 1].Style.Font.Size = 16;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 58, 138));
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Row(1).Height = 30;
        }

        private static void AddSection(ExcelWorksheet ws, int row, string title, int columnCount)
        {
            ws.Cells[row, 1, row, columnCount].Merge = true;
            ws.Cells[row, 1].Value = title;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(243, 244, 246));
            ws.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        private static void AddHeader(ExcelWorksheet ws, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
                ws.Cells[row, i + 1].Value = headers[i];

            using var range = ws.Cells[row, 1, row, headers.Length];
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(229, 231, 235));
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private static void FormatTable(ExcelWorksheet ws, int startRow, int endRow, int columnCount)
        {
            if (endRow < startRow)
                return;

            using var range = ws.Cells[startRow, 1, endRow, columnCount];

            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            if (endRow > startRow)
                ws.Cells[startRow + 1, 1, endRow, columnCount].Style.Numberformat.Format = "#,##0.00";
        }

        private static void AddSummary(ExcelWorksheet ws, int row, int labelColumn, string label, decimal value)
        {
            ws.Cells[row, labelColumn].Value = label;
            ws.Cells[row, labelColumn + 1].Value = value;
            ws.Cells[row, labelColumn, row, labelColumn + 1].Style.Font.Bold = true;
            ws.Cells[row, labelColumn + 1].Style.Numberformat.Format = "#,##0.00";
        }

        private static void StyleKeyValueArea(ExcelWorksheet ws, int startRow, int startCol, int endRow, int endCol)
        {
            using var range = ws.Cells[startRow, startCol, endRow, endCol];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            ws.Cells[startRow, startCol, endRow, startCol].Style.Font.Bold = true;
            ws.Cells[startRow, startCol, endRow, startCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[startRow, startCol, endRow, startCol].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(243, 244, 246));
        }

        private static void FinishWorksheet(ExcelWorksheet ws, int columnCount)
        {
            ws.View.FreezePanes(4, 1);

            if (ws.Dimension != null)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                ws.Cells[ws.Dimension.Address].Style.Font.Name = "Segoe UI";
                ws.Cells[ws.Dimension.Address].Style.Font.Size = 10;
            }

            for (int i = 1; i <= columnCount; i++)
                ws.Column(i).Width = Math.Min(ws.Column(i).Width + 2, 45);

            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;
        }

        private static void MarkDanger(ExcelRange range)
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(254, 226, 226));
            range.Style.Font.Color.SetColor(Color.FromArgb(185, 28, 28));
            range.Style.Font.Bold = true;
        }

        private static void MarkWarning(ExcelRange range)
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(254, 243, 199));
            range.Style.Font.Color.SetColor(Color.FromArgb(146, 64, 14));
            range.Style.Font.Bold = true;
        }

        private static void MarkSuccess(ExcelRange range)
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 252, 231));
            range.Style.Font.Color.SetColor(Color.FromArgb(22, 101, 52));
            range.Style.Font.Bold = true;
        }

        private static void MarkMuted(ExcelRange range)
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));
            range.Style.Font.Color.SetColor(Color.FromArgb(100, 116, 139));
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value.Trim();
        }
    }
}