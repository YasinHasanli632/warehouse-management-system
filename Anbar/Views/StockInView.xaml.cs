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
using System.Windows.Media;
using Microsoft.VisualBasic;

namespace Anbar.Views
{
    public partial class StockInView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;
        private readonly ShelfService _shelfService;
        private readonly SupplierService _supplierService;
        private readonly StockService _stockService;
        private readonly InvoiceService _invoiceService;
        private readonly ExpenseTypeService _expenseTypeService; // YENI

        private Invoice? _currentInvoice;
        private readonly List<StockInItemRow> _rows = new();
        private readonly List<StockInExpenseRow> _expenseRows = new(); // YENI
        private readonly List<StockInExpenseDetailFieldRow> _expenseDetailFields = new(); // YENI

        private List<Category> _categories = new();
        private List<Product> _allProducts = new();
        private List<Shelf> _allShelves = new();
        private List<StockInProductComboRow> _productComboRows = new();
        private List<ExpenseType> _expenseTypes = new(); // YENI

        private Product? _selectedProduct;
        private Shelf? _selectedShelf;
        private StockInExpenseRow? _selectedExpenseRow; // YENI

        private bool _isLoadingLookups = false;

        public StockInView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);

            _productService = new ProductService(_context);
            _shelfService = new ShelfService(_context);
            _supplierService = new SupplierService(_context);
            _stockService = new StockService(_context);
            _invoiceService = new InvoiceService(_context, _stockService);
            _expenseTypeService = new ExpenseTypeService(_context); // YENI

            // YENI:
            // XAML-də xərc button-larının hamısına ayrıca Click yazmadan mərkəzdən idarə edirik.
            AddHandler(Button.ClickEvent, new RoutedEventHandler(ExpenseButton_Click));
            ExpenseTypeCombo.SelectionChanged += ExpenseTypeCombo_SelectionChanged;
            ExpensesGrid.SelectionChanged += ExpensesGrid_SelectionChanged;

            Loaded += StockInView_Loaded;
        }

        private async void StockInView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLookupsAsync();
            RefreshGrid();
            UpdateShelfLimitInfo();
        }

        private async System.Threading.Tasks.Task LoadLookupsAsync()
        {
            try
            {
                _isLoadingLookups = true;

                var suppliersResult = await _supplierService.GetAllAsync();
                if (suppliersResult.IsSuccess)
                    SupplierCombo.ItemsSource = suppliersResult.Data;

                var productsResult = await _productService.GetAllAsync();
                if (productsResult.IsSuccess && productsResult.Data != null)
                {
                    _allProducts = productsResult.Data
                        .Where(x => x.Status == ProductStatus.Active)
                        .OrderBy(x => x.Category?.Name)
                        .ThenBy(x => x.Name)
                        .ToList();
                }

                var shelvesResult = await _shelfService.GetAllAsync();
                if (shelvesResult.IsSuccess && shelvesResult.Data != null)
                {
                    _allShelves = shelvesResult.Data
                        .OrderBy(x => x.Code)
                        .ToList();

                    ShelfCombo.ItemsSource = _allShelves;
                }

                await LoadCategoriesAsync();
                await LoadExpenseTypesAsync(); // YENI

                ProductCombo.ItemsSource = null;
                SelectedProductAttributesText.Text = "Əvvəl kateqoriya, sonra məhsul seçin.";

                _selectedProduct = null;
                _selectedShelf = null;

                UpdateShelfLimitInfo();

                MessageText.Text = "Məlumatlar yükləndi. Yeni giriş qaiməsi yarada bilərsiniz.";
            }
            catch (Exception ex)
            {
                ShowError($"Məlumatlar yüklənmədi: {ex.Message}");
            }
            finally
            {
                _isLoadingLookups = false;
            }
        }

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            _categories = await _context.Categories
                .Where(x => x.IsActive)
                .OrderBy(x => x.ParentCategoryId)
                .ThenBy(x => x.Name)
                .ToListAsync();

            MainCategoryCombo.ItemsSource = _categories
                .Where(x => x.ParentCategoryId == null)
                .OrderBy(x => x.Name)
                .ToList();

            MainCategoryCombo.SelectedIndex = -1;

            SubCategoryCombo.ItemsSource = null;
            SubCategoryCombo.SelectedIndex = -1;
            SubCategoryCombo.IsEnabled = false;
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
                if (SupplierCombo.SelectedValue == null)
                {
                    ShowWarning("Zəhmət olmasa təchizatçı seçin.");
                    return;
                }

                var supplierId = Convert.ToInt32(SupplierCombo.SelectedValue);
                var paidAmount = ReadDecimal(PaidAmountText.Text, "Ödənilən məbləğ");

                var result = await _invoiceService.CreateDraftAsync(
                    type: InvoiceType.StockIn,
                    supplierId: supplierId,
                    customerId: null,
                    paidAmount: paidAmount,
                    note: NoteText.Text?.Trim());

                if (!result.IsSuccess || result.Data == null)
                {
                    ShowWarning(result.Message);
                    return;
                }

                _currentInvoice = result.Data;
                _rows.Clear();
                _expenseRows.Clear(); // YENI
                RefreshExpenseGrid(); // YENI

                // YENI:
                // Xərclər grid-i üçün row-lar hazırlanır.
                _expenseRows.Clear();

                var expenseIndex = 1;
                foreach (var expense in _currentInvoice.Expenses.Where(x => x.IsActive).OrderBy(x => x.Id))
                {
                    _expenseRows.Add(ToExpenseRow(expense, expenseIndex));
                    expenseIndex++;
                }

                InvoiceNumberText.Text = _currentInvoice.InvoiceNumber;
                InvoiceStatusText.Text = "Draft qaimə yaradıldı. İndi məhsul əlavə edin.";

                ConfirmButton.IsEnabled = true;
                CancelButton.IsEnabled = true;

                RefreshGrid();
                SetMessage("Qaimə yaradıldı.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null)
                {
                    ShowWarning("Əvvəlcə yeni qaimə yaradın.");
                    return;
                }

                if (ProductCombo.SelectedValue == null)
                {
                    ShowWarning("Məhsul seçin.");
                    return;
                }

                if (ShelfCombo.SelectedValue == null)
                {
                    ShowWarning("Rəf seçin.");
                    return;
                }

                var productId = Convert.ToInt32(ProductCombo.SelectedValue);
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

                var selectedProduct = _allProducts.FirstOrDefault(x => x.Id == productId);

                if (selectedProduct == null)
                {
                    ShowWarning("Seçilmiş məhsul tapılmadı.");
                    return;
                }

                var selectedCategoryId = GetSelectedCategoryId();

                if (selectedCategoryId > 0 && selectedProduct.CategoryId != selectedCategoryId)
                {
                    ShowWarning("Seçilmiş məhsul seçilmiş kateqoriyaya aid deyil.");
                    return;
                }

                var capacityCheck = await _stockService.HasEnoughShelfCapacityAsync(
                    productId,
                    shelfId,
                    quantity);

                if (!capacityCheck.IsSuccess || !capacityCheck.Data)
                {
                    ShowWarning(capacityCheck.Message);
                    await ReloadShelvesOnlyAsync();
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
                await ReloadShelvesOnlyAsync();

                ShelfCombo.SelectedValue = shelfId;
                ProductCombo.SelectedValue = productId;

                QuantityText.Text = "1";
                PriceText.Text = selectedProduct.PurchasePrice.ToString("0.##", CultureInfo.InvariantCulture);

                UpdateShelfLimitInfo();

                SetMessage("Məhsul giriş qaiməsinə əlavə edildi.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async System.Threading.Tasks.Task ReloadShelvesOnlyAsync()
        {
            var shelvesResult = await _shelfService.GetAllAsync();

            if (shelvesResult.IsSuccess && shelvesResult.Data != null)
            {
                var selectedShelfId = ShelfCombo.SelectedValue == null
                    ? 0
                    : Convert.ToInt32(ShelfCombo.SelectedValue);

                _allShelves = shelvesResult.Data
                    .OrderBy(x => x.Code)
                    .ToList();

                ShelfCombo.ItemsSource = null;
                ShelfCombo.ItemsSource = _allShelves;

                if (selectedShelfId > 0)
                    ShelfCombo.SelectedValue = selectedShelfId;

                _selectedShelf = _allShelves.FirstOrDefault(x => x.Id == selectedShelfId);
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

                if (ItemsGrid.SelectedItem is not StockInItemRow selected)
                {
                    ShowWarning("Silmək üçün sətir seçin.");
                    return;
                }

                var confirm = MessageBox.Show(
                    "Seçilmiş məhsulu qaimədən silmək istəyirsiniz?",
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
                await ReloadShelvesOnlyAsync();

                UpdateShelfLimitInfo();

                SetMessage("Məhsul qaimədən silindi.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        // YENI:
        // Təsdiqlənmiş giriş qaiməsində yaranmış FIFO partiyanın adını/qeydini dəyişir.
        // Burada ayrıca yeni property yaratmırıq, StockBatch.Note sahəsini istifadə edirik.
        private async void EditBatchName_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button button)
                    return;

                if (button.DataContext is not StockInItemRow row)
                {
                    ShowWarning("Partiya sətri tapılmadı.");
                    return;
                }

                if (!row.BatchId.HasValue || row.BatchId.Value <= 0)
                {
                    ShowWarning("Bu sətir üçün partiya hələ yaranmayıb. Partiya qaimə təsdiqlənəndən sonra yaranır.");
                    return;
                }

                var currentName = row.BatchName == "-" ? string.Empty : row.BatchName;

                var newName = Interaction.InputBox(
                    "Partiya adını/qeydini yazın:",
                    "Partiya adını dəyiş",
                    currentName);

                if (string.IsNullOrWhiteSpace(newName))
                    return;

                var batch = await _context.StockBatches
                    .FirstOrDefaultAsync(x => x.Id == row.BatchId.Value);

                if (batch == null)
                {
                    ShowWarning("Partiya tapılmadı.");
                    return;
                }

                batch.Note = newName.Trim();
                batch.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await ReloadCurrentInvoiceAsync();

                SetMessage("Partiya adı yeniləndi.", false);
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
                    ShowWarning("Təsdiqlənəcək qaimə yoxdur.");
                    return;
                }

                var confirm = MessageBox.Show(
                    "Qaimə təsdiqlənsin və mallar rəflərə əlavə olunsun?",
                    "Qaimə təsdiqi",
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

                InvoiceStatusText.Text = "Təsdiqləndi və stoklara əlavə edildi.";
                ConfirmButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                SetMessage("Qaimə təsdiqləndi. Dashboard artıq real stok datalarını göstərəcək.", false);

                await ReloadCurrentInvoiceAsync();
                await ReloadShelvesOnlyAsync();

                UpdateShelfLimitInfo();
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
                    ShowWarning("Ləğv ediləcək qaimə yoxdur.");
                    return;
                }

                var confirm = MessageBox.Show(
                    "Draft qaimə ləğv edilsin?",
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
                _expenseRows.Clear(); // YENI
                RefreshExpenseGrid(); // YENI
                ClearExpenseDetailPanel(); // YENI

                InvoiceNumberText.Text = "Hələ yaradılmayıb";
                InvoiceStatusText.Text = "Qaimə ləğv edildi.";
                ConfirmButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                RefreshGrid();
                UpdateShelfLimitInfo();

                SetMessage("Qaimə ləğv edildi.", false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void MainCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            LoadSubCategoriesForSelectedMain();
            BindProductsBySelectedCategory();
            UpdateShelfLimitInfo();
        }

        private void SubCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLookups)
                return;

            BindProductsBySelectedCategory();
            UpdateShelfLimitInfo();
        }

        private void LoadSubCategoriesForSelectedMain()
        {
            if (MainCategoryCombo.SelectedValue == null)
            {
                SubCategoryCombo.ItemsSource = null;
                SubCategoryCombo.SelectedIndex = -1;
                SubCategoryCombo.IsEnabled = false;
                return;
            }

            var mainCategoryId = Convert.ToInt32(MainCategoryCombo.SelectedValue);

            var subCategories = _categories
                .Where(x => x.ParentCategoryId == mainCategoryId)
                .OrderBy(x => x.Name)
                .ToList();

            SubCategoryCombo.ItemsSource = subCategories;
            SubCategoryCombo.SelectedIndex = -1;
            SubCategoryCombo.IsEnabled = subCategories.Any();
        }

        private int GetSelectedCategoryId()
        {
            if (SubCategoryCombo.SelectedValue != null)
                return Convert.ToInt32(SubCategoryCombo.SelectedValue);

            if (MainCategoryCombo.SelectedValue != null)
                return Convert.ToInt32(MainCategoryCombo.SelectedValue);

            return 0;
        }

        private void BindProductsBySelectedCategory()
        {
            ProductCombo.ItemsSource = null;
            ProductCombo.SelectedIndex = -1;

            _selectedProduct = null;

            SelectedProductAttributesText.Text = "Məhsul seçilməyib.";
            PriceText.Text = "0";

            var categoryId = GetSelectedCategoryId();

            if (categoryId <= 0)
            {
                _productComboRows = new List<StockInProductComboRow>();
                ProductCombo.ItemsSource = _productComboRows;
                SelectedProductAttributesText.Text = "Əvvəl kateqoriya seçin.";
                return;
            }

            _productComboRows = _allProducts
                .Where(x => x.CategoryId == categoryId)
                .Select(ToProductComboRow)
                .OrderBy(x => x.DisplayName)
                .ToList();

            ProductCombo.ItemsSource = _productComboRows;

            if (!_productComboRows.Any())
                SelectedProductAttributesText.Text = "Bu kateqoriyada məhsul yoxdur.";
        }

        private StockInProductComboRow ToProductComboRow(Product product)
        {
            var attributesText = GetProductAttributesText(product);

            var displayName = string.IsNullOrWhiteSpace(attributesText)
                ? product.Name
                : $"{product.Name} / {attributesText}";

            return new StockInProductComboRow
            {
                Id = product.Id,
                Name = product.Name,
                DisplayName = displayName,
                CategoryId = product.CategoryId,
                PurchasePrice = product.PurchasePrice,
                AttributesText = attributesText
            };
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductCombo.SelectedItem is not StockInProductComboRow productRow)
            {
                _selectedProduct = null;
                SelectedProductAttributesText.Text = "Məhsul seçilməyib.";
                PriceText.Text = "0";
                UpdateShelfLimitInfo();
                return;
            }

            _selectedProduct = _allProducts.FirstOrDefault(x => x.Id == productRow.Id);

            PriceText.Text = productRow.PurchasePrice.ToString("0.##", CultureInfo.InvariantCulture);

            SelectedProductAttributesText.Text = string.IsNullOrWhiteSpace(productRow.AttributesText)
                ? "Bu məhsul üçün xüsusiyyət seçilməyib."
                : productRow.AttributesText;

            UpdateShelfLimitInfo();
        }

        private void ShelfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShelfCombo.SelectedItem is Shelf shelf)
                _selectedShelf = shelf;
            else
                _selectedShelf = null;

            UpdateShelfLimitInfo();
        }

        private void QuantityText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateShelfLimitInfo();
        }

        private async void UpdateShelfLimitInfo()
        {
            try
            {
                if (ShelfLimitInfoText == null || ShelfLimitStatusText == null)
                    return;

                if (_selectedShelf == null)
                {
                    ShelfLimitInfoText.Text = "Rəf seçilməyib.";
                    ShelfLimitStatusText.Text = "";
                    return;
                }

                var quantity = ReadDecimalSafe(QuantityText.Text);

                var totalQuantity = _selectedShelf.ShelfStocks?
                    .Where(x => x.IsActive)
                    .Sum(x => x.Quantity) ?? 0;

                var infoLines = new List<string>
        {
            $"Rəf: {_selectedShelf.Code}",
            $"Mövcud stok sayı: {totalQuantity:0.##}"
        };

                var hasProblem = false;
                var statusMessages = new List<string>();

                if (_selectedShelf.Capacity > 0)
                {
                    var afterQuantity = totalQuantity + quantity;
                    var availableQuantity = _selectedShelf.Capacity - totalQuantity;

                    if (availableQuantity < 0)
                        availableQuantity = 0;

                    infoLines.Add($"Say limiti: {totalQuantity:0.##} / {_selectedShelf.Capacity:0.##}");
                    infoLines.Add($"Boş say yeri: {availableQuantity:0.##}");

                    if (quantity > 0 && afterQuantity > _selectedShelf.Capacity)
                    {
                        hasProblem = true;
                        statusMessages.Add($"❌ Say limiti aşılır. Əlavə edilən: {quantity:0.##}");
                    }
                    else
                    {
                        statusMessages.Add("✅ Say limiti uyğundur.");
                    }
                }
                else
                {
                    infoLines.Add("Say limiti: təyin edilməyib.");
                }

                var activeLimitAttributes = _selectedShelf.AttributeValues?
                    .Where(x =>
                        x.IsActive &&
                        x.ShelfAttributeDefinition != null &&
                        x.ShelfAttributeDefinition.IsActive &&
                        x.ShelfAttributeDefinition.IsLimit &&
                        x.NumericValue.HasValue &&
                        x.NumericValue.Value > 0)
                    .ToList() ?? new List<ShelfAttributeValue>();

                if (activeLimitAttributes.Any())
                {
                    foreach (var limit in activeLimitAttributes)
                    {
                        var definition = limit.ShelfAttributeDefinition;

                        if (definition == null)
                            continue;

                        var unit = string.IsNullOrWhiteSpace(definition.Unit) ? "" : $" {definition.Unit}";

                        infoLines.Add($"{definition.Name}: {limit.NumericValue:0.##}{unit}");

                        if (_selectedProduct == null || quantity <= 0)
                            continue;

                        var key = definition.Key?.Trim() ?? "";

                        if (key.Equals("MaxWeightKg", StringComparison.OrdinalIgnoreCase))
                        {
                            var preview = BuildNumericLimitPreview(
                                _selectedShelf,
                                _selectedProduct,
                                quantity,
                                new[] { "çəki", "ceki", "weight", "kg", "kq" },
                                definition.Name,
                                limit.NumericValue.Value,
                                definition.Unit ?? "kg");

                            infoLines.Add(preview.InfoText);

                            if (!preview.IsValid)
                                hasProblem = true;

                            statusMessages.Add(preview.StatusText);
                        }
                        else if (key.Equals("MaxVolumeM3", StringComparison.OrdinalIgnoreCase))
                        {
                            var preview = BuildNumericLimitPreview(
                                _selectedShelf,
                                _selectedProduct,
                                quantity,
                                new[] { "həcm", "hecm", "volume", "m3", "m³" },
                                definition.Name,
                                limit.NumericValue.Value,
                                definition.Unit ?? "m³");

                            infoLines.Add(preview.InfoText);

                            if (!preview.IsValid)
                                hasProblem = true;

                            statusMessages.Add(preview.StatusText);
                        }
                    }
                }
                else
                {
                    infoLines.Add("Dinamik limit: təyin edilməyib.");
                }

                if (_selectedProduct != null && quantity > 0)
                {
                    var check = await _stockService.HasEnoughShelfCapacityAsync(
                        _selectedProduct.Id,
                        _selectedShelf.Id,
                        quantity);

                    if (!check.IsSuccess || !check.Data)
                    {
                        hasProblem = true;

                        if (!statusMessages.Any(x => x.Contains(check.Message)))
                            statusMessages.Add($"❌ {check.Message}");
                    }
                }

                ShelfLimitInfoText.Text = string.Join(Environment.NewLine, infoLines);

                if (!statusMessages.Any())
                {
                    ShelfLimitStatusText.Text = "Məhsul və miqdar seçildikdən sonra uyğunluq yoxlanacaq.";
                    ShelfLimitStatusText.Foreground = System.Windows.Media.Brushes.DarkSlateGray;
                    return;
                }

                ShelfLimitStatusText.Text = string.Join(Environment.NewLine, statusMessages.Distinct());

                ShelfLimitStatusText.Foreground = hasProblem
                    ? System.Windows.Media.Brushes.Firebrick
                    : System.Windows.Media.Brushes.SeaGreen;
            }
            catch (Exception ex)
            {
                if (ShelfLimitStatusText != null)
                {
                    ShelfLimitStatusText.Text = $"❌ Rəf limiti yoxlanarkən xəta: {ex.Message}";
                    ShelfLimitStatusText.Foreground = System.Windows.Media.Brushes.Firebrick;
                }
            }
        }

        private NumericLimitPreviewResult BuildNumericLimitPreview(
            Shelf shelf,
            Product incomingProduct,
            decimal incomingQuantity,
            string[] productAttributeNameKeywords,
            string shelfLimitName,
            decimal shelfLimitValue,
            string unit)
        {
            var incomingSingleValue = GetProductNumericAttributeValue(
                incomingProduct,
                productAttributeNameKeywords);

            if (!incomingSingleValue.HasValue)
            {
                return new NumericLimitPreviewResult
                {
                    IsValid = false,
                    InfoText = $"{shelfLimitName}: məhsul dəyəri tapılmadı.",
                    StatusText = $"❌ {incomingProduct.Name} məhsulunda {shelfLimitName.ToLower()} üçün uyğun dəyər yoxdur."
                };
            }

            var currentTotalValue = 0m;

            foreach (var stock in shelf.ShelfStocks?.Where(x => x.IsActive && x.Quantity > 0) ?? new List<ShelfStock>())
            {
                if (stock.Product == null)
                    continue;

                var productSingleValue = GetProductNumericAttributeValue(
                    stock.Product,
                    productAttributeNameKeywords);

                if (!productSingleValue.HasValue)
                {
                    return new NumericLimitPreviewResult
                    {
                        IsValid = false,
                        InfoText = $"{shelfLimitName}: rəfdəki məhsul dəyəri tapılmadı.",
                        StatusText = $"❌ Rəfdə olan {stock.Product.Name} məhsulunda {shelfLimitName.ToLower()} üçün uyğun dəyər yoxdur."
                    };
                }

                currentTotalValue += productSingleValue.Value * stock.Quantity;
            }

            var incomingTotalValue = incomingSingleValue.Value * incomingQuantity;
            var newTotalValue = currentTotalValue + incomingTotalValue;
            var availableValue = shelfLimitValue - currentTotalValue;

            if (availableValue < 0)
                availableValue = 0;

            var info =
                $"{shelfLimitName}: mövcud {currentTotalValue:0.##} {unit}, " +
                $"boş {availableValue:0.##} {unit}, " +
                $"əlavə ediləcək {incomingTotalValue:0.##} {unit}";

            if (newTotalValue > shelfLimitValue)
            {
                return new NumericLimitPreviewResult
                {
                    IsValid = false,
                    InfoText = info,
                    StatusText = $"❌ {shelfLimitName} limiti aşılır."
                };
            }

            return new NumericLimitPreviewResult
            {
                IsValid = true,
                InfoText = info,
                StatusText = $"✅ {shelfLimitName} limiti uyğundur."
            };
        }

        private decimal? GetProductNumericAttributeValue(
            Product product,
            string[] attributeNameKeywords)
        {
            if (product.Attributes == null || !product.Attributes.Any())
                return null;

            var attribute = product.Attributes
                .Where(x =>
                    x.IsActive &&
                    x.AttributeValue != null &&
                    x.AttributeValue.IsActive &&
                    x.AttributeValue.AttributeDefinition != null &&
                    x.AttributeValue.AttributeDefinition.IsActive)
                .FirstOrDefault(x =>
                {
                    var name = x.AttributeValue.AttributeDefinition.Name?.Trim().ToLower() ?? string.Empty;

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

            if (decimal.TryParse(
                    rawValue,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                return value;
            }

            return null;
        }

        private async System.Threading.Tasks.Task ReloadCurrentInvoiceAsync()
        {
            if (_currentInvoice == null)
                return;

            var invoice = await _context.Invoices
                .Include(x => x.Supplier)
                // YENI:
                // Qaiməyə bağlı əlavə xərcləri və detail-ləri yükləyirik.
                .Include(x => x.Expenses.Where(ex => ex.IsActive))
                    .ThenInclude(x => x.ExpenseType)
                .Include(x => x.Expenses.Where(ex => ex.IsActive))
                    .ThenInclude(x => x.FieldValues.Where(f => f.IsActive))
                .FirstOrDefaultAsync(x => x.Id == _currentInvoice.Id && x.IsActive);

            if (invoice == null)
            {
                ShowWarning("Qaimə tapılmadı.");
                return;
            }

            var items = await _context.InvoiceItems
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Attributes)
                        .ThenInclude(x => x.AttributeValue)
                            .ThenInclude(x => x.AttributeDefinition)
                .Include(x => x.Shelf)
                .Where(x => x.InvoiceId == invoice.Id && x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync();

            // YENI:
            // Bu giriş qaiməsi təsdiqlənəndən sonra hər qaimə sətri üçün yaranan FIFO partiyaları oxuyuruq.
            // Draft qaimədə hələ StockBatch olmur, ona görə grid-də "Təsdiqdən sonra yaranacaq" göstərilir.
            var batches = await _context.StockBatches
                .Where(x => x.SourceInvoiceId == invoice.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            _currentInvoice = invoice;
            _currentInvoice.Items = items;

            _rows.Clear();

            foreach (var item in items)
            {
                var batch = batches.FirstOrDefault(x => x.SourceInvoiceItemId == item.Id);

                _rows.Add(new StockInItemRow
                {
                    ItemId = item.Id,
                    ProductId = item.ProductId,
                    ShelfId = item.ShelfId,

                    // YENI:
                    // Confirm-dən sonra yaranan StockBatch Id-si. Draft vaxtı null qalır.
                    BatchId = batch?.Id,

                    ProductName = item.Product?.Name ?? "",
                    AttributesText = item.Product == null
                        ? ""
                        : GetProductAttributesText(item.Product),
                    ShelfCode = item.Shelf?.Code ?? "",

                    // YENI:
                    // Hər qaimə sətri üçün sistemin avtomatik yaratdığı partiya nömrəsi.
                    BatchNumber = batch == null
                        ? "Təsdiqdən sonra yaranacaq"
                        : batch.BatchNumber,

                    // YENI:
                    // Partiya adı/qeydi üçün StockBatch.Note istifadə olunur.
                    // İstifadəçi istəsə grid-də "Ad dəyiş" ilə bunu dəyişə bilər.
                    BatchName = batch == null
                        ? "-"
                        : string.IsNullOrWhiteSpace(batch.Note)
                            ? "-"
                            : batch.Note,

                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Total
                });
            }

            // YENI:
            // Xərclər grid-i üçün row-lar hazırlanır.
            _expenseRows.Clear();

            var expenseIndex = 1;
            foreach (var expense in _currentInvoice.Expenses.Where(x => x.IsActive).OrderBy(x => x.Id))
            {
                _expenseRows.Add(ToExpenseRow(expense, expenseIndex));
                expenseIndex++;
            }

            InvoiceNumberText.Text = _currentInvoice.InvoiceNumber;
            InvoiceStatusText.Text = $"Status: {_currentInvoice.Status}";

            RefreshGrid();
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

        private void RefreshGrid()
        {
            // YENI: DataGrid bəzən eyni List reference ilə UI-ni yeniləmir.
            // Ona görə yeni List reference veririk və məcburi Refresh edirik.
            ItemsGrid.ItemsSource = null;
            ItemsGrid.ItemsSource = _rows.ToList();
            ItemsGrid.Items.Refresh();

            var itemsTotal = _rows.Sum(x => x.Total);
            var expensePlusTotal = _expenseRows
                .Where(x => x.Direction == ExpenseDirection.Plus)
                .Sum(x => x.Amount);
            var expenseMinusTotal = _expenseRows
                .Where(x => x.Direction == ExpenseDirection.Minus)
                .Sum(x => x.Amount);

            var total = itemsTotal + expensePlusTotal - expenseMinusTotal;
            var paid = ReadDecimalSafe(PaidAmountText.Text);

            if (_currentInvoice != null)
            {
                paid = _currentInvoice.PaidAmount;

                if (_currentInvoice.TotalAmount > 0)
                {
                    itemsTotal = _currentInvoice.ItemsTotalAmount > 0 ? _currentInvoice.ItemsTotalAmount : itemsTotal;
                    expensePlusTotal = _currentInvoice.ExtraExpenseAmount;
                    expenseMinusTotal = _currentInvoice.DiscountAmount;
                    total = _currentInvoice.TotalAmount;
                }
            }

            var debt = total - paid;

            if (debt < 0)
                debt = 0;

            ExpensePlusTotalText.Text = $"+{expensePlusTotal:N2} AZN";
            ExpenseMinusTotalText.Text = $"-{expenseMinusTotal:N2} AZN";
            TotalAmountText.Text = $"{total:N2} AZN";
            PaidSummaryText.Text = $"{paid:N2} AZN";
            DebtAmountText.Text = $"{debt:N2} AZN";

            RefreshExpenseGrid();

            // YENI: Test üçün mesajda sətir sayını da göstəririk.
            // İstəsən sonra bu sətri silə bilərsən.
            if (_rows.Any())
                SetMessage($"Qaimədə {_rows.Count} məhsul var.", false);
        }


        // =========================
        // 💰 EXPENSE UI LOGIC
        // =========================
        // YENI:
        // Xərc növlərini combobox-a yükləyir.
        private async System.Threading.Tasks.Task LoadExpenseTypesAsync()
        {
            var result = await _expenseTypeService.GetAllAsync(InvoiceType.StockIn);

            if (!result.IsSuccess || result.Data == null)
            {
                SetMessage(result.Message, true);
                return;
            }

            _expenseTypes = result.Data;

            ExpenseTypeCombo.ItemsSource = null;
            ExpenseTypeCombo.DisplayMemberPath = "Name";
            ExpenseTypeCombo.SelectedValuePath = "Id";
            ExpenseTypeCombo.ItemsSource = _expenseTypes;

            if (_expenseTypes.Any())
                ExpenseTypeCombo.SelectedIndex = 0;
            else
                ClearExpenseDetailPanel();
        }

        // YENI:
        // Xərc növü dəyişəndə default tip və dinamik detail sahələri dəyişir.
        // Artıq yalnız Daşınma üçün hardcoded deyil; Fəhlə pulu və digər xərc növləri də işləyir.
        private void ExpenseTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExpenseTypeCombo.SelectedItem is not ExpenseType expenseType)
                return;

            ExpenseDirectionCombo.SelectedIndex = expenseType.DefaultDirection == ExpenseDirection.Minus ? 1 : 0;
            _selectedExpenseRow = null;
            BuildExpenseDetailFields(expenseType, null);
        }

        // YENI:
        // Xərclər gridində sətir seçiləndə detail panelə həmin xərcin sahələrini yükləyirik.
        private void ExpensesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExpensesGrid.SelectedItem is StockInExpenseRow row)
            {
                _selectedExpenseRow = row;
                LoadExpenseDetailToPanel(row);
            }
        }

        // YENI:
        // XAML-də xərc button-larının hamısını mərkəzdən idarə edirik.
        private async void ExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Button button)
                return;

            var content = button.Content?.ToString()?.Trim() ?? "";

            try
            {
                if (content == "Əlavə et")
                {
                    await AddExpenseFromUiAsync();
                    e.Handled = true;
                    return;
                }

                if (content == "+ Yeni xərc növü")
                {
                    await CreateExpenseTypeFromUiAsync();
                    e.Handled = true;
                    return;
                }

                if (content == "Detal" && button.DataContext is StockInExpenseRow detailRow)
                {
                    _selectedExpenseRow = detailRow;
                    ExpensesGrid.SelectedItem = detailRow;
                    LoadExpenseDetailToPanel(detailRow);
                    SetMessage("Xərc detalları panelə yükləndi.", false);
                    e.Handled = true;
                    return;
                }

                if (content == "Sil" && button.DataContext is StockInExpenseRow removeRow)
                {
                    await RemoveExpenseFromUiAsync(removeRow);
                    e.Handled = true;
                    return;
                }

                if (content == "Yadda saxla")
                {
                    await SaveSelectedExpenseDetailsAsync();
                    e.Handled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        // YENI:
        // UI-dan xərc əlavə edir.
        private async System.Threading.Tasks.Task AddExpenseFromUiAsync()
        {
            if (_currentInvoice == null)
            {
                ShowWarning("Əvvəlcə yeni qaimə yaradın.");
                return;
            }

            if (ExpenseTypeCombo.SelectedItem is not ExpenseType expenseType)
            {
                ShowWarning("Xərc növü seçin.");
                return;
            }

            var amount = ReadDecimal(ExpenseAmountText.Text, "Xərc məbləği");

            if (amount <= 0)
            {
                ShowWarning("Xərc məbləği 0-dan böyük olmalıdır.");
                return;
            }

            var direction = GetSelectedExpenseDirection();
            var fields = BuildExpenseFieldDictionary();

            var result = await _invoiceService.AddExpenseAsync(
                invoiceId: _currentInvoice.Id,
                expenseTypeId: expenseType.Id,
                customName: expenseType.Name,
                amount: amount,
                direction: direction,
                affectStockCost: expenseType.AffectStockCost,
                note: ExpenseNoteText.Text?.Trim(),
                fieldValues: fields);

            if (!result.IsSuccess)
            {
                ShowWarning(result.Message);
                return;
            }

            ExpenseAmountText.Text = "0";
            ExpenseNoteText.Text = "";
            ClearExpenseDetailPanel();

            await ReloadCurrentInvoiceAsync();
            SetMessage("Xərc qaiməyə əlavə edildi.", false);
        }

        // YENI:
        // Sadə input ilə yeni xərc növü yaradır.
        private async System.Threading.Tasks.Task CreateExpenseTypeFromUiAsync()
        {
            var name = Interaction.InputBox(
                "Yeni xərc növünün adını yazın:",
                "Yeni xərc növü",
                "Yeni xərc");

            if (string.IsNullOrWhiteSpace(name))
                return;

            var direction = GetSelectedExpenseDirection();

            var result = await _expenseTypeService.CreateAsync(
                name: name.Trim(),
                defaultDirection: direction,
                useForStockIn: true,
                useForStockOut: true,
                affectStockCost: direction == ExpenseDirection.Plus);

            if (!result.IsSuccess)
            {
                ShowWarning(result.Message);
                return;
            }

            await LoadExpenseTypesAsync();
            ExpenseTypeCombo.SelectedValue = result.Data!.Id;

            SetMessage("Yeni xərc növü yaradıldı.", false);
        }

        // YENI:
        // Seçilmiş xərc silinir.
        private async System.Threading.Tasks.Task RemoveExpenseFromUiAsync(StockInExpenseRow row)
        {
            if (_currentInvoice == null)
            {
                ShowWarning("Aktiv qaimə yoxdur.");
                return;
            }

            var confirm = MessageBox.Show(
                "Seçilmiş xərc qaimədən silinsin?",
                "Xərci sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _invoiceService.RemoveExpenseAsync(_currentInvoice.Id, row.ExpenseId);

            if (!result.IsSuccess)
            {
                ShowWarning(result.Message);
                return;
            }

            _selectedExpenseRow = null;
            ClearExpenseDetailPanel();
            await ReloadCurrentInvoiceAsync();
            SetMessage("Xərc silindi.", false);
        }

        // YENI:
        // Detail paneldəki dinamik məlumatları seçilmiş xərcə yazır.
        private async System.Threading.Tasks.Task SaveSelectedExpenseDetailsAsync()
        {
            if (_currentInvoice == null)
            {
                ShowWarning("Aktiv qaimə yoxdur.");
                return;
            }

            if (_selectedExpenseRow == null)
            {
                ShowWarning("Əvvəl xərclər siyahısından Detal seçin.");
                return;
            }

            var fields = BuildExpenseFieldDictionary();

            var result = await _invoiceService.UpdateExpenseFieldsAsync(
                _currentInvoice.Id,
                _selectedExpenseRow.ExpenseId,
                fields);

            if (!result.IsSuccess)
            {
                ShowWarning(result.Message);
                return;
            }

            await ReloadCurrentInvoiceAsync();

            var refreshedRow = _expenseRows.FirstOrDefault(x => x.ExpenseId == _selectedExpenseRow.ExpenseId);
            if (refreshedRow != null)
            {
                _selectedExpenseRow = refreshedRow;
                ExpensesGrid.SelectedItem = refreshedRow;
                LoadExpenseDetailToPanel(refreshedRow);
            }

            SetMessage("Xərc detalları yadda saxlanıldı.", false);
        }

        // YENI:
        // Detail paneldən key-value dictionary yaradır.
        // UI-da artıq ayrıca Key/Value yoxdur; sahələr ExpenseTypeFieldDefinition-dan dinamik gəlir.
        private Dictionary<string, string?> BuildExpenseFieldDictionary()
        {
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in _expenseDetailFields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldKey))
                    continue;

                fields[field.FieldKey] = field.Value?.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ExpenseDetailNoteText.Text))
                fields["ExpenseDetailNote"] = ExpenseDetailNoteText.Text.Trim();

            return fields;
        }

        // YENI:
        // ExpenseDirectionCombo-dan enum qaytarır.
        private ExpenseDirection GetSelectedExpenseDirection()
        {
            return ExpenseDirectionCombo.SelectedIndex == 1
                ? ExpenseDirection.Minus
                : ExpenseDirection.Plus;
        }

        // YENI:
        // Entity-ni grid row-a çevirir.
        private StockInExpenseRow ToExpenseRow(InvoiceExpense expense, int rowNumber)
        {
            return new StockInExpenseRow
            {
                RowNumber = rowNumber,
                ExpenseId = expense.Id,
                ExpenseTypeId = expense.ExpenseTypeId,
                ExpenseTypeName = expense.ExpenseType?.Name ?? expense.Name,
                Amount = expense.Amount,
                Direction = expense.Direction,
                DirectionText = expense.Direction == ExpenseDirection.Plus ? "+" : "-",
                Note = expense.Note ?? "",
                FieldValues = expense.FieldValues
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Label)
                    .ToDictionary(x => x.FieldKey, x => x.Value ?? "", StringComparer.OrdinalIgnoreCase)
            };
        }

        // YENI:
        // Xərclər gridini refresh edir.
        private void RefreshExpenseGrid()
        {
            if (ExpensesGrid == null)
                return;

            ExpensesGrid.ItemsSource = null;
            ExpensesGrid.ItemsSource = _expenseRows.ToList();
            ExpensesGrid.Items.Refresh();
        }

        // YENI:
        // Xərc növünə görə detail input-larını dinamik yaradır.
        private void BuildExpenseDetailFields(ExpenseType? expenseType, StockInExpenseRow? row)
        {
            _expenseDetailFields.Clear();

            var title = expenseType?.Name;

            if (string.IsNullOrWhiteSpace(title) && row != null)
                title = row.ExpenseTypeName;

            ExpenseDetailTitleText.Text = string.IsNullOrWhiteSpace(title)
                ? "Xərc detalı"
                : $"Xərc detalı ({title})";

            ExpenseDetailNoteText.Text = row != null
                ? GetFieldValue(row, "ExpenseDetailNote", "Qeyd")
                : string.Empty;

            var definitions = expenseType?.FieldDefinitions?
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Label)
                .ToList() ?? new List<ExpenseTypeFieldDefinition>();

            foreach (var definition in definitions)
            {
                _expenseDetailFields.Add(new StockInExpenseDetailFieldRow
                {
                    ExpenseTypeFieldDefinitionId = definition.Id,
                    FieldKey = definition.FieldKey,
                    Label = definition.Label,
                    Value = row == null ? string.Empty : GetFieldValue(row, definition.FieldKey, definition.Label),
                    SortOrder = definition.SortOrder
                });
            }

            // Əgər DB-də əvvəl saxlanmış, amma artıq definition siyahısında olmayan field varsa, itirməyək.
            if (row != null)
            {
                foreach (var pair in row.FieldValues)
                {
                    if (pair.Key.Equals("ExpenseDetailNote", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var exists = _expenseDetailFields.Any(x =>
                        x.FieldKey.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) ||
                        x.Label.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        _expenseDetailFields.Add(new StockInExpenseDetailFieldRow
                        {
                            FieldKey = pair.Key,
                            Label = pair.Key,
                            Value = pair.Value,
                            SortOrder = 999
                        });
                    }
                }
            }

            ExpenseDetailFieldsItems.ItemsSource = null;
            ExpenseDetailFieldsItems.ItemsSource = _expenseDetailFields;

            ExpenseDetailHintText.Text = _expenseDetailFields.Any()
                ? "Sahələri doldurub Yadda saxla edin."
                : "Bu xərc növündə default detail sahələri yoxdur.";
        }

        // YENI:
        // Seçilmiş xərcin detail məlumatlarını panelə yazır.
        private void LoadExpenseDetailToPanel(StockInExpenseRow row)
        {
            var expenseType = _expenseTypes.FirstOrDefault(x => x.Id == row.ExpenseTypeId);
            BuildExpenseDetailFields(expenseType, row);
        }

        // YENI:
        // Field dictionary-dən dəyər oxuyur.
        private string GetFieldValue(StockInExpenseRow row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (row.FieldValues.TryGetValue(key, out var value))
                    return value ?? "";
            }

            return "";
        }

        // YENI:
        // Xərc detail panelini təmizləyir.
        private void ClearExpenseDetailPanel()
        {
            _expenseDetailFields.Clear();

            ExpenseDetailTitleText.Text = "Xərc detalı";
            ExpenseDetailHintText.Text = "Xərclər siyahısından Detal seçin.";
            ExpenseDetailNoteText.Text = string.Empty;

            if (ExpenseDetailFieldsItems != null)
            {
                ExpenseDetailFieldsItems.ItemsSource = null;
                ExpenseDetailFieldsItems.ItemsSource = _expenseDetailFields;
            }
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

    public class StockInItemRow
    {
        public int ItemId { get; set; }

        public int ProductId { get; set; }

        public int ShelfId { get; set; }

        // YENI:
        // Qaimə təsdiqlənəndən sonra yaranan FIFO partiyanın Id-si.
        public int? BatchId { get; set; }

        // YENI:
        // Sistem tərəfindən avtomatik verilən partiya nömrəsi.
        public string BatchNumber { get; set; } = string.Empty;

        // YENI:
        // İstifadəçinin dəyişə biləcəyi partiya adı/qeydi.
        public string BatchName { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string AttributesText { get; set; } = string.Empty;

        public string ShelfCode { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Total { get; set; }
    }

    public class StockInProductComboRow
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public decimal PurchasePrice { get; set; }

        public string AttributesText { get; set; } = string.Empty;
    }

    public class StockInExpenseDetailFieldRow
    {
        public int? ExpenseTypeFieldDefinitionId { get; set; }

        public string FieldKey { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }

    public class StockInExpenseRow
    {
        public int RowNumber { get; set; }

        public int ExpenseId { get; set; }

        public int? ExpenseTypeId { get; set; }

        public string ExpenseTypeName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public ExpenseDirection Direction { get; set; }

        public string DirectionText { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public Dictionary<string, string> FieldValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class NumericLimitPreviewResult
    {
        public bool IsValid { get; set; }

        public string InfoText { get; set; } = string.Empty;

        public string StatusText { get; set; } = string.Empty;
    }
}