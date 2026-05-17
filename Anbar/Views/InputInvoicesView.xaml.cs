using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Anbar.Views
{
    public partial class InputInvoicesView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ExcelExportService _excelExportService;

        private List<Invoice> _invoices = new();

        public InputInvoicesView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);

            _excelExportService = new ExcelExportService(_context, new ReportService(_context));

            Loaded += InputInvoicesView_Loaded;
        }

        private async void InputInvoicesView_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareFilters();

            FromDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Now;

            await LoadData();
        }

        private void PrepareFilters()
        {
            StatusCombo.ItemsSource = Enum.GetValues(typeof(InvoiceStatus));
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            var query = _context.Invoices
                .Include(x => x.Supplier)
                .Where(x => x.Type == InvoiceType.StockIn && x.IsActive);

            if (FromDatePicker.SelectedDate.HasValue)
                query = query.Where(x => x.InvoiceDate >= FromDatePicker.SelectedDate);

            if (ToDatePicker.SelectedDate.HasValue)
                query = query.Where(x => x.InvoiceDate <= ToDatePicker.SelectedDate);

            if (StatusCombo.SelectedItem is InvoiceStatus status)
                query = query.Where(x => x.Status == status);

            var list = await query
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();

            var search = SearchText.Text?.ToLower();

            if (!string.IsNullOrWhiteSpace(search))
            {
                list = list.Where(x =>
                    x.InvoiceNumber.ToLower().Contains(search) ||
                    (x.Supplier != null && x.Supplier.Name.ToLower().Contains(search))
                ).ToList();
            }

            InvoicesGrid.ItemsSource = list.Select(x => new
            {
                x.Id,
                x.InvoiceNumber,
                InvoiceDateText = x.InvoiceDate.ToString("dd.MM.yyyy HH:mm"),
                StatusText = x.Status.ToString(),
                PartyName = x.Supplier?.Name,
                x.TotalAmount,
                x.DebtAmount
            });
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            var folder = System.IO.Path.GetDirectoryName(dialog.FileName);

            await _excelExportService.ExportDetailedInvoiceReportAsync(
                folderPath: folder,
                fromDate: FromDatePicker.SelectedDate,
                toDate: ToDatePicker.SelectedDate,
                type: InvoiceType.StockIn,
                status: StatusCombo.SelectedItem as InvoiceStatus?,
                search: SearchText.Text);
        }

        private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // İstəsən burda detail popup açarıq sonradan
        }
    }
}