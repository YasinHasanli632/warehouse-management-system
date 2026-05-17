using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;  
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Anbar.Views
{
    public partial class StockOutView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ShelfService _shelfService;
        private readonly CustomerService _customerService;
        private readonly StockService _stockService;
        private readonly InvoiceService _invoiceService;

        // YENI:
        // Category → SubCategory → Shelf → Product axını üçün servis.
        private readonly CategoryService _categoryService;

        private Invoice? _currentInvoice;

        private readonly List<StockOutItemRow> _rows = new();

        private List<Shelf> _shelves = new();
        private List<Customer> _customers = new();

        // YENI:
        // Kateqoriya siyahısı yadda saxlanılır ki, category/subcategory dəyişəndə məhsullar filterlənsin.
        private List<Category> _categories = new();

        private List<StockOutProductComboRow> _availableProductsInShelf = new();

        private bool _isLoadingLookups = false;

        public StockOutView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);

            _stockService = new StockService(_context);
            _shelfService = new ShelfService(_context, _stockService);
            _customerService = new CustomerService(_context);
            _invoiceService = new InvoiceService(_context, _stockService);

            // YENI:
            _categoryService = new CategoryService(_context);

            Loaded += StockOutView_Loaded;
        }

        private async void StockOutView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLookupsAsync();
            RefreshGrid();
        }

        private async System.Threading.Tasks.Task LoadLookupsAsync()
        {
            try
            {
                _isLoadingLookups = true;

                var selectedCustomerId = GetSelectedCustomerId();
                var selectedShelfId = ShelfCombo.SelectedValue == null ? 0 : Convert.ToInt32(ShelfCombo.SelectedValue);
                var selectedMainCategoryId = MainCategoryCombo.SelectedValue == null ? 0 : Convert.ToInt32(MainCategoryCombo.SelectedValue);
                var selectedSubCategoryId = SubCategoryCombo.SelectedValue == null ? 0 : Convert.ToInt32(SubCategoryCombo.SelectedValue);

                var customersResult = await _customerService.GetAllAsync();

                if (customersResult.IsSuccess && customersResult.Data != null)
                {
                    _customers = customersResult.Data
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.Name)
                        .ToList();

                    CustomerCombo.SelectionChanged -= CustomerCombo_SelectionChanged;
                    CustomerCombo.ItemsSource = _customers;
                    CustomerCombo.DisplayMemberPath = "Name";
                    CustomerCombo.SelectedValuePath = "Id";
                    CustomerCombo.SelectionChanged += CustomerCombo_SelectionChanged;

                    LoadPaymentTypeCombo();
                    LoadCurrencyCombo();

                    if (selectedCustomerId > 0 && _customers.Any(x => x.Id == selectedCustomerId))
                        CustomerCombo.SelectedValue = selectedCustomerId;
                    else if (_customers.Any())
                        CustomerCombo.SelectedIndex = 0;
                }

                // YENI:
                // Kateqoriyalar yüklənir.
                var categoriesResult = await _categoryService.GetAllAsync();

                if (categoriesResult.IsSuccess && categoriesResult.Data != null)
                {
                    _categories = categoriesResult.Data
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.Name)
                        .ToList();

                    MainCategoryCombo.SelectionChanged -= MainCategoryCombo_SelectionChanged;
                    MainCategoryCombo.ItemsSource = _categories
                        .Where(x => x.ParentCategoryId == null)
                        .OrderBy(x => x.Name)
                        .ToList();
                    MainCategoryCombo.DisplayMemberPath = "Name";
                    MainCategoryCombo.SelectedValuePath = "Id";

                    if (selectedMainCategoryId > 0 && _categories.Any(x => x.Id == selectedMainCategoryId))
                        MainCategoryCombo.SelectedValue = selectedMainCategoryId;
                    else
                        MainCategoryCombo.SelectedIndex = -1;

                    MainCategoryCombo.SelectionChanged += MainCategoryCombo_SelectionChanged;

                    BindSubCategories(selectedSubCategoryId);
                }

                var shelvesResult = await _shelfService.GetAllAsync();

                if (shelvesResult.IsSuccess && shelvesResult.Data != null)
                {
                    _shelves = shelvesResult.Data
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.Zone)
                        .ThenBy(x => x.RowNumber)
                        .ToList();

                    ShelfCombo.SelectionChanged -= ShelfCombo_SelectionChanged;
                    ShelfCombo.ItemsSource = _shelves;

                    if (selectedShelfId > 0 && _shelves.Any(x => x.Id == selectedShelfId))
                        ShelfCombo.SelectedValue = selectedShelfId;
                    else
                        ShelfCombo.SelectedIndex = -1;

                    ShelfCombo.SelectionChanged += ShelfCombo_SelectionChanged;
                }

                ProductCombo.ItemsSource = null;
                SelectedProductInfoText.Text = "Əvvəl kateqoriya, rəf, sonra məhsul seçin.";

                SetMessage("Məlumatlar yükləndi. Yeni çıxış qaiməsi yarada bilərsiniz.", false);
            }
            catch (Exception ex)
            {
                ShowError($"Məlumatlar yüklənmədi: {ex.Message}");
            }
            finally
            {
                _isLoadingLookups = false;
                ApplySelectedCustomerDefaults();
                BindProductsBySelectedShelfAndCategory();
            }
        }

        private void MainCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            BindSubCategories();
            BindProductsBySelectedShelfAndCategory();
        }

        private void SubCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            BindProductsBySelectedShelfAndCategory();
        }

        private void BindSubCategories(int selectedSubCategoryId = 0)
        {
            SubCategoryCombo.SelectionChanged -= SubCategoryCombo_SelectionChanged;

            if (MainCategoryCombo.SelectedValue == null)
            {
                SubCategoryCombo.ItemsSource = null;
                SubCategoryCombo.SelectedIndex = -1;
                SubCategoryCombo.IsEnabled = false;
                SubCategoryCombo.SelectionChanged += SubCategoryCombo_SelectionChanged;
                return;
            }

            var mainCategoryId = Convert.ToInt32(MainCategoryCombo.SelectedValue);

            var subs = _categories
                .Where(x => x.ParentCategoryId == mainCategoryId)
                .OrderBy(x => x.Name)
                .ToList();

            SubCategoryCombo.ItemsSource = subs;
            SubCategoryCombo.DisplayMemberPath = "Name";
            SubCategoryCombo.SelectedValuePath = "Id";
            SubCategoryCombo.IsEnabled = subs.Any();

            if (selectedSubCategoryId > 0 && subs.Any(x => x.Id == selectedSubCategoryId))
                SubCategoryCombo.SelectedValue = selectedSubCategoryId;
            else
                SubCategoryCombo.SelectedIndex = -1;

            SubCategoryCombo.SelectionChanged += SubCategoryCombo_SelectionChanged;
        }

        private int GetSelectedCategoryId()
        {
            if (SubCategoryCombo.SelectedValue != null)
                return Convert.ToInt32(SubCategoryCombo.SelectedValue);

            if (MainCategoryCombo.SelectedValue != null)
                return Convert.ToInt32(MainCategoryCombo.SelectedValue);

            return 0;
        }

        private void LoadPaymentTypeCombo()
        {
            PaymentTypeCombo.SelectionChanged -= PaymentTypeCombo_SelectionChanged;

            PaymentTypeCombo.ItemsSource = Enum.GetValues(typeof(PaymentType))
                .Cast<PaymentType>()
                .Select(x => new PaymentTypeComboRow
                {
                    Id = x,
                    Name = GetPaymentTypeText(x)
                })
                .ToList();

            PaymentTypeCombo.DisplayMemberPath = "Name";
            PaymentTypeCombo.SelectedValuePath = "Id";
            PaymentTypeCombo.SelectionChanged += PaymentTypeCombo_SelectionChanged;
        }

        private void LoadCurrencyCombo()
        {
            CurrencyCombo.SelectionChanged -= CurrencyCombo_SelectionChanged;

            CurrencyCombo.ItemsSource = Enum.GetValues(typeof(CurrencyType))
                .Cast<CurrencyType>()
                .Select(x => new CurrencyComboRow
                {
                    Id = x,
                    Name = GetCurrencyText(x)
                })
                .ToList();

            CurrencyCombo.DisplayMemberPath = "Name";
            CurrencyCombo.SelectedValuePath = "Id";
            CurrencyCombo.SelectionChanged += CurrencyCombo_SelectionChanged;
        }

        private void CustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            ApplySelectedCustomerDefaults();
        }

        private void PaymentTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            RefreshGrid();
        }

        private void CurrencyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            RefreshGrid();
        }

        private void ApplySelectedCustomerDefaults()
        {
            var customer = GetSelectedCustomerFromCombo();

            if (customer == null)
                return;

            PaymentTypeCombo.SelectedValue = customer.PaymentType;
            CurrencyCombo.SelectedValue = customer.Currency;

            RefreshGrid();
        }

        private PaymentType GetSelectedPaymentType()
        {
            if (PaymentTypeCombo.SelectedValue is PaymentType selectedPaymentType)
                return selectedPaymentType;

            var customer = GetSelectedCustomerFromCombo();

            if (customer != null)
                return customer.PaymentType;

            return PaymentType.Cash;
        }

        private CurrencyType GetSelectedCurrency()
        {
            if (CurrencyCombo.SelectedValue is CurrencyType selectedCurrency)
                return selectedCurrency;

            var customer = GetSelectedCustomerFromCombo();

            if (customer != null)
                return customer.Currency;

            return CurrencyType.AZN;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadLookupsAsync();

            if (_currentInvoice != null)
                await ReloadCurrentInvoiceAsync();
        }

        private async void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var customerId = GetSelectedCustomerId();

                if (customerId <= 0)
                {
                    ShowWarning("Zəhmət olmasa müştəri seçin.");
                    return;
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(x => x.Id == customerId && x.IsActive);

                if (customer == null)
                {
                    ShowWarning("Müştəri tapılmadı.");
                    return;
                }

                var paidAmount = ReadDecimal(PaidAmountText.Text, "Ödənilən məbləğ");

                var result = await _invoiceService.CreateDraftAsync(
                    type: InvoiceType.StockOut,
                    supplierId: null,
                    customerId: customerId,
                    paidAmount: paidAmount,
                    note: NoteText.Text?.Trim());

                if (!result.IsSuccess || result.Data == null)
                {
                    ShowWarning(result.Message);
                    return;
                }

                _currentInvoice = result.Data;
                _rows.Clear();

                ApplySelectedCustomerDefaults();

                InvoiceNumberText.Text = _currentInvoice.InvoiceNumber;
                InvoiceStatusText.Text = "Draft çıxış qaiməsi yaradıldı. İndi məhsul əlavə edin.";

                ConfirmButton.IsEnabled = true;
                CancelButton.IsEnabled = true;

                RefreshGrid();

                SetMessage(
                    $"Çıxış qaiməsi yaradıldı. Default ödəniş: {GetPaymentTypeText(GetSelectedPaymentType())}, valyuta: {GetCurrencyText(GetSelectedCurrency())}.",
                    false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShelfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            // YENI:
            // Köhnə BindProductsBySelectedShelf yerinə category filterli metod.
            BindProductsBySelectedShelfAndCategory();
        }

        private void BindProductsBySelectedShelfAndCategory()
        {
            ProductCombo.ItemsSource = null;
            ProductCombo.SelectedIndex = -1;
            PriceText.Text = "0";
            SelectedProductInfoText.Text = "Məhsul seçilməyib.";

            if (ShelfCombo.SelectedItem is not Shelf shelf)
            {
                _availableProductsInShelf.Clear();
                SelectedProductInfoText.Text = "Əvvəl rəf seçin.";
                return;
            }

            var categoryId = GetSelectedCategoryId();

            if (categoryId <= 0)
            {
                _availableProductsInShelf.Clear();
                SelectedProductInfoText.Text = "Əvvəl kateqoriya və ya alt kateqoriya seçin.";
                return;
            }

            _availableProductsInShelf = shelf.ShelfStocks?
                .Where(x =>
                    x.IsActive &&
                    x.Quantity > 0 &&
                    x.Product != null &&
                    x.Product.IsActive &&
                    x.Product.CategoryId == categoryId)
                .OrderBy(x => x.Product!.Name)
                .Select(x => new StockOutProductComboRow
                {
                    ProductId = x.ProductId,
                    ShelfId = x.ShelfId,
                    ProductName = x.Product?.Name ?? "",
                    ProductCode = x.Product?.Code ?? "",
                    AttributesText = x.Product == null ? "" : GetProductAttributesText(x.Product),
                    Quantity = x.Quantity,
                    SalePrice = x.Product?.SalePrice ?? 0,
                    DisplayName = BuildProductDisplayName(x)
                })
                .ToList() ?? new List<StockOutProductComboRow>();

            ProductCombo.ItemsSource = _availableProductsInShelf;

            if (!_availableProductsInShelf.Any())
                SelectedProductInfoText.Text = "Seçilmiş kateqoriya və rəfdə aktiv stok yoxdur.";
        }

        private string BuildProductDisplayName(ShelfStock stock)
        {
            var productName = stock.Product?.Name ?? "";
            var attributes = stock.Product == null ? "" : GetProductAttributesText(stock.Product);

            if (string.IsNullOrWhiteSpace(attributes))
                return $"{productName} | Qalıq: {stock.Quantity:0.##}";

            return $"{productName} / {attributes} | Qalıq: {stock.Quantity:0.##}";
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductCombo.SelectedItem is not StockOutProductComboRow productRow)
            {
                PriceText.Text = "0";
                SelectedProductInfoText.Text = "Məhsul seçilməyib.";
                return;
            }

            PriceText.Text = productRow.SalePrice.ToString("0.##", CultureInfo.InvariantCulture);

            SelectedProductInfoText.Text =
                $"Kod: {productRow.ProductCode} | Qalıq: {productRow.Quantity:0.##} | " +
                $"Xüsusiyyətlər: {(string.IsNullOrWhiteSpace(productRow.AttributesText) ? "Yoxdur" : productRow.AttributesText)}";
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null)
                {
                    ShowWarning("Əvvəlcə yeni çıxış qaiməsi yaradın.");
                    return;
                }

                if (GetSelectedCategoryId() <= 0)
                {
                    ShowWarning("Əvvəl kateqoriya və ya alt kateqoriya seçin.");
                    return;
                }

                if (ShelfCombo.SelectedValue == null)
                {
                    ShowWarning("Rəf seçin.");
                    return;
                }

                if (ProductCombo.SelectedItem is not StockOutProductComboRow selectedProduct)
                {
                    ShowWarning("Məhsul seçin.");
                    return;
                }

                var productId = selectedProduct.ProductId;
                var shelfId = Convert.ToInt32(ShelfCombo.SelectedValue);
                var quantity = ReadDecimal(QuantityText.Text, "Miqdar");
                var price = ReadDecimal(PriceText.Text, "Qiymət");

                if (quantity <= 0)
                {
                    ShowWarning("Miqdar 0-dan böyük olmalıdır.");
                    return;
                }

                if (price < 0)
                {
                    ShowWarning("Qiymət mənfi ola bilməz.");
                    return;
                }

                var alreadyInInvoice = _rows
                    .Where(x => x.ProductId == productId && x.ShelfId == shelfId)
                    .Sum(x => x.Quantity);

                if (alreadyInInvoice + quantity > selectedProduct.Quantity)
                {
                    ShowWarning(
                        $"Bu rəfdə maksimum {selectedProduct.Quantity:0.##} var. " +
                        $"Qaiməyə artıq {alreadyInInvoice:0.##} əlavə olunub.");
                    return;
                }

                var stockCheck = await _stockService.HasEnoughStockAsync(productId, shelfId, alreadyInInvoice + quantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                {
                    ShowWarning(stockCheck.Message);
                    return;
                }

                var result = await _invoiceService.AddItemAsync(
                    invoiceId: _currentInvoice.Id,
                    productId: productId,
                    shelfId: shelfId,
                    quantity: quantity,
                    price: price);

                if (!result.IsSuccess)
                {
                    ShowWarning(result.Message);
                    return;
                }

                await ReloadCurrentInvoiceAsync();

                QuantityText.Text = "1";
                PriceText.Text = selectedProduct.SalePrice.ToString("0.##", CultureInfo.InvariantCulture);

                SetMessage("Məhsul çıxış qaiməsinə əlavə edildi.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void RemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null)
                {
                    ShowWarning("Aktiv qaimə yoxdur.");
                    return;
                }

                if (ItemsGrid.SelectedItem is not StockOutItemRow selected)
                {
                    ShowWarning("Silmək üçün sətir seçin.");
                    return;
                }

                var confirm = MessageBox.Show(
                    "Seçilmiş məhsulu çıxış qaiməsindən silmək istəyirsiniz?",
                    "Təsdiq",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await _invoiceService.RemoveItemAsync(_currentInvoice.Id, selected.ItemId);

                if (!result.IsSuccess)
                {
                    ShowWarning(result.Message);
                    return;
                }

                await ReloadCurrentInvoiceAsync();
                SetMessage("Məhsul qaimədən silindi.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void ConfirmInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null)
                {
                    ShowWarning("Təsdiqlənəcək çıxış qaiməsi yoxdur.");
                    return;
                }

                var paymentUpdateResult = await ApplyPaymentBusinessRulesToCurrentInvoiceAsync();

                if (!paymentUpdateResult.IsSuccess)
                {
                    ShowWarning(paymentUpdateResult.Message);
                    return;
                }

                await ReloadCurrentInvoiceAsync();

                var confirm = MessageBox.Show(
                    "Çıxış qaiməsi təsdiqlənsin və mallar stokdan çıxarılsın?",
                    "Çıxış qaiməsi təsdiqi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await _invoiceService.ConfirmAsync(_currentInvoice.Id);

                if (!result.IsSuccess)
                {
                    ShowWarning(result.Message);
                    return;
                }

                InvoiceStatusText.Text = "Təsdiqləndi və stokdan çıxıldı.";
                ConfirmButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                SetMessage("Çıxış qaiməsi təsdiqləndi. Rəf stoku yeniləndi.", false);

                await LoadLookupsAsync();
                await ReloadCurrentInvoiceAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void CancelInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null)
                {
                    ShowWarning("Ləğv ediləcək çıxış qaiməsi yoxdur.");
                    return;
                }

                var confirm = MessageBox.Show(
                    "Draft çıxış qaiməsi ləğv edilsin?",
                    "Qaiməni ləğv et",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await _invoiceService.CancelAsync(_currentInvoice.Id);

                if (!result.IsSuccess)
                {
                    ShowWarning(result.Message);
                    return;
                }

                _currentInvoice = null;
                _rows.Clear();

                InvoiceNumberText.Text = "Hələ yaradılmayıb";
                InvoiceStatusText.Text = "Qaimə ləğv edildi.";
                ConfirmButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                RefreshGrid();
                SetMessage("Çıxış qaiməsi ləğv edildi.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async System.Threading.Tasks.Task ReloadCurrentInvoiceAsync()
        {
            if (_currentInvoice == null)
                return;

            var result = await _invoiceService.GetByIdAsync(_currentInvoice.Id);

            if (!result.IsSuccess || result.Data == null)
            {
                ShowWarning(result.Message);
                return;
            }

            _currentInvoice = result.Data;
            _rows.Clear();

            foreach (var item in _currentInvoice.Items.Where(x => x.IsActive))
            {
                _rows.Add(new StockOutItemRow
                {
                    ItemId = item.Id,
                    ProductId = item.ProductId,
                    ShelfId = item.ShelfId,
                    ProductName = item.Product?.Name ?? "",
                    AttributesText = item.Product == null ? "" : GetProductAttributesText(item.Product),
                    ShelfCode = item.Shelf?.Code ?? "",
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Total
                });
            }

            InvoiceNumberText.Text = _currentInvoice.InvoiceNumber;
            InvoiceStatusText.Text = $"Status: {_currentInvoice.Status}";

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            ItemsGrid.ItemsSource = null;
            ItemsGrid.ItemsSource = _rows.ToList();
            ItemsGrid.Items.Refresh();

            var total = _rows.Sum(x => x.Total);
            var paid = ReadDecimalSafe(PaidAmountText.Text);
            var paymentType = GetSelectedPaymentType();
            var currency = GetSelectedCurrency();

            if (_currentInvoice != null)
            {
                if (_currentInvoice.TotalAmount > 0)
                    total = _currentInvoice.TotalAmount;

                if (paymentType != PaymentType.Credit)
                {
                    paid = total;
                    PaidAmountText.Text = paid.ToString("0.##", CultureInfo.InvariantCulture);
                }
                else
                {
                    paid = ReadDecimalSafe(PaidAmountText.Text);

                    if (paid <= 0 && _currentInvoice.PaidAmount > 0)
                        paid = _currentInvoice.PaidAmount;
                }
            }

            if (paid > total)
                paid = total;

            var debt = total - paid;

            if (debt < 0)
                debt = 0;

            var currencyText = GetCurrencyText(currency);

            TotalAmountText.Text = $"{total:N2} {currencyText}";
            PaidSummaryText.Text = $"{paid:N2} {currencyText}";
            DebtAmountText.Text = $"{debt:N2} {currencyText}";
        }

        private async System.Threading.Tasks.Task<Anbar.Entities.Common.Result<bool>> ApplyPaymentBusinessRulesToCurrentInvoiceAsync()
        {
            if (_currentInvoice == null)
                return Anbar.Entities.Common.Result<bool>.Fail("Aktiv qaimə yoxdur.");

            var invoice = await _context.Invoices
                .Include(x => x.Items.Where(i => i.IsActive))
                .FirstOrDefaultAsync(x => x.Id == _currentInvoice.Id && x.IsActive);

            if (invoice == null)
                return Anbar.Entities.Common.Result<bool>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Anbar.Entities.Common.Result<bool>.Fail("Yalnız draft qaimənin ödənişi dəyişdirilə bilər.");

            var total = invoice.Items
                .Where(x => x.IsActive)
                .Sum(x => x.Total);

            invoice.TotalAmount = total;

            var paymentType = GetSelectedPaymentType();
            var paidFromUi = ReadDecimal(PaidAmountText.Text, "Ödənilən məbləğ");

            if (paymentType != PaymentType.Credit)
            {
                invoice.PaidAmount = total;
                invoice.DebtAmount = 0;
                PaidAmountText.Text = total.ToString("0.##", CultureInfo.InvariantCulture);
            }
            else
            {
                if (paidFromUi > total)
                    paidFromUi = total;

                invoice.PaidAmount = paidFromUi;
                invoice.DebtAmount = total - paidFromUi;

                if (invoice.DebtAmount < 0)
                    invoice.DebtAmount = 0;
            }

            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _currentInvoice = invoice;

            return Anbar.Entities.Common.Result<bool>.Success(true, "Ödəniş məlumatları qaiməyə yazıldı.");
        }

        private Customer? GetSelectedCustomerFromCombo()
        {
            if (CustomerCombo.SelectedItem is Customer selectedCustomer)
                return selectedCustomer;

            if (CustomerCombo.SelectedValue is int selectedCustomerId)
                return _customers.FirstOrDefault(x => x.Id == selectedCustomerId);

            if (CustomerCombo.SelectedValue != null &&
                int.TryParse(CustomerCombo.SelectedValue.ToString(), out var parsedId))
            {
                return _customers.FirstOrDefault(x => x.Id == parsedId);
            }

            return null;
        }

        private int GetSelectedCustomerId()
        {
            if (CustomerCombo.SelectedItem is Customer selectedCustomer)
                return selectedCustomer.Id;

            if (CustomerCombo.SelectedValue is int selectedInt)
                return selectedInt;

            if (CustomerCombo.SelectedValue != null &&
                int.TryParse(CustomerCombo.SelectedValue.ToString(), out var parsedId))
            {
                return parsedId;
            }

            return 0;
        }

        private string GetPaymentTypeText(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => "Nağd",
                PaymentType.BankTransfer => "Bank köçürməsi",
                PaymentType.Card => "Kart",
                PaymentType.Credit => "Kredit/Borc",
                _ => paymentType.ToString()
            };
        }

        private string GetCurrencyText(CurrencyType currency)
        {
            return currency switch
            {
                CurrencyType.AZN => "AZN",
                CurrencyType.USD => "USD",
                CurrencyType.EUR => "EUR",
                CurrencyType.TRY => "TRY",
                _ => currency.ToString()
            };
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

        private decimal ReadDecimal(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var normalized = value.Trim().Replace(",", ".");

            if (!decimal.TryParse(
                    normalized,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                throw new Exception($"{fieldName} düzgün formatda deyil.");
            }

            if (result < 0)
                throw new Exception($"{fieldName} mənfi ola bilməz.");

            return result;
        }

        private decimal ReadDecimalSafe(string? value)
        {
            try
            {
                return ReadDecimal(value, "Məbləğ");
            }
            catch
            {
                return 0;
            }
        }

        private void SetMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError
                ? System.Windows.Media.Brushes.Firebrick
                : System.Windows.Media.Brushes.SeaGreen;
        }

        private void ShowWarning(string message)
        {
            SetMessage(message, true);
            MessageBox.Show(message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowError(string message)
        {
            SetMessage(message, true);
            MessageBox.Show(message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class StockOutItemRow
    {
        public int ItemId { get; set; }
        public int ProductId { get; set; }
        public int ShelfId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string AttributesText { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    public class StockOutProductComboRow
    {
        public int ProductId { get; set; }
        public int ShelfId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string AttributesText { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class PaymentTypeComboRow
    {
        public PaymentType Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CurrencyComboRow
    {
        public CurrencyType Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}