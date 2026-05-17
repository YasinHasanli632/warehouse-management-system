using Anbar.Data;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Anbar.Views
{
    public partial class OutputInvoicesView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ExcelExportService _excelExportService;

        public OutputInvoicesView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _excelExportService = new ExcelExportService(_context, new ReportService(_context));

            Loaded += OutputInvoicesView_Loaded;
        }

        private async void OutputInvoicesView_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareFilters();

            FromDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Now;

            await LoadDataAsync();
        }

        private void PrepareFilters()
        {
            StatusCombo.ItemsSource = Enum.GetValues(typeof(InvoiceStatus));
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                var query = _context.Invoices
                    .Include(x => x.Customer)
                    .Where(x => x.Type == InvoiceType.StockOut && x.IsActive)
                    .AsQueryable();

                if (FromDatePicker.SelectedDate.HasValue)
                {
                    var fromDate = FromDatePicker.SelectedDate.Value.Date;
                    query = query.Where(x => x.InvoiceDate.Date >= fromDate);
                }

                if (ToDatePicker.SelectedDate.HasValue)
                {
                    var toDate = ToDatePicker.SelectedDate.Value.Date;
                    query = query.Where(x => x.InvoiceDate.Date <= toDate);
                }

                if (StatusCombo.SelectedItem is InvoiceStatus selectedStatus)
                    query = query.Where(x => x.Status == selectedStatus);

                var search = SearchText.Text?.Trim().ToLower();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.InvoiceNumber.ToLower().Contains(search) ||
                        (x.Customer != null && x.Customer.Name.ToLower().Contains(search)) ||
                        (x.Customer != null && x.Customer.CompanyName != null && x.Customer.CompanyName.ToLower().Contains(search)) ||
                        (x.Note != null && x.Note.ToLower().Contains(search)));
                }

                var list = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .Select(x => new OutputInvoiceRow
                    {
                        Id = x.Id,
                        InvoiceNumber = x.InvoiceNumber,
                        InvoiceDateText = x.InvoiceDate.ToString("dd.MM.yyyy HH:mm"),
                        StatusText = x.Status.ToString(),
                        PartyName = x.Customer != null ? x.Customer.Name : "-",
                        TotalAmount = x.TotalAmount,
                        PaidAmount = x.PaidAmount,
                        DebtAmount = x.DebtAmount
                    })
                    .ToListAsync();

                InvoicesGrid.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Çıxış qaimələri Excel export",
                    Filter = "Excel faylı (*.xlsx)|*.xlsx",
                    FileName = $"Cixis_Qaimeleri_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (dialog.ShowDialog() != true)
                    return;

                var folder = Path.GetDirectoryName(dialog.FileName);

                if (string.IsNullOrWhiteSpace(folder))
                {
                    MessageBox.Show("Qovluq seçilmədi.", "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = await _excelExportService.ExportDetailedInvoiceReportAsync(
                    folderPath: folder,
                    fromDate: FromDatePicker.SelectedDate,
                    toDate: ToDatePicker.SelectedDate,
                    type: InvoiceType.StockOut,
                    status: StatusCombo.SelectedItem is InvoiceStatus s ? s : null,
                    search: SearchText.Text);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.Message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("Çıxış qaimələri Excel faylına çıxarıldı.", "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class OutputInvoiceRow
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = "";

        public string InvoiceDateText { get; set; } = "";

        public string StatusText { get; set; } = "";

        public string PartyName { get; set; } = "";

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DebtAmount { get; set; }
    }
}