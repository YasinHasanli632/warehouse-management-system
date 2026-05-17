using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Anbar.Views
{
    public partial class ReportsView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ReportService _reportService;
        private readonly ExcelExportService _excelExportService;

        private List<InvoiceReportLineRow> _rows = new();

        public ReportsView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _reportService = new ReportService(_context);
            _excelExportService = new ExcelExportService(_context, _reportService);

            Loaded += ReportsView_Loaded;
        }

        private async void ReportsView_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareFilters();

            FromDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Now;

            await LoadReportAsync();
        }

        private void PrepareFilters()
        {
            TypeCombo.ItemsSource = new List<InvoiceTypeFilter>
            {
                new InvoiceTypeFilter { Text = "Hamısı", Value = null },
                new InvoiceTypeFilter { Text = "Giriş qaiməsi", Value = InvoiceType.StockIn },
                new InvoiceTypeFilter { Text = "Çıxış qaiməsi", Value = InvoiceType.StockOut }
            };

            TypeCombo.DisplayMemberPath = "Text";
            TypeCombo.SelectedValuePath = "Value";
            TypeCombo.SelectedIndex = 0;

            StatusCombo.ItemsSource = new List<InvoiceStatusFilter>
            {
                new InvoiceStatusFilter { Text = "Hamısı", Value = null },
                new InvoiceStatusFilter { Text = "Draft", Value = InvoiceStatus.Draft },
                new InvoiceStatusFilter { Text = "Təsdiqlənib", Value = InvoiceStatus.Confirmed },
                new InvoiceStatusFilter { Text = "Ləğv edilib", Value = InvoiceStatus.Cancelled }
            };

            StatusCombo.DisplayMemberPath = "Text";
            StatusCombo.SelectedValuePath = "Value";
            StatusCombo.SelectedIndex = 0;
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async System.Threading.Tasks.Task LoadReportAsync()
        {
            try
            {
                var fromDate = FromDatePicker.SelectedDate;
                var toDate = ToDatePicker.SelectedDate;
                var type = (TypeCombo.SelectedItem as InvoiceTypeFilter)?.Value;
                var status = (StatusCombo.SelectedItem as InvoiceStatusFilter)?.Value;
                var search = SearchText.Text?.Trim().ToLower();

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
                    query = query.Where(x =>
                        x.InvoiceNumber.ToLower().Contains(search) ||
                        (x.Supplier != null && x.Supplier.Name.ToLower().Contains(search)) ||
                        (x.Customer != null && x.Customer.Name.ToLower().Contains(search)) ||
                        (x.Supplier != null && x.Supplier.CompanyName != null && x.Supplier.CompanyName.ToLower().Contains(search)) ||
                        (x.Customer != null && x.Customer.CompanyName != null && x.Customer.CompanyName.ToLower().Contains(search)) ||
                        (x.Note != null && x.Note.ToLower().Contains(search)) ||
                        x.Items.Any(i =>
                            i.IsActive &&
                            i.Product != null &&
                            (
                                i.Product.Name.ToLower().Contains(search) ||
                                i.Product.Code.ToLower().Contains(search) ||
                                (i.Product.Category != null && i.Product.Category.Name.ToLower().Contains(search))
                            )));
                }

                var invoices = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .ToListAsync();

                _rows = new List<InvoiceReportLineRow>();

                foreach (var invoice in invoices)
                {
                    var partyName = invoice.Type == InvoiceType.StockIn
                        ? invoice.Supplier?.Name ?? "-"
                        : invoice.Customer?.Name ?? "-";

                    var activeItems = invoice.Items.Where(x => x.IsActive).ToList();

                    if (!activeItems.Any())
                    {
                        _rows.Add(new InvoiceReportLineRow
                        {
                            InvoiceNumber = invoice.InvoiceNumber,
                            InvoiceDateText = invoice.InvoiceDate.ToString("dd.MM.yyyy HH:mm"),
                            TypeText = invoice.Type == InvoiceType.StockIn ? "Giriş" : "Çıxış",
                            StatusText = invoice.Status.ToString(),
                            PartyName = partyName,
                            ProductName = "-",
                            CategoryName = "-",
                            AttributesText = "",
                            ShelfCode = "-",
                            Quantity = 0,
                            Price = 0,
                            LineTotal = 0,
                            InvoiceTotal = invoice.TotalAmount,
                            DebtAmount = invoice.DebtAmount
                        });

                        continue;
                    }

                    foreach (var item in activeItems)
                    {
                        _rows.Add(new InvoiceReportLineRow
                        {
                            InvoiceNumber = invoice.InvoiceNumber,
                            InvoiceDateText = invoice.InvoiceDate.ToString("dd.MM.yyyy HH:mm"),
                            TypeText = invoice.Type == InvoiceType.StockIn ? "Giriş" : "Çıxış",
                            StatusText = invoice.Status.ToString(),
                            PartyName = partyName,
                            ProductName = item.Product?.Name ?? "-",
                            CategoryName = item.Product?.Category?.Name ?? "-",
                            AttributesText = item.Product == null ? "" : GetProductAttributesText(item.Product),
                            ShelfCode = item.Shelf?.Code ?? "-",
                            Quantity = item.Quantity,
                            Price = item.Price,
                            LineTotal = item.Total,
                            InvoiceTotal = invoice.TotalAmount,
                            DebtAmount = invoice.DebtAmount
                        });
                    }
                }

                ReportGrid.ItemsSource = _rows;

                InvoiceCountText.Text = invoices.Count.ToString();
                ItemCountText.Text = _rows.Count.ToString();

                // YENI: Maliyyə kartlarında yalnız təsdiqlənmiş qaimələr hesaba alınır.
                // Draft qaimə real satış, alış və qazanc sayılmır.
                var confirmedInvoices = invoices
                    .Where(x => x.Status == InvoiceStatus.Confirmed)
                    .ToList();

                // YENI: Dövriyyə yalnız təsdiqlənmiş çıxış qaimələridir.
                var salesTotal = confirmedInvoices
                    .Where(x => x.Type == InvoiceType.StockOut)
                    .Sum(x => x.TotalAmount);

                // YENI: Alış xərci yalnız təsdiqlənmiş giriş qaimələridir.
                var purchaseTotal = confirmedInvoices
                    .Where(x => x.Type == InvoiceType.StockIn)
                    .Sum(x => x.TotalAmount);

                // YENI: Sadə net qazanc.
                // Daha professional mərhələdə bunu Product.PurchasePrice əsasında maya ilə hesablayacağıq.
                var netProfit = salesTotal - purchaseTotal;

                TotalAmountText.Text = $"{salesTotal:N2} AZN";
                DebtAmountText.Text = $"{netProfit:N2} AZN";

                ShowMessage($"{invoices.Count} qaimə, {_rows.Count} məhsul sətri tapıldı.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Hesabat yüklənmədi: {ex.Message}", true);
            }
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Ətraflı qaimə hesabatını yadda saxla",
                    Filter = "Excel faylı (*.xlsx)|*.xlsx",
                    FileName = $"Etrafli_Qaime_Hesabati_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                var folderPath = Path.GetDirectoryName(saveDialog.FileName);

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    ShowMessage("Qovluq seçilmədi.", true);
                    return;
                }

                var fromDate = FromDatePicker.SelectedDate;
                var toDate = ToDatePicker.SelectedDate;
                var type = (TypeCombo.SelectedItem as InvoiceTypeFilter)?.Value;
                var status = (StatusCombo.SelectedItem as InvoiceStatusFilter)?.Value;
                var search = SearchText.Text?.Trim();

                var result = await _excelExportService.ExportDetailedInvoiceReportAsync(
                    folderPath: folderPath,
                    fromDate: fromDate,
                    toDate: toDate,
                    type: type,
                    status: status,
                    search: search);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    MessageBox.Show(result.Message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowMessage($"Excel hazırdır: {result.Data}", false);
                MessageBox.Show("Ətraflı qaimə hesabatı Excel faylına çıxarıldı.", "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
                MessageBox.Show(ex.Message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetProductAttributesText(Product product)
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

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError ? Brushes.Firebrick : Brushes.SeaGreen;
        }
    }

    public class InvoiceReportLineRow
    {
        public string InvoiceNumber { get; set; } = "";

        public string InvoiceDateText { get; set; } = "";

        public string TypeText { get; set; } = "";

        public string StatusText { get; set; } = "";

        public string PartyName { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public string AttributesText { get; set; } = "";

        public string ShelfCode { get; set; } = "";

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal LineTotal { get; set; }

        public decimal InvoiceTotal { get; set; }

        public decimal DebtAmount { get; set; }
    }

    public class InvoiceTypeFilter
    {
        public string Text { get; set; } = "";

        public InvoiceType? Value { get; set; }
    }

    public class InvoiceStatusFilter
    {
        public string Text { get; set; } = "";

        public InvoiceStatus? Value { get; set; }
    }
}