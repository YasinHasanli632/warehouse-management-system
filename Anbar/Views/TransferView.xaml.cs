using Anbar.Data;
using Anbar.Entities;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Anbar.Views
{
    public partial class TransferView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ShelfService _shelfService;
        private readonly StockService _stockService;

        private bool _isBusy = false;
        private bool _isLoading = false;

        private const int ProductShelfPageSize = 5;
        private int _visibleProductShelfCount = ProductShelfPageSize;

        private List<Product> _products = new();
        private List<Shelf> _shelves = new();
        private List<ProductShelfMapCard> _allProductShelfCards = new();

        public TransferView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;")
                .Options;

            _context = new AppDbContext(options);
            _stockService = new StockService(_context);
            _shelfService = new ShelfService(_context, _stockService);

            Loaded += TransferView_Loaded;
        }

        private async void TransferView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private AppDbContext CreateReadContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;")
                .Options;

            return new AppDbContext(options);
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                _isLoading = true;

                var shelvesResult = await _shelfService.GetAllAsync();

                if (!shelvesResult.IsSuccess)
                {
                    ShowMessage(shelvesResult.Message, true);
                    return;
                }

                _shelves = shelvesResult.Data ?? new List<Shelf>();

                await using var readContext = CreateReadContext();

                var productIds = await readContext.ShelfStocks
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.Quantity > 0)
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToListAsync();

                _products = await readContext.Products
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Include(x => x.Attributes)
                        .ThenInclude(x => x.AttributeValue)
                            .ThenInclude(x => x.AttributeDefinition)
                    .Where(x => x.IsActive && productIds.Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                ProductCombo.ItemsSource = _products;
                ProductCombo.DisplayMemberPath = "Name";
                ProductCombo.SelectedValuePath = "Id";
                ProductCombo.SelectedIndex = -1;

                ProductCombo.ItemsSource = _products;
                ProductCombo.DisplayMemberPath = "Name";
                ProductCombo.SelectedValuePath = "Id";
                ProductCombo.SelectedIndex = -1;

                BindZoneCombos();
                ClearShelfFilters();
                ResetSummary();

                StockGrid.ItemsSource = new List<TransferStockRow>();
                _allProductShelfCards = new List<ProductShelfMapCard>();
                BindProductShelfMap();
                LoadStatistics();

                QuantityText.Text = "1";
                ShowMessage("Məhsul seçin.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Məlumatlar yüklənmədi: {ex.Message}", true);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _isBusy)
                return;

            try
            {
                _isBusy = true;
                _visibleProductShelfCount = ProductShelfPageSize;

                ClearShelfFilters();
                ResetSummary();

                if (ProductCombo.SelectedItem is Product product)
                {
                    ProductCombo.SelectedValue = product.Id;

                    var attributes = GetProductAttributesText(product);
                    SelectedProductText.Text = $"{product.Name} / {product.Code}";
                    SelectedProductSubText.Text = string.IsNullOrWhiteSpace(attributes) ? $"Kod: {product.Code}" : attributes;

                    await LoadProductShelfStocksAsync();
                    await LoadProductShelfMapAsync(product.Id);
                    BindFromZoneCombosForSelectedProduct();
                    BindToZoneCombosAllShelves();
                    ShowMessage("Məhsul seçildi. Kartdan 1-ci klik çıxış rəfi, 2-ci klik giriş rəfi seçir.", false);
                }
                else
                {
                    ProductCombo.SelectedIndex = -1;
                    StockGrid.ItemsSource = new List<TransferStockRow>();
                    _allProductShelfCards = new List<ProductShelfMapCard>();
                    BindProductShelfMap();
                    ShowMessage("Məhsul seçin.", false);
                }

                LoadStatistics();
                await UpdateSummaryAsync();
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void BindZoneCombos()
        {
            BindFromZoneCombosForSelectedProduct();
            BindToZoneCombosAllShelves();
        }

        private void BindFromZoneCombosForSelectedProduct()
        {
            if (ProductCombo.SelectedValue == null)
            {
                FromZoneCombo.ItemsSource = new List<string>();
                return;
            }

            var zones = _allProductShelfCards
                .Select(x => x.Zone)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            FromZoneCombo.ItemsSource = zones;
        }

        private void BindToZoneCombosAllShelves()
        {
            var zones = _shelves
                .Where(x => x.IsActive)
                .Select(x => x.Zone)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ToZoneCombo.ItemsSource = zones;
        }

        private void FromZoneCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            FromRowCombo.ItemsSource = null;
            FromShelfCombo.ItemsSource = null;
            FromRowCombo.SelectedIndex = -1;
            FromShelfCombo.SelectedIndex = -1;
            BindFromRowsByZone();
            SyncMirrorCombos();
        }

        private void FromRowCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            BindFromShelvesByZoneAndRow();
            SyncMirrorCombos();
        }

        private async void FromShelfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            BindToShelvesByZoneAndRow();
            SyncMirrorCombos();
            await UpdateSummaryAsync();
        }

        private void ToZoneCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            ToRowCombo.ItemsSource = null;
            ToShelfCombo.ItemsSource = null;
            ToRowCombo.SelectedIndex = -1;
            ToShelfCombo.SelectedIndex = -1;
            BindToRowsByZone();
            SyncMirrorCombos();
        }

        private void ToRowCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            BindToShelvesByZoneAndRow();
            SyncMirrorCombos();
        }

        private async void ToShelfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            SyncMirrorCombos();
            await UpdateSummaryAsync();
        }

        private void BindFromRowsByZone()
        {
            if (FromZoneCombo.SelectedItem == null)
                return;

            var zone = FromZoneCombo.SelectedItem.ToString();

            var rows = _allProductShelfCards
                .Where(x => x.Zone == zone)
                .Select(x => x.RowNumber)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            FromRowCombo.ItemsSource = rows;
        }

        private void BindFromShelvesByZoneAndRow()
        {
            FromShelfCombo.ItemsSource = null;
            FromShelfCombo.SelectedIndex = -1;

            if (FromZoneCombo.SelectedItem == null || FromRowCombo.SelectedItem == null)
                return;

            var zone = FromZoneCombo.SelectedItem.ToString();
            var row = Convert.ToInt32(FromRowCombo.SelectedItem);

            var shelfIds = _allProductShelfCards
                .Where(x => x.Zone == zone && x.RowNumber == row)
                .Select(x => x.ShelfId)
                .Distinct()
                .ToList();

            var shelves = _shelves
                .Where(x => x.IsActive && shelfIds.Contains(x.Id))
                .OrderBy(x => x.Code)
                .ToList();

            FromShelfCombo.ItemsSource = shelves;
            FromShelfCombo.DisplayMemberPath = "Code";
            FromShelfCombo.SelectedValuePath = "Id";
        }

        private void BindToRowsByZone()
        {
            if (ToZoneCombo.SelectedItem == null)
                return;

            var zone = ToZoneCombo.SelectedItem.ToString();

            var rows = _shelves
                .Where(x => x.IsActive && x.Zone == zone)
                .Select(x => x.RowNumber)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ToRowCombo.ItemsSource = rows;
        }

        private void BindToShelvesByZoneAndRow()
        {
            ToShelfCombo.ItemsSource = null;
            ToShelfCombo.SelectedIndex = -1;

            if (ToZoneCombo.SelectedItem == null || ToRowCombo.SelectedItem == null)
                return;

            var zone = ToZoneCombo.SelectedItem.ToString();
            var row = Convert.ToInt32(ToRowCombo.SelectedItem);
            var fromShelfId = FromShelfCombo.SelectedValue == null ? 0 : Convert.ToInt32(FromShelfCombo.SelectedValue);

            var shelves = _shelves
                .Where(x => x.IsActive && x.Zone == zone && x.RowNumber == row && x.Id != fromShelfId)
                .OrderBy(x => x.Code)
                .ToList();

            ToShelfCombo.ItemsSource = shelves;
            ToShelfCombo.DisplayMemberPath = "Code";
            ToShelfCombo.SelectedValuePath = "Id";
        }

        private async System.Threading.Tasks.Task LoadProductShelfStocksAsync()
        {
            try
            {
                if (ProductCombo.SelectedValue == null)
                {
                    StockGrid.ItemsSource = new List<TransferStockRow>();
                    return;
                }

                var productId = Convert.ToInt32(ProductCombo.SelectedValue);

                await using var readContext = CreateReadContext();

                var data = await readContext.ShelfStocks
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.Shelf)
                    .Where(x => x.IsActive && x.ProductId == productId && x.Quantity > 0)
                    .OrderBy(x => x.Shelf.Zone)
                    .ThenBy(x => x.Shelf.RowNumber)
                    .ThenBy(x => x.Shelf.Code)
                    .ToListAsync();

                var rows = data.Select(x =>
                {
                    var percent = NormalizePercent(x.Shelf.OccupancyPercent);
                    return new TransferStockRow
                    {
                        ShelfId = x.ShelfId,
                        ShelfCode = x.Shelf.Code,
                        Zone = x.Shelf.Zone,
                        RowNumber = x.Shelf.RowNumber,
                        Quantity = x.Quantity,
                        QuantityText = $"{x.Quantity:N2}",
                        Unit = x.Product.Unit,
                        OccupancyPercent = percent,
                        OccupancyText = $"{percent:N0}%",
                        OccupancyBarWidth = CalculateOccupancyBarWidth(percent),
                        OccupancyBrush = GetOccupancyBrush(percent),
                        LastMovementDateText = x.LastMovementDate.HasValue ? x.LastMovementDate.Value.ToString("dd.MM.yyyy HH:mm") : "-"
                    };
                }).ToList();

                StockGrid.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                ShowMessage($"Qalıqlar yüklənmədi: {ex.Message}", true);
            }
        }

        private async System.Threading.Tasks.Task LoadProductShelfMapAsync(int productId)
        {
            await using var readContext = CreateReadContext();

            var stocks = await readContext.ShelfStocks
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Shelf)
                .Where(x => x.IsActive && x.ProductId == productId && x.Quantity > 0)
                .OrderBy(x => x.Shelf.Zone)
                .ThenBy(x => x.Shelf.RowNumber)
                .ThenBy(x => x.Shelf.Code)
                .ToListAsync();

            _allProductShelfCards = stocks.Select(x =>
            {
                var percent = NormalizePercent(x.Shelf.OccupancyPercent);
                return new ProductShelfMapCard
                {
                    ShelfId = x.ShelfId,
                    ShelfCode = x.Shelf.Code,
                    Zone = x.Shelf.Zone,
                    RowNumber = x.Shelf.RowNumber,
                    ProductQuantity = x.Quantity,
                    ProductQuantityText = $"{x.Quantity:N0} {x.Product.Unit}",
                    OccupancyPercent = percent,
                    OccupancyText = $"{percent:N0}%",
                    CardBackground = GetStatusBackground(percent),
                    CardBorder = GetStatusBorder(percent),
                    CardForeground = GetStatusForeground(percent)
                };
            }).ToList();

            TotalProductStockText.Text = $"{_allProductShelfCards.Sum(x => x.ProductQuantity):N2}";
            BindProductShelfMap();
        }

        private void BindProductShelfMap()
        {
            ProductMapEmptyText.Visibility = ProductCombo.SelectedValue == null || !_allProductShelfCards.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (ProductCombo.SelectedValue == null || !_allProductShelfCards.Any())
            {
                ProductShelfMapItems.ItemsSource = new List<ProductShelfMapZoneRow>();
                ShowMoreProductShelvesButton.Visibility = Visibility.Collapsed;
                return;
            }

            var visibleCards = _allProductShelfCards
                .Take(_visibleProductShelfCount)
                .ToList();

            var rows = visibleCards
                .GroupBy(x => x.Zone)
                .Select(x => new ProductShelfMapZoneRow
                {
                    Zone = x.Key,
                    Shelves = x.ToList()
                })
                .ToList();

            ProductShelfMapItems.ItemsSource = rows;

            var remaining = _allProductShelfCards.Count - visibleCards.Count;
            ShowMoreProductShelvesButton.Visibility = remaining > 0 ? Visibility.Visible : Visibility.Collapsed;
            ShowMoreProductShelvesButton.Content = remaining > 0
                ? $"+  {Math.Min(ProductShelfPageSize, remaining)} rəf daha göstər"
                : "+  5 rəf daha göstər";
        }

        private void ShowMoreProductShelves_Click(object sender, RoutedEventArgs e)
        {
            _visibleProductShelfCount += ProductShelfPageSize;
            BindProductShelfMap();
        }

        private async void ProductShelfCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag == null)
                return;

            var shelfId = Convert.ToInt32(button.Tag);
            var shelf = _shelves.FirstOrDefault(x => x.Id == shelfId && x.IsActive);

            if (shelf == null)
                return;

            if (ProductCombo.SelectedValue == null)
            {
                ShowMessage("Əvvəl məhsul seçin.", true);
                return;
            }

            if (FromShelfCombo.SelectedValue == null)
            {
                await SelectFromShelfAsync(shelf);
                return;
            }

            await SelectToShelfAsync(shelf);
        }

        private async System.Threading.Tasks.Task SelectFromShelfAsync(Shelf shelf)
        {
            FromZoneCombo.SelectedItem = shelf.Zone;
            BindFromRowsByZone();
            FromRowCombo.SelectedItem = shelf.RowNumber;
            BindFromShelvesByZoneAndRow();
            FromShelfCombo.SelectedValue = shelf.Id;
            SyncMirrorCombos();
            ShowMessage($"{shelf.Code} çıxış rəfi kimi seçildi. İndi giriş rəfini seçin.", false);
            await UpdateSummaryAsync();
        }

        private async System.Threading.Tasks.Task SelectToShelfAsync(Shelf shelf)
        {
            if (FromShelfCombo.SelectedValue != null && Convert.ToInt32(FromShelfCombo.SelectedValue) == shelf.Id)
            {
                ShowMessage("Eyni rəf həm çıxış, həm giriş ola bilməz.", true);
                return;
            }

            ToZoneCombo.SelectedItem = shelf.Zone;
            BindToRowsByZone();
            ToRowCombo.SelectedItem = shelf.RowNumber;
            BindToShelvesByZoneAndRow();
            ToShelfCombo.SelectedValue = shelf.Id;
            SyncMirrorCombos();
            ShowMessage($"{shelf.Code} giriş rəfi kimi seçildi.", false);
            await UpdateSummaryAsync();
        }

        private async System.Threading.Tasks.Task UpdateSummaryAsync()
        {
            try
            {
                FromStockText.Text = "0";
                FromSmallStockText.Text = "0";
                FromShelfSubText.Text = "";
                FromOccupancySmallText.Text = "-";
                AvailableQtyText.Text = "Mövcud miqdar: 0";

                ToShelfStatusText.Text = "-";
                ToSmallOccupancyText.Text = "-";
                ToShelfSubText.Text = "";
                ToCurrentQtyText.Text = "0";

                await using var readContext = CreateReadContext();

                if (ProductCombo.SelectedValue != null && FromShelfCombo.SelectedValue != null)
                {
                    var productId = Convert.ToInt32(ProductCombo.SelectedValue);
                    var fromShelfId = Convert.ToInt32(FromShelfCombo.SelectedValue);

                    var fromStock = await readContext.ShelfStocks
                        .AsNoTracking()
                        .Include(x => x.Product)
                        .Include(x => x.Shelf)
                        .FirstOrDefaultAsync(x => x.IsActive && x.ProductId == productId && x.ShelfId == fromShelfId);

                    if (fromStock != null)
                    {
                        var percent = NormalizePercent(fromStock.Shelf.OccupancyPercent);
                        var unit = fromStock.Product.Unit ?? "";

                        FromStockText.Text = $"{fromStock.Quantity:N2} {unit}";
                        FromSmallStockText.Text = $"{fromStock.Quantity:N2} {unit}";
                        FromShelfSubText.Text = fromStock.Shelf.Code;
                        FromOccupancySmallText.Text = $"{percent:N0}%";
                        AvailableQtyText.Text = $"Mövcud miqdar: {fromStock.Quantity:N2} {unit}";
                    }
                }

                if (ToShelfCombo.SelectedValue != null)
                {
                    var toShelfId = Convert.ToInt32(ToShelfCombo.SelectedValue);
                    var productId = ProductCombo.SelectedValue == null ? 0 : Convert.ToInt32(ProductCombo.SelectedValue);

                    var toShelf = await readContext.Shelves
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == toShelfId && x.IsActive);

                    var toQty = productId == 0
                        ? 0
                        : await readContext.ShelfStocks
                            .AsNoTracking()
                            .Where(x => x.IsActive && x.ProductId == productId && x.ShelfId == toShelfId)
                            .SumAsync(x => (decimal?)x.Quantity) ?? 0;

                    if (toShelf != null)
                    {
                        var percent = NormalizePercent(toShelf.OccupancyPercent);
                        ToShelfStatusText.Text = $"{percent:N0}%";
                        ToSmallOccupancyText.Text = $"{percent:N0}%";
                        ToShelfSubText.Text = $"{toShelf.Code} / {GetShelfStatusText(toShelf.Status.ToString(), percent)}";
                        ToCurrentQtyText.Text = $"{toQty:N2}";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Xülasə yüklənmədi: {ex.Message}", true);
            }
        }

        private async void Transfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProductCombo.SelectedValue == null)
                {
                    ShowMessage("Məhsul seçin.", true);
                    return;
                }

                if (FromShelfCombo.SelectedValue == null)
                {
                    ShowMessage("Çıxış rəfini seçin.", true);
                    return;
                }

                if (ToShelfCombo.SelectedValue == null)
                {
                    ShowMessage("Giriş rəfini seçin.", true);
                    return;
                }

                var productId = Convert.ToInt32(ProductCombo.SelectedValue);
                var fromShelfId = Convert.ToInt32(FromShelfCombo.SelectedValue);
                var toShelfId = Convert.ToInt32(ToShelfCombo.SelectedValue);
                var quantity = ReadDecimal(QuantityText.Text);

                if (fromShelfId == toShelfId)
                {
                    ShowMessage("Eyni rəfə transfer etmək olmaz.", true);
                    return;
                }

                var stockCheck = await _stockService.HasEnoughStockAsync(productId, fromShelfId, quantity);

                if (!stockCheck.IsSuccess || !stockCheck.Data)
                {
                    ShowMessage(stockCheck.Message, true);
                    MessageBox.Show(stockCheck.Message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fromShelfCode = (FromShelfCombo.SelectedItem as Shelf)?.Code ?? "-";
                var toShelfCode = (ToShelfCombo.SelectedItem as Shelf)?.Code ?? "-";

                var confirm = MessageBox.Show(
                    $"{fromShelfCode} rəfindən {toShelfCode} rəfinə {quantity:N2} miqdar transfer edilsin?",
                    "Transfer təsdiqi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var note = string.IsNullOrWhiteSpace(NoteText.Text)
                    ? $"UI üzərindən transfer edildi. {fromShelfCode} → {toShelfCode}"
                    : NoteText.Text.Trim();

                var result = await _stockService.TransferShelfAsync(productId, fromShelfId, toShelfId, quantity, note);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    MessageBox.Show(result.Message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowMessage("Transfer uğurla tamamlandı.", false);

                var selectedProductId = productId;
                await LoadDataAsync();
                ProductCombo.SelectedValue = selectedProductId;
                QuantityText.Text = "1";
                NoteText.Text = "";
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
                MessageBox.Show(ex.Message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            ProductCombo.SelectedItem = null;
            ProductCombo.SelectedItem = null;
            ClearShelfFilters();
            ResetSummary();
            QuantityText.Text = "1";
            NoteText.Text = "";
            StockGrid.ItemsSource = new List<TransferStockRow>();
            _allProductShelfCards = new List<ProductShelfMapCard>();
            BindProductShelfMap();
            LoadStatistics();
            await System.Threading.Tasks.Task.CompletedTask;
            ShowMessage("Forma təmizləndi.", false);
        }

        private void ClearShelfFilters()
        {
            FromZoneCombo.SelectedItem = null;
            FromRowCombo.ItemsSource = null;
            FromShelfCombo.ItemsSource = null;

            ToZoneCombo.SelectedItem = null;
            ToRowCombo.ItemsSource = null;
            ToShelfCombo.ItemsSource = null;

            SyncMirrorCombos();
        }

        private void SyncMirrorCombos()
        {
            FromZoneMirror.ItemsSource = FromZoneCombo.ItemsSource;
            FromZoneMirror.SelectedItem = FromZoneCombo.SelectedItem;
            FromRowMirror.ItemsSource = FromRowCombo.ItemsSource;
            FromRowMirror.SelectedItem = FromRowCombo.SelectedItem;
            FromShelfMirror.ItemsSource = FromShelfCombo.ItemsSource;
            FromShelfMirror.DisplayMemberPath = "Code";
            FromShelfMirror.SelectedValuePath = "Id";
            FromShelfMirror.SelectedValue = FromShelfCombo.SelectedValue;

            ToZoneMirror.ItemsSource = ToZoneCombo.ItemsSource;
            ToZoneMirror.SelectedItem = ToZoneCombo.SelectedItem;
            ToRowMirror.ItemsSource = ToRowCombo.ItemsSource;
            ToRowMirror.SelectedItem = ToRowCombo.SelectedItem;
            ToShelfMirror.ItemsSource = ToShelfCombo.ItemsSource;
            ToShelfMirror.DisplayMemberPath = "Code";
            ToShelfMirror.SelectedValuePath = "Id";
            ToShelfMirror.SelectedValue = ToShelfCombo.SelectedValue;
        }

        private void ResetSummary()
        {
            SelectedProductText.Text = "-";
            SelectedProductSubText.Text = "";
            FromStockText.Text = "0";
            FromSmallStockText.Text = "0";
            FromShelfSubText.Text = "";
            FromOccupancySmallText.Text = "-";
            ToShelfStatusText.Text = "-";
            ToSmallOccupancyText.Text = "-";
            ToShelfSubText.Text = "";
            ToCurrentQtyText.Text = "0";
            AvailableQtyText.Text = "Mövcud miqdar: 0";
            TotalProductStockText.Text = "0";
        }

        private void LoadStatistics()
        {
            var active = _shelves.Where(x => x.IsActive).ToList();
            TotalShelvesText.Text = active.Count.ToString();
            LowShelvesText.Text = active.Count(x => NormalizePercent(x.OccupancyPercent) <= 30).ToString();
            MediumShelvesText.Text = active.Count(x => NormalizePercent(x.OccupancyPercent) > 30 && NormalizePercent(x.OccupancyPercent) <= 70).ToString();
            HighShelvesText.Text = active.Count(x => NormalizePercent(x.OccupancyPercent) > 70).ToString();
        }

        private decimal ReadDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("Miqdar boş ola bilməz.");

            var normalized = value.Trim().Replace(",", ".");

            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                throw new Exception("Miqdar düzgün formatda deyil.");

            if (result <= 0)
                throw new Exception("Miqdar 0-dan böyük olmalıdır.");

            return result;
        }

        private string GetProductAttributesText(Product product)
        {
            if (product.Attributes == null || !product.Attributes.Any())
                return "";

            var values = product.Attributes
                .Where(x => x.IsActive && x.AttributeValue != null && x.AttributeValue.IsActive && x.AttributeValue.AttributeDefinition != null && x.AttributeValue.AttributeDefinition.IsActive)
                .OrderBy(x => x.AttributeValue.AttributeDefinition.Name)
                .Select(x => $"{x.AttributeValue.AttributeDefinition.Name}: {x.AttributeValue.Value}")
                .ToList();

            return string.Join(" / ", values);
        }

        private decimal NormalizePercent(decimal percent)
        {
            return percent <= 1 ? percent * 100 : percent;
        }

        private double CalculateOccupancyBarWidth(decimal percent)
        {
            percent = NormalizePercent(percent);
            if (percent <= 0) return 0;
            if (percent >= 100) return 72;
            return Convert.ToDouble(percent) * 0.72;
        }

        private Brush GetOccupancyBrush(decimal percent)
        {
            percent = NormalizePercent(percent);
            if (percent <= 30) return new SolidColorBrush(Color.FromRgb(22, 163, 74));
            if (percent <= 70) return new SolidColorBrush(Color.FromRgb(217, 119, 6));
            return new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }

        private Brush GetStatusBackground(decimal percent)
        {
            percent = NormalizePercent(percent);
            if (percent <= 30) return new SolidColorBrush(Color.FromRgb(236, 253, 245));
            if (percent <= 70) return new SolidColorBrush(Color.FromRgb(255, 251, 235));
            return new SolidColorBrush(Color.FromRgb(254, 242, 242));
        }

        private Brush GetStatusBorder(decimal percent)
        {
            percent = NormalizePercent(percent);
            if (percent <= 30) return new SolidColorBrush(Color.FromRgb(187, 247, 208));
            if (percent <= 70) return new SolidColorBrush(Color.FromRgb(253, 230, 138));
            return new SolidColorBrush(Color.FromRgb(254, 202, 202));
        }

        private Brush GetStatusForeground(decimal percent)
        {
            percent = NormalizePercent(percent);
            if (percent <= 30) return new SolidColorBrush(Color.FromRgb(21, 128, 61));
            if (percent <= 70) return new SolidColorBrush(Color.FromRgb(180, 83, 9));
            return new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }

        private string GetShelfStatusText(string status, decimal percent)
        {
            percent = NormalizePercent(percent);
            if (percent <= 30) return "Boş";
            if (percent <= 70) return "Orta";
            return "Doludur";
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError ? Brushes.Firebrick : Brushes.SeaGreen;
        }
    }

    public class TransferStockRow
    {
        public int ShelfId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public decimal Quantity { get; set; }
        public string QuantityText { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OccupancyPercent { get; set; }
        public string OccupancyText { get; set; } = string.Empty;
        public double OccupancyBarWidth { get; set; }
        public Brush OccupancyBrush { get; set; } = Brushes.SeaGreen;
        public string LastMovementDateText { get; set; } = string.Empty;
    }

    public class ProductShelfMapZoneRow
    {
        public string Zone { get; set; } = string.Empty;
        public List<ProductShelfMapCard> Shelves { get; set; } = new();
    }

    public class ProductShelfMapCard
    {
        public int ShelfId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public decimal ProductQuantity { get; set; }
        public string ProductQuantityText { get; set; } = string.Empty;
        public decimal OccupancyPercent { get; set; }
        public string OccupancyText { get; set; } = string.Empty;
        public Brush CardBackground { get; set; } = Brushes.White;
        public Brush CardBorder { get; set; } = Brushes.LightGray;
        public Brush CardForeground { get; set; } = Brushes.Black;
    }
}
