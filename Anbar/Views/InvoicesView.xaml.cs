using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Anbar.Views
{
    public partial class InvoicesView : UserControl
    {
        private const string ConnectionString =
            "Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly InvoiceService _invoiceService;

        private List<Invoice> _allInvoices = new();
        private List<InvoiceListRow> _filteredRows = new();

        private int _currentPage = 1;
        private int _pageSize = 20;
        private bool _isInitializing = true;

        public InvoicesView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            _context = new AppDbContext(options);

            // YENI:
            // InvoiceService-in köhnə compatibility constructor-u səndə mövcuddur.
            // Biz confirm/cancel kimi biznes əməliyyatlarını servis üzərindən edirik.
            _stockService = new StockService(_context);
            _invoiceService = new InvoiceService(_context, _stockService);

            Loaded += InvoicesView_Loaded;
            Unloaded += InvoicesView_Unloaded;
        }

        private async void InvoicesView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isInitializing = true;

                PrepareFilters();
                PreparePagination();

                await LoadInvoicesAsync();

                _isInitializing = false;
                ApplyFiltersAndRefresh();
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                MessageBox.Show(ex.Message, "Qaimələr yüklənmədi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InvoicesView_Unloaded(object sender, RoutedEventArgs e)
        {
            // YENI:
            // View bağlananda DbContext saxlanmasın.
            _context.Dispose();
        }

        private void PrepareFilters()
        {
            InvoiceTypeCombo.ItemsSource = new List<FilterItem<InvoiceType?>>
            {
                new() { Text = "Hamısı", Value = null },
                new() { Text = "Giriş", Value = InvoiceType.StockIn },
                new() { Text = "Çıxış", Value = InvoiceType.StockOut },
                new() { Text = "Müştəri geri qaytarma", Value = InvoiceType.CustomerReturnIn },
                new() { Text = "Təchizatçı geri qaytarma", Value = InvoiceType.SupplierReturnOut }
            };
            InvoiceTypeCombo.DisplayMemberPath = "Text";
            InvoiceTypeCombo.SelectedIndex = 0;

            InvoiceStatusCombo.ItemsSource = new List<FilterItem<InvoiceStatus?>>
            {
                new() { Text = "Hamısı", Value = null },
                new() { Text = "Draft", Value = InvoiceStatus.Draft },
                new() { Text = "Təsdiqlənmiş", Value = InvoiceStatus.Confirmed },
                new() { Text = "Ləğv edilmiş", Value = InvoiceStatus.Cancelled }
            };
            InvoiceStatusCombo.DisplayMemberPath = "Text";
            InvoiceStatusCombo.SelectedIndex = 0;

            PaymentStatusCombo.ItemsSource = new List<FilterItem<PaymentStatus?>>
            {
                new() { Text = "Hamısı", Value = null },
                new() { Text = "Ödənilməyib", Value = PaymentStatus.Unpaid },
                new() { Text = "Qismən ödənilib", Value = PaymentStatus.PartialPaid },
                new() { Text = "Ödənilib", Value = PaymentStatus.Paid },
                new() { Text = "Artıq ödəniş", Value = PaymentStatus.OverPaid }
            };
            PaymentStatusCombo.DisplayMemberPath = "Text";
            PaymentStatusCombo.SelectedIndex = 0;

            WarehouseCombo.ItemsSource = new List<string> { "Hamısı" };
            WarehouseCombo.SelectedIndex = 0;

            DateFromPicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateToPicker.SelectedDate = DateTime.Now.Date;
        }

        private void PreparePagination()
        {
            PageSizeCombo.ItemsSource = new List<int> { 10, 20, 50, 100 };
            PageSizeCombo.SelectedItem = _pageSize;
        }

        private async System.Threading.Tasks.Task LoadInvoicesAsync()
        {
            // YENI:
            // Siyahı üçün InvoiceService.GetAllAsync istifadə edirik.
            // Anbar adı üçün rəf -> anbar include lazım olduğuna görə aşağıda Entry-ləri əlavə yükləyirik.
            var result = await _invoiceService.GetAllAsync();

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Warning);
                _allInvoices = new List<Invoice>();
                return;
            }

            var invoiceIds = (result.Data ?? new List<Invoice>())
                .Select(x => x.Id)
                .ToList();

            _allInvoices = await _context.Invoices
                .Include(x => x.Supplier)
                .Include(x => x.Customer)
                .Include(x => x.ParentInvoice)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items.Where(i => i.IsActive))
                    .ThenInclude(x => x.Shelf)
                        .ThenInclude(x => x.Warehouse)
                .Include(x => x.StockMovements.Where(m => m.IsActive))
                .Where(x => invoiceIds.Contains(x.Id) && x.IsActive)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();

            LoadWarehouseFilter();
            UpdateKpiCards();
        }

        private void LoadWarehouseFilter()
        {
            var warehouses = _allInvoices
                .Select(GetWarehouseName)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var items = new List<string> { "Hamısı" };
            items.AddRange(warehouses);

            var oldValue = WarehouseCombo.SelectedItem?.ToString();

            WarehouseCombo.ItemsSource = items;

            if (!string.IsNullOrWhiteSpace(oldValue) && items.Contains(oldValue))
                WarehouseCombo.SelectedItem = oldValue;
            else
                WarehouseCombo.SelectedIndex = 0;
        }

        private void ApplyFiltersAndRefresh()
        {
            if (_isInitializing)
                return;

            var rows = _allInvoices.Select(MapInvoiceToRow).ToList();

            var keyword = SearchTextBox.Text?.Trim().ToLower() ?? "";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                rows = rows.Where(x =>
                        x.InvoiceNumber.ToLower().Contains(keyword) ||
                        x.PartyName.ToLower().Contains(keyword) ||
                        x.WarehouseName.ToLower().Contains(keyword) ||
                        x.TypeText.ToLower().Contains(keyword) ||
                        x.StatusText.ToLower().Contains(keyword) ||
                        x.SourceText.ToLower().Contains(keyword) ||
                        x.CurrencyText.ToLower().Contains(keyword))
                    .ToList();
            }

            if (InvoiceTypeCombo.SelectedItem is FilterItem<InvoiceType?> typeItem && typeItem.Value.HasValue)
                rows = rows.Where(x => x.Type == typeItem.Value.Value).ToList();

            if (InvoiceStatusCombo.SelectedItem is FilterItem<InvoiceStatus?> statusItem && statusItem.Value.HasValue)
                rows = rows.Where(x => x.Status == statusItem.Value.Value).ToList();

            if (PaymentStatusCombo.SelectedItem is FilterItem<PaymentStatus?> paymentItem && paymentItem.Value.HasValue)
                rows = rows.Where(x => x.PaymentStatus == paymentItem.Value.Value).ToList();

            if (DateFromPicker.SelectedDate.HasValue)
            {
                var from = DateFromPicker.SelectedDate.Value.Date;
                rows = rows.Where(x => x.InvoiceDate.Date >= from).ToList();
            }

            if (DateToPicker.SelectedDate.HasValue)
            {
                var to = DateToPicker.SelectedDate.Value.Date;
                rows = rows.Where(x => x.InvoiceDate.Date <= to).ToList();
            }

            var selectedWarehouse = WarehouseCombo.SelectedItem?.ToString();

            if (!string.IsNullOrWhiteSpace(selectedWarehouse) && selectedWarehouse != "Hamısı")
                rows = rows.Where(x => x.WarehouseName == selectedWarehouse).ToList();

            _filteredRows = rows
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .ToList();

            _currentPage = Math.Max(1, Math.Min(_currentPage, GetTotalPages()));

            RefreshGrid();
            UpdateFooter();
        }

        private void RefreshGrid()
        {
            var pageRows = _filteredRows
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            InvoicesGrid.ItemsSource = pageRows;

            PageInfoText.Text = $"{_currentPage} / {GetTotalPages()}";

            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < GetTotalPages();
        }

        private int GetTotalPages()
        {
            if (_filteredRows.Count == 0)
                return 1;

            return (int)Math.Ceiling(_filteredRows.Count / (double)_pageSize);
        }

        private void UpdateKpiCards()
        {
            var today = DateTime.Now.Date;
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var weekStart = DateTime.Now.Date.AddDays(-7);

            var todayCount = _allInvoices.Count(x => x.InvoiceDate.Date == today);
            var draftCount = _allInvoices.Count(x => x.Status == InvoiceStatus.Draft);
            var confirmedCount = _allInvoices.Count(x => x.Status == InvoiceStatus.Confirmed);
            var confirmedWeekCount = _allInvoices.Count(x => x.Status == InvoiceStatus.Confirmed && x.InvoiceDate.Date >= weekStart);
            var importCount = _allInvoices.Count(x => x.IsImport && x.InvoiceDate.Date >= monthStart);
            var returnCount = _allInvoices.Count(x =>
                (x.Type == InvoiceType.CustomerReturnIn || x.Type == InvoiceType.SupplierReturnOut) &&
                x.InvoiceDate.Date >= monthStart);

            var totalAmount = _allInvoices
                .Where(x => x.InvoiceDate.Date >= monthStart && x.Status != InvoiceStatus.Cancelled)
                .Sum(x => x.TotalAmount);

            var totalDebt = _allInvoices
                .Where(x => x.InvoiceDate.Date >= monthStart && x.Status != InvoiceStatus.Cancelled)
                .Sum(x => x.DebtAmount);

            TodayInvoicesCountText.Text = todayCount.ToString();
            TodayInvoicesSubText.Text = $"+{todayCount} bu gün";

            DraftInvoicesCountText.Text = draftCount.ToString();
            DraftInvoicesSubText.Text = $"+{draftCount} yeni";

            ConfirmedInvoicesCountText.Text = confirmedCount.ToString();
            ConfirmedInvoicesSubText.Text = $"+{confirmedWeekCount} bu həftə";

            ImportInvoicesCountText.Text = importCount.ToString();

            ReturnInvoicesCountText.Text = returnCount.ToString();

            TotalInvoiceAmountText.Text = $"{totalAmount:N2} ₼";
            TotalDebtAmountText.Text = $"{totalDebt:N2} ₼";
        }

        private void UpdateFooter()
        {
            var totalAmount = _filteredRows.Sum(x => x.TotalAmount);
            var paidAmount = _filteredRows.Sum(x => x.PaidAmount);
            var debtAmount = _filteredRows.Sum(x => x.DebtAmount);
            var itemCount = _filteredRows.Sum(x => x.ItemCount);

            FooterTotalInvoicesText.Text = $"Ümumi qaimə: {_filteredRows.Count}";
            FooterTotalAmountText.Text = $"Ümumi məbləğ: {totalAmount:N2} ₼";
            FooterPaidAmountText.Text = $"Ödənilib: {paidAmount:N2} ₼";
            FooterDebtAmountText.Text = $"Qalıq borc: {debtAmount:N2} ₼";
            FooterItemCountText.Text = $"Item sayı: {itemCount:N0}";
        }

        private InvoiceListRow MapInvoiceToRow(Invoice invoice)
        {
            var warehouseName = GetWarehouseName(invoice);

            return new InvoiceListRow
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceDateText = invoice.InvoiceDate.ToString("dd.MM.yyyy\nHH:mm"),

                Type = invoice.Type,
                TypeText = GetInvoiceTypeText(invoice.Type),
                TypeIcon = GetInvoiceTypeIcon(invoice.Type),
                TypeBadgeBackground = GetInvoiceTypeBackground(invoice.Type, invoice.IsImport),
                TypeBadgeForeground = GetInvoiceTypeForeground(invoice.Type, invoice.IsImport),

                SourceText = invoice.IsImport ? "İdxal" : "Yerli",
                PartyName = GetPartyName(invoice),
                WarehouseName = warehouseName,

                CurrencyText = invoice.Currency.ToString(),
                ExchangeRateText = invoice.ExchangeRate.ToString("N4"),

                Status = invoice.Status,
                StatusText = GetStatusText(invoice.Status),
                StatusBadgeBackground = GetStatusBackground(invoice.Status),
                StatusBadgeForeground = GetStatusForeground(invoice.Status),

                PaymentStatus = invoice.PaymentStatus,
                PaymentStatusText = GetPaymentStatusText(invoice.PaymentStatus),

                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                DebtAmount = invoice.DebtAmount,

                TotalAmountText = invoice.TotalAmount.ToString("N2"),
                PaidAmountText = invoice.PaidAmount.ToString("N2"),
                DebtAmountText = invoice.DebtAmount.ToString("N2"),
                DebtForeground = invoice.DebtAmount > 0 ? Brushes.Red : Brushes.Green,

                ItemCount = invoice.Items?.Count(x => x.IsActive) ?? 0,

                CostStatusText = GetCostStatusText(invoice.CostStatus),
                StockStatusText = GetStockStatusText(invoice),

                CreatedByText = "Admin"
            };
        }

        private string GetPartyName(Invoice invoice)
        {
            if (invoice.Type == InvoiceType.StockIn || invoice.Type == InvoiceType.SupplierReturnOut)
            {
                if (!string.IsNullOrWhiteSpace(invoice.Supplier?.CompanyName))
                    return invoice.Supplier.CompanyName;

                return invoice.Supplier?.Name ?? "-";
            }

            if (!string.IsNullOrWhiteSpace(invoice.Customer?.CompanyName))
                return invoice.Customer.CompanyName;

            return invoice.Customer?.Name ?? "-";
        }

        private string GetWarehouseName(Invoice invoice)
        {
            var warehouseName = invoice.Items?
                .Where(x => x.IsActive)
                .Select(x => x.Shelf?.Warehouse?.Name)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            return string.IsNullOrWhiteSpace(warehouseName) ? "-" : warehouseName;
        }

        private string GetInvoiceTypeText(InvoiceType type)
        {
            return type switch
            {
                InvoiceType.StockIn => "Giriş",
                InvoiceType.StockOut => "Çıxış",
                InvoiceType.CustomerReturnIn => "Geri qaytarma",
                InvoiceType.SupplierReturnOut => "Təh. return",
                _ => "Naməlum"
            };
        }

        private string GetInvoiceTypeIcon(InvoiceType type)
        {
            return type switch
            {
                InvoiceType.StockIn => "\uE8A5",
                InvoiceType.StockOut => "\uE8A5",
                InvoiceType.CustomerReturnIn => "\uE8BB",
                InvoiceType.SupplierReturnOut => "\uE8BB",
                _ => "\uE8A5"
            };
        }

        private Brush GetInvoiceTypeBackground(InvoiceType type, bool isImport)
        {
            if (isImport)
                return new SolidColorBrush(Color.FromRgb(254, 243, 199));

            return type switch
            {
                InvoiceType.StockIn => new SolidColorBrush(Color.FromRgb(220, 252, 231)),
                InvoiceType.StockOut => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                InvoiceType.CustomerReturnIn => new SolidColorBrush(Color.FromRgb(243, 232, 255)),
                InvoiceType.SupplierReturnOut => new SolidColorBrush(Color.FromRgb(255, 237, 213)),
                _ => new SolidColorBrush(Color.FromRgb(241, 245, 249))
            };
        }

        private Brush GetInvoiceTypeForeground(InvoiceType type, bool isImport)
        {
            if (isImport)
                return new SolidColorBrush(Color.FromRgb(217, 119, 6));

            return type switch
            {
                InvoiceType.StockIn => new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                InvoiceType.StockOut => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                InvoiceType.CustomerReturnIn => new SolidColorBrush(Color.FromRgb(147, 51, 234)),
                InvoiceType.SupplierReturnOut => new SolidColorBrush(Color.FromRgb(234, 88, 12)),
                _ => new SolidColorBrush(Color.FromRgb(71, 85, 105))
            };
        }

        private string GetStatusText(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Draft => "Draft",
                InvoiceStatus.Confirmed => "Təsdiqlənmiş",
                InvoiceStatus.Cancelled => "Ləğv edilmiş",
                _ => "Naməlum"
            };
        }

        private Brush GetStatusBackground(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Draft => new SolidColorBrush(Color.FromRgb(254, 243, 199)),
                InvoiceStatus.Confirmed => new SolidColorBrush(Color.FromRgb(220, 252, 231)),
                InvoiceStatus.Cancelled => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                _ => new SolidColorBrush(Color.FromRgb(241, 245, 249))
            };
        }

        private Brush GetStatusForeground(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Draft => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
                InvoiceStatus.Confirmed => new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                InvoiceStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                _ => new SolidColorBrush(Color.FromRgb(71, 85, 105))
            };
        }

        private string GetPaymentStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Unpaid => "Ödənilməyib",
                PaymentStatus.PartialPaid => "Qismən ödənilib",
                PaymentStatus.Paid => "Ödənilib",
                PaymentStatus.OverPaid => "Artıq ödənilib",
                _ => "-"
            };
        }

        private string GetCostStatusText(CostRecalculationStatus status)
        {
            return status switch
            {
                CostRecalculationStatus.NotCalculated => "-",
                CostRecalculationStatus.Calculated => "Hazır",
                CostRecalculationStatus.NeedsRecalculation => "Hesablanır",
                CostRecalculationStatus.Locked => "Hazır",
                _ => "-"
            };
        }

        private string GetStockStatusText(Invoice invoice)
        {
            if (invoice.Status == InvoiceStatus.Draft)
                return "Stoklanmayıb";

            if (invoice.Status == InvoiceStatus.Cancelled)
                return "-";

            return invoice.Type switch
            {
                InvoiceType.StockIn => "Stoklandı",
                InvoiceType.CustomerReturnIn => "Stoklandı",
                InvoiceType.StockOut => "Çıxarıldı",
                InvoiceType.SupplierReturnOut => "Çıxarıldı",
                _ => "-"
            };
        }

        private async void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Yeni qaimə yaratma paneli növbəti mərhələdə InvoiceDetailView ilə açılacaq.\n\nƏvvəl bu list ekranını tam servisə bağladıqdan sonra Add/Edit ekranına keçəcəyik.",
                "Yeni qaimə",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await LoadInvoicesAsync();
            ApplyFiltersAndRefresh();
        }

        private void Excel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Excel export növbəti mərhələdə ExcelExportService ilə bağlanacaq.",
                "Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Çap funksiyası növbəti mərhələdə qaimə print/receipt dizaynı ilə bağlanacaq.",
                "Çap et",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Columns_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Sütun seçimi növbəti mərhələdə əlavə olunacaq.",
                "Sütunlar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MoreFilter_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Daha çox filter panelinə sonra məbləğ aralığı, borc statusu, maya statusu, stok statusu əlavə edəcəyik.",
                "Filter",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void InvoiceTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void InvoiceStatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void PaymentStatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void DateFilter_Changed(object? sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void WarehouseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _currentPage = 1;
            ApplyFiltersAndRefresh();
        }

        private void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (PageSizeCombo.SelectedItem is int selectedSize)
                _pageSize = selectedSize;

            _currentPage = 1;
            RefreshGrid();
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1)
                return;

            _currentPage--;
            RefreshGrid();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage >= GetTotalPages())
                return;

            _currentPage++;
            RefreshGrid();
        }

        private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // YENI:
            // Hələlik selection yalnız row highlight üçündür.
            // Tam preview/detail növbəti InvoiceDetailView mərhələsində açılacaq.
        }

        private void InvoicesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is InvoiceListRow row)
                OpenInvoiceDetail(row.Id, readOnly: false);
        }

        private void ViewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invoiceId)
                OpenInvoiceDetail(invoiceId, readOnly: true);
        }

        private void EditInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invoiceId)
                OpenInvoiceDetail(invoiceId, readOnly: false);
        }

        private void PrintInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invoiceId)
            {
                MessageBox.Show(
                    $"Qaimə çapı növbəti mərhələdə bağlanacaq. InvoiceId: {invoiceId}",
                    "Çap",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async void MoreInvoiceActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int invoiceId)
                return;

            var invoice = _allInvoices.FirstOrDefault(x => x.Id == invoiceId);

            if (invoice == null)
                return;

            if (invoice.Status == InvoiceStatus.Draft)
            {
                var result = MessageBox.Show(
                    $"{invoice.InvoiceNumber} draft qaiməsini təsdiqləmək istəyirsiniz?",
                    "Qaiməni təsdiqlə",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var confirmResult = await _invoiceService.ConfirmAsync(invoiceId);

                    if (!confirmResult.IsSuccess)
                    {
                        MessageBox.Show(confirmResult.Message, "Təsdiq xətası", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await LoadInvoicesAsync();
                    ApplyFiltersAndRefresh();

                    MessageBox.Show(confirmResult.Message, "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (result == MessageBoxResult.No)
                {
                    var cancelResult = await _invoiceService.CancelAsync(invoiceId);

                    if (!cancelResult.IsSuccess)
                    {
                        MessageBox.Show(cancelResult.Message, "Ləğv xətası", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await LoadInvoicesAsync();
                    ApplyFiltersAndRefresh();

                    MessageBox.Show(cancelResult.Message, "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(
                    "Confirmed qaimələr birbaşa ləğv edilmir. FIFO düzgünlüyü üçün geri qaytarma qaiməsi yaradılmalıdır.",
                    "Enterprise qayda",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
      
        private void OpenInvoiceDetail(int invoiceId, bool readOnly)
        {
            var window = Window.GetWindow(this) as MainWindow;

            if (window == null)
            {
                MessageBox.Show("MainWindow tapılmadı.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            window.OpenView(
                new InvoiceDetailView(invoiceId, readOnly),
                readOnly ? "Qaiməyə baxış" : "Qaimə detalı",
                readOnly
                    ? "Qaimə məlumatları read-only rejimdə göstərilir"
                    : "Qaimə məhsulları, stok hərəkətləri, maya və təsdiq əməliyyatları");
        }
       
    }

    public class InvoiceListRow
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public string InvoiceDateText { get; set; } = string.Empty;

        public InvoiceType Type { get; set; }

        public string TypeText { get; set; } = string.Empty;

        public string TypeIcon { get; set; } = string.Empty;

        public Brush TypeBadgeBackground { get; set; } = Brushes.Transparent;

        public Brush TypeBadgeForeground { get; set; } = Brushes.Black;

        public string SourceText { get; set; } = string.Empty;

        public string PartyName { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;

        public string CurrencyText { get; set; } = string.Empty;

        public string ExchangeRateText { get; set; } = string.Empty;

        public InvoiceStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public Brush StatusBadgeBackground { get; set; } = Brushes.Transparent;

        public Brush StatusBadgeForeground { get; set; } = Brushes.Black;

        public PaymentStatus PaymentStatus { get; set; }

        public string PaymentStatusText { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DebtAmount { get; set; }

        public string TotalAmountText { get; set; } = string.Empty;

        public string PaidAmountText { get; set; } = string.Empty;

        public string DebtAmountText { get; set; } = string.Empty;

        public Brush DebtForeground { get; set; } = Brushes.Black;

        public int ItemCount { get; set; }

        public string CostStatusText { get; set; } = string.Empty;

        public string StockStatusText { get; set; } = string.Empty;

        public string CreatedByText { get; set; } = string.Empty;
    }

    public class FilterItem<T>
    {
        public string Text { get; set; } = string.Empty;

        public T? Value { get; set; }
    }
}