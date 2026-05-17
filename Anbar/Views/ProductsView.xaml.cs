using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Anbar.Views
{
    public partial class ProductsView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;
        private readonly UnitService _unitService;

        private List<Product> _allProducts = new();
        private List<ProductListRow> _filteredRows = new();

        private int _currentPage = 1;
        private int _pageSize = 10;
        private bool _isLoaded = false;

        public ProductsView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _productService = new ProductService(_context);
            _unitService = new UnitService(_context);

            Loaded += ProductsView_Loaded;
        }

        private async void ProductsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;

                await LoadFiltersAsync();
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Məhsullar səhifəsi açılmadı:\n{ex.Message}",
                    "Xəta",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadFiltersAsync()
        {
            var categories = await _context.Categories
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new FilterOption
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            categories.Insert(0, new FilterOption
            {
                Id = 0,
                Name = "Hamısı"
            });

            CategoryFilterCombo.ItemsSource = categories;
            CategoryFilterCombo.SelectedValue = 0;

            var unitResult = await _unitService.GetActiveAsync();

            var units = new List<FilterOption>
            {
                new FilterOption
                {
                    Id = 0,
                    Name = "Hamısı"
                }
            };

            if (unitResult.IsSuccess && unitResult.Data != null)
            {
                units.AddRange(unitResult.Data
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new FilterOption
                    {
                        Id = x.Id,
                        Name = string.IsNullOrWhiteSpace(x.Symbol)
                            ? x.Name
                            : $"{x.Name} ({x.Symbol})"
                    }));
            }

            UnitFilterCombo.ItemsSource = units;
            UnitFilterCombo.SelectedValue = 0;

            StatusFilterCombo.ItemsSource = new List<FilterOption>
            {
                new FilterOption { Id = 0, Name = "Hamısı" },
                new FilterOption { Id = 1, Name = "Aktiv" },
                new FilterOption { Id = 2, Name = "Passiv" }
            };

            StatusFilterCombo.SelectedValue = 0;
        }

        private async Task LoadProductsAsync()
        {
            var result = await _productService.GetAllAsync();

            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(
                    result.Message,
                    "Xəta",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _allProducts = result.Data;

            _currentPage = 1;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var query = _allProducts.AsQueryable();

            var keyword = SearchText.Text?.Trim().ToLower() ?? "";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    (p.Name ?? "").ToLower().Contains(keyword) ||
                    (p.Code ?? "").ToLower().Contains(keyword) ||
                    (p.Barcode ?? "").ToLower().Contains(keyword) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(keyword)) ||
                    (p.UnitEntity != null && p.UnitEntity.Name.ToLower().Contains(keyword)) ||
                    (p.UnitEntity != null && p.UnitEntity.Symbol.ToLower().Contains(keyword)) ||
                    (p.Unit ?? "").ToLower().Contains(keyword));
            }

            var categoryId = GetComboSelectedId(CategoryFilterCombo);

            if (categoryId > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            var unitId = GetComboSelectedId(UnitFilterCombo);

            if (unitId > 0)
                query = query.Where(p => p.UnitId == unitId);

            var statusId = GetComboSelectedId(StatusFilterCombo);

            if (statusId == 1)
                query = query.Where(p => p.IsActive && p.Status == ProductStatus.Active);

            if (statusId == 2)
                query = query.Where(p => !p.IsActive || p.Status != ProductStatus.Active);

            if (DateFromPicker.SelectedDate.HasValue)
            {
                var from = DateFromPicker.SelectedDate.Value.Date;
                query = query.Where(p => p.CreatedAt >= from);
            }

            if (DateToPicker.SelectedDate.HasValue)
            {
                var to = DateToPicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.CreatedAt <= to);
            }

            var products = query
                .OrderByDescending(p => p.Id)
                .ToList();

            _filteredRows = products
                .Select((p, index) => new ProductListRow
                {
                    RowNo = index + 1,
                    Id = p.Id,
                    Code = p.Code,
                    Barcode = string.IsNullOrWhiteSpace(p.Barcode) ? "-" : p.Barcode,
                    Name = p.Name,
                    CategoryName = p.Category?.Name ?? "-",
                    UnitName = p.UnitEntity != null
                        ? p.UnitEntity.Symbol
                        : string.IsNullOrWhiteSpace(p.Unit) ? "-" : p.Unit,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    MinStockQuantity = p.MinStockQuantity,
                    IsActive = p.IsActive && p.Status == ProductStatus.Active
                })
                .ToList();

            BindPage();
        }

        private void BindPage()
        {
            var total = _filteredRows.Count;

            if (_pageSize <= 0)
                _pageSize = 10;

            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (decimal)_pageSize));

            if (_currentPage < 1)
                _currentPage = 1;

            if (_currentPage > totalPages)
                _currentPage = totalPages;

            var rows = _filteredRows
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            ProductsGrid.ItemsSource = rows;

            TotalCountText.Text = $"Cəmi: {total} məhsul";

            PageOneButton.Content = _currentPage.ToString();
            PageTwoButton.Content = (_currentPage + 1 <= totalPages) ? (_currentPage + 1).ToString() : "";
            PageThreeButton.Content = (_currentPage + 2 <= totalPages) ? (_currentPage + 2).ToString() : "";
        }

        private int GetComboSelectedId(ComboBox comboBox)
        {
            if (comboBox.SelectedValue == null)
                return 0;

            if (int.TryParse(comboBox.SelectedValue.ToString(), out var id))
                return id;

            return 0;
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _currentPage = 1;
            ApplyFilters();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _currentPage = 1;
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _currentPage = 1;
            ApplyFilters();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchText.Text = "";

            CategoryFilterCombo.SelectedValue = 0;
            UnitFilterCombo.SelectedValue = 0;
            StatusFilterCombo.SelectedValue = 0;

            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;

            _currentPage = 1;
            ApplyFilters();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
        }

        private void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            if (PageSizeCombo.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content.ToString(), out var size))
            {
                _pageSize = size;
                _currentPage = 1;
                BindPage();
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage--;
            BindPage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            BindPage();
        }

        private async void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "Yeni Məhsul Əlavə Et",
                Content = new ProductAddView(),
                Width = 1300,
                Height = 850,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();

            await LoadProductsAsync();
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            var productId = GetButtonProductId(sender);

            if (productId <= 0)
                return;

            MessageBox.Show(
                $"Məhsul detal səhifəsi sonra qoşulacaq. ProductId: {productId}",
                "Məhsul detalları",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            var productId = GetButtonProductId(sender);

            if (productId <= 0)
                return;

            var window = new Window
            {
                Title = "Məhsulu Redaktə Et",
                Content = new ProductAddView(productId),
                Width = 1300,
                Height = 850,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();

            await LoadProductsAsync();
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            var productId = GetButtonProductId(sender);

            if (productId <= 0)
                return;

            var confirm = MessageBox.Show(
                "Bu məhsul passiv edilsin?",
                "Təsdiq",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _productService.DeactivateAsync(productId);

            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    result.Message,
                    "Xəta",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            await LoadProductsAsync();
        }

        private int GetButtonProductId(object sender)
        {
            if (sender is not Button button)
                return 0;

            if (button.Tag == null)
                return 0;

            if (int.TryParse(button.Tag.ToString(), out var id))
                return id;

            return 0;
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Excel ixrac funksiyasını sonra qoşacağıq.",
                "Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "PDF ixrac funksiyasını sonra qoşacağıq.",
                "PDF",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    public class ProductListRow
    {
        public int RowNo { get; set; }
        public int Id { get; set; }

        public string Code { get; set; } = "";
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinStockQuantity { get; set; }

        public bool IsActive { get; set; }

        public string PurchasePriceText => PurchasePrice.ToString("0.00", CultureInfo.InvariantCulture);
        public string SalePriceText => SalePrice.ToString("0.00", CultureInfo.InvariantCulture);
        public string MinStockText => MinStockQuantity.ToString("0.00", CultureInfo.InvariantCulture);
        public string StatusText => IsActive ? "Aktiv" : "Passiv";
    }

    public class FilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}