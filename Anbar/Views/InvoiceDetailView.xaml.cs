using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Anbar.Views
{
    public partial class InvoiceDetailView : UserControl
    {
        private const string ConnectionString =
            "Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        private readonly int _invoiceId;
        private readonly bool _readOnly;

        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly InvoiceService _invoiceService;

        private Invoice? _invoice;

        public InvoiceDetailView(int invoiceId, bool readOnly = false)
        {
            InitializeComponent();

            _invoiceId = invoiceId;
            _readOnly = readOnly;

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            _context = new AppDbContext(options);
            _stockService = new StockService(_context);
            _invoiceService = new InvoiceService(_context, _stockService);

            Loaded += InvoiceDetailView_Loaded;
            Unloaded += InvoiceDetailView_Unloaded;
        }

        private async void InvoiceDetailView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadInvoiceAsync();
        }

        private void InvoiceDetailView_Unloaded(object sender, RoutedEventArgs e)
        {
            _context.Dispose();
        }

        private async System.Threading.Tasks.Task LoadInvoiceAsync()
        {
            try
            {
                var result = await _invoiceService.GetByIdAsync(_invoiceId);

                if (!result.IsSuccess || result.Data == null)
                {
                    MessageBox.Show(result.Message, "Qaimə tapılmadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _invoice = result.Data;

                await _context.Entry(_invoice).Collection(x => x.Items).LoadAsync();

                foreach (var item in _invoice.Items.Where(x => x.IsActive))
                {
                    await _context.Entry(item).Reference(x => x.Product).LoadAsync();
                    await _context.Entry(item).Reference(x => x.Shelf).LoadAsync();
                }

                await _context.Entry(_invoice).Collection(x => x.StockMovements).LoadAsync();

                foreach (var movement in _invoice.StockMovements.Where(x => x.IsActive))
                {
                    await _context.Entry(movement).Reference(x => x.Product).LoadAsync();
                }

                BindHeader();
                BindItems();
                ApplyMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Qaimə detalı yüklənmədi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BindHeader()
        {
            if (_invoice == null)
                return;

            TitleText.Text = _invoice.InvoiceNumber;
            StatusText.Text = GetStatusText(_invoice.Status);
            InvoiceTypeText.Text = GetTypeText(_invoice.Type);

            SubtitleText.Text =
                $"{GetPartyLabel(_invoice)}: {GetPartyName(_invoice)}  •  Anbar: {GetWarehouseName(_invoice)}  •  Tarix: {_invoice.InvoiceDate:dd.MM.yyyy HH:mm}  •  Sənəd nömrəsi: {_invoice.InvoiceNumber}";

            InvoiceNumberText.Text = _invoice.InvoiceNumber;
            PartyNameText.Text = GetPartyName(_invoice);
            InvoiceDateText.Text = _invoice.InvoiceDate.ToString("dd.MM.yyyy");
            PaymentStatusText.Text = GetPaymentStatusText(_invoice.PaymentStatus);

            SourceText.Text = _invoice.IsImport ? "İdxal qaiməsi" : "Yerli qaimə";
            CurrencyText.Text = $"{_invoice.Currency} / {_invoice.ExchangeRate:N4}";

            TotalAmountText.Text = $"{_invoice.TotalAmount:N2} {_invoice.Currency}";
            PaidAmountText.Text = $"{_invoice.PaidAmount:N2} {_invoice.Currency}";
            DebtAmountText.Text = $"{_invoice.DebtAmount:N2} {_invoice.Currency}";
            CostStatusText.Text = $"Maya statusu        {GetCostStatusText(_invoice.CostStatus)}";

            NoteText.Text = string.IsNullOrWhiteSpace(_invoice.Note)
                ? "Qeyd yazın..."
                : _invoice.Note;
        }

        private void BindItems()
        {
            if (_invoice == null)
                return;

            var rows = _invoice.Items
                .Where(x => x.IsActive)
                .Select((x, index) => new InvoiceDetailItemRow
                {
                    RowNo = index + 1,
                    ProductName = x.Product?.Name ?? "-",
                    UnitText = GetProductUnitText(x.Product),
                    ShelfCode = x.Shelf?.Code ?? "-",
                    QuantityText = x.Quantity.ToString("N2"),
                    PriceText = x.Price.ToString("N2"),
                    NetText = GetInvoiceItemNetAmount(x).ToString("N2"),
                    VatText = GetInvoiceItemVatAmount(x).ToString("N2"),
                    TotalText = x.Total.ToString("N2"),
                    BatchText = GetInvoiceItemBatchText(x)
                })
                .ToList();

            ItemsGrid.ItemsSource = rows;
        }

        private void ApplyMode()
        {
            if (_invoice == null)
                return;

            var isDraft = _invoice.Status == InvoiceStatus.Draft;

            AddItemButton.IsEnabled = isDraft && !_readOnly;
            ConfirmButton.IsEnabled = isDraft && !_readOnly;
            CancelButton.IsEnabled = isDraft && !_readOnly;

            if (_readOnly)
            {
                AddItemButton.Visibility = Visibility.Collapsed;
                ConfirmButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as MainWindow;

            if (window == null)
            {
                MessageBox.Show("MainWindow tapılmadı.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            window.OpenView(
                new InvoicesView(),
                "Bütün qaimələr",
                "Bütün növ qaimələrin siyahısı və idarə edilməsi");
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Məhsul əlavə paneli növbəti mərhələdə yazılacaq.",
                "Məhsul əlavə et",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice == null)
                return;

            var confirm = MessageBox.Show(
                $"{_invoice.InvoiceNumber} qaiməsi təsdiqlənsin?\n\nTəsdiqdən sonra stok/FIFO hərəkətləri yazılacaq.",
                "Qaiməni təsdiqlə",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _invoiceService.ConfirmAsync(_invoice.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message, "Təsdiq xətası", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(result.Message, "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadInvoiceAsync();
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice == null)
                return;

            var confirm = MessageBox.Show(
                $"{_invoice.InvoiceNumber} draft qaiməsi ləğv edilsin?",
                "Qaiməni ləğv et",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _invoiceService.CancelAsync(_invoice.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message, "Ləğv xətası", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(result.Message, "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadInvoiceAsync();
        }

        private string GetPartyLabel(Invoice invoice)
        {
            return invoice.Type == InvoiceType.StockIn || invoice.Type == InvoiceType.SupplierReturnOut
                ? "Təchizatçı"
                : "Müştəri";
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

            return string.IsNullOrWhiteSpace(warehouseName) ? "Baş Anbar" : warehouseName;
        }

        private string GetTypeText(InvoiceType type)
        {
            return type switch
            {
                InvoiceType.StockIn => "Giriş qaiməsi",
                InvoiceType.StockOut => "Çıxış qaiməsi",
                InvoiceType.CustomerReturnIn => "Geri qaytarma",
                InvoiceType.SupplierReturnOut => "Təchizatçı return",
                _ => "Naməlum"
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
                CostRecalculationStatus.NotCalculated => "Hesablanmayıb",
                CostRecalculationStatus.Calculated => "Hesablanıb",
                CostRecalculationStatus.NeedsRecalculation => "Yenidən hesablanmalıdır",
                CostRecalculationStatus.Locked => "Kilidlənib",
                _ => "-"
            };
        }

        private string GetProductUnitText(Product? product)
        {
            if (product == null)
                return "-";

            var unitProp = product.GetType().GetProperty("Unit");

            if (unitProp != null)
            {
                var value = unitProp.GetValue(product);
                return value?.ToString() ?? "-";
            }

            var unitNameProp = product.GetType().GetProperty("UnitName");

            if (unitNameProp != null)
            {
                var value = unitNameProp.GetValue(product);
                return value?.ToString() ?? "-";
            }

            return "-";
        }

        private decimal GetInvoiceItemNetAmount(InvoiceItem item)
        {
            var reflected = ReadDecimal(item, "NetAmount");

            if (reflected.HasValue)
                return reflected.Value;

            return item.Quantity * item.Price;
        }

        private decimal GetInvoiceItemVatAmount(InvoiceItem item)
        {
            var reflected = ReadDecimal(item, "VatAmount");

            if (reflected.HasValue)
                return reflected.Value;

            var total = item.Total;
            var net = GetInvoiceItemNetAmount(item);

            if (total > net)
                return total - net;

            return 0;
        }

        private string GetInvoiceItemBatchText(InvoiceItem item)
        {
            var batchNumber = ReadString(item, "BatchNumber");

            if (!string.IsNullOrWhiteSpace(batchNumber))
                return batchNumber;

            var stockBatchId = ReadNullableInt(item, "StockBatchId");

            if (stockBatchId.HasValue)
                return $"BATCH-{stockBatchId.Value}";

            return "-";
        }

        private decimal? ReadDecimal(object source, string propertyName)
        {
            var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                return null;

            var value = prop.GetValue(source);

            if (value == null)
                return null;

            if (value is decimal decimalValue)
                return decimalValue;

            if (decimal.TryParse(value.ToString(), out var parsed))
                return parsed;

            return null;
        }

        private int? ReadNullableInt(object source, string propertyName)
        {
            var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                return null;

            var value = prop.GetValue(source);

            if (value == null)
                return null;

            if (value is int intValue)
                return intValue;

            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;

            return null;
        }

        private string? ReadString(object source, string propertyName)
        {
            var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                return null;

            return prop.GetValue(source)?.ToString();
        }
    }

    public class InvoiceDetailItemRow
    {
        public int RowNo { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitText { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public string QuantityText { get; set; } = string.Empty;
        public string PriceText { get; set; } = string.Empty;
        public string NetText { get; set; } = string.Empty;
        public string VatText { get; set; } = string.Empty;
        public string TotalText { get; set; } = string.Empty;
        public string BatchText { get; set; } = string.Empty;
    }
}