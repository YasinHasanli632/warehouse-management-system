using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Enum;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Anbar.Views
{
    public partial class WarehouseMapView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly ShelfService _shelfService;

        private Dictionary<string, List<Shelf>> _allZoneGroups = new();
        private Shelf? _selectedShelf;

        public WarehouseMapView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _stockService = new StockService(_context);
            _shelfService = new ShelfService(_context, _stockService);

            Loaded += WarehouseMapView_Loaded;
        }

        private async void WarehouseMapView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMapAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadMapAsync();
        }

        private async System.Threading.Tasks.Task LoadMapAsync()
        {
            ZonesPanel.Children.Clear();

            await _shelfService.RecalculateAllShelvesAsync();

            var result = await _shelfService.GetShelvesGroupedByZoneAsync();

            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message, "Xəta", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _allZoneGroups = result.Data;

            RenderMap(_allZoneGroups);
            RefreshTopStats();
            ClearDetailPanel();
        }

        private void RenderMap(Dictionary<string, List<Shelf>> zoneGroups)
        {
            ZonesPanel.Children.Clear();

            var allShelves = zoneGroups?
                .SelectMany(x => x.Value)
                .ToList() ?? new List<Shelf>();

            if (!allShelves.Any())
            {
                ZonesPanel.Children.Add(new TextBlock
                {
                    Text = "Rəf tapılmadı.",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 15,
                    Margin = new Thickness(0, 20, 0, 0)
                });

                return;
            }

            foreach (var zoneGroup in zoneGroups.OrderBy(x => x.Key))
            {
                ZonesPanel.Children.Add(CreateZoneBlock(zoneGroup.Key, zoneGroup.Value));
            }
        }

        private Border CreateZoneBlock(string zoneName, List<Shelf> shelves)
        {
            var zoneBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 18),
                Margin = new Thickness(0, 0, 0, 22)
            };

            var stack = new StackPanel();

            var zoneTitle = new TextBlock
            {
                Text = $"ZONA {zoneName}",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush(Color.FromRgb(11, 100, 216)),
                Margin = new Thickness(0, 0, 0, 14)
            };

            stack.Children.Add(zoneTitle);

            var rowGroups = shelves
                .GroupBy(x => x.RowNumber)
                .OrderBy(x => x.Key)
                .ToList();

            foreach (var rowGroup in rowGroups)
            {
                stack.Children.Add(CreateRowLine(rowGroup.Key, rowGroup.ToList()));
            }

            zoneBorder.Child = stack;
            return zoneBorder;
        }

        private Grid CreateRowLine(int rowNumber, List<Shelf> shelves)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var rowTitle = new TextBlock
            {
                Text = $"SIRA {GetRowLabel(rowNumber)}",
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush(Color.FromRgb(11, 100, 216)),
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(rowTitle);

            var wrap = new WrapPanel
            {
                Margin = new Thickness(8, 0, 0, 0)
            };

            foreach (var shelf in shelves.OrderBy(x => x.Code))
            {
                wrap.Children.Add(CreateShelfButton(shelf));
            }

            Grid.SetColumn(wrap, 1);
            grid.Children.Add(wrap);

            return grid;
        }

        private Button CreateShelfButton(Shelf shelf)
        {
            var activeStocks = shelf.ShelfStocks?
                .Where(x => x.IsActive && x.Quantity > 0)
                .ToList() ?? new List<ShelfStock>();

            var totalQuantity = activeStocks.Sum(x => x.Quantity);
            var mainProduct = activeStocks
                .OrderByDescending(x => x.Quantity)
                .FirstOrDefault();

            var button = new Button
            {
                Width = 92,
                Height = 110,
                Margin = new Thickness(0, 0, 12, 10),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = shelf
            };

            var card = new Border
            {
                Background = GetShelfBackgroundByPercent(shelf.OccupancyPercent),
                BorderBrush = GetShelfBorderByPercent(shelf.OccupancyPercent),
                BorderThickness = new Thickness(_selectedShelf?.Id == shelf.Id ? 2.2 : 1.2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7)
            };

            if (_selectedShelf?.Id == shelf.Id)
            {
                card.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(37, 99, 235),
                    BlurRadius = 14,
                    ShadowDepth = 0,
                    Opacity = 0.45
                };
            }

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = shelf.Code,
                FontSize = 15,
                FontWeight = FontWeights.Black,
                Foreground = GetShelfTextByPercent(shelf.OccupancyPercent),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = mainProduct?.Product?.Name ?? "Boş",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Height = 34,
                Margin = new Thickness(0, 7, 0, 0)
            });

            stack.Children.Add(new TextBlock
            {
                Text = totalQuantity > 0
                    ? $"{totalQuantity:0.##} {mainProduct?.Product?.Unit ?? ""}"
                    : "",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            });

            stack.Children.Add(new TextBlock
            {
                Text = totalQuantity > 0 ? $"{shelf.OccupancyPercent:0.##}%" : "",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = GetShelfTextByPercent(shelf.OccupancyPercent),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 7, 0, 0)
            });

            card.Child = stack;
            button.Content = card;
            button.Click += ShelfButton_Click;

            return button;
        }

        private void ShelfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Shelf shelf)
                return;

            SelectShelf(shelf);

            // Seçilmiş rəfi mavi border ilə göstərmək üçün xəritəni yenidən çəkirik.
            RenderMap(_allZoneGroups);
        }

        private void SelectShelf(Shelf shelf)
        {
            _selectedShelf = shelf;

            var rows = shelf.ShelfStocks
                .Where(x => x.IsActive && x.Quantity > 0)
                .Select(x => new ShelfStockRow
                {
                    ProductName = x.Product?.Name ?? "",
                    Quantity = x.Quantity,
                    Unit = x.Product?.Unit ?? "",
                    QuantityWithUnit = $"{x.Quantity:0.##} {x.Product?.Unit ?? ""}"
                })
                .ToList();

            var totalQuantity = rows.Sum(x => x.Quantity);
            var mainRow = rows.OrderByDescending(x => x.Quantity).FirstOrDefault();

            SelectedShelfTitleText.Text = $"SEÇİLMİŞ RƏF: {shelf.Code}";
            ShelfCodeText.Text = shelf.Code;
            ShelfMainProductText.Text = mainRow?.ProductName ?? "Boş";
            ShelfMainQuantityText.Text = totalQuantity > 0 ? $"{totalQuantity:0.##} {mainRow?.Unit}" : "-";

            OccupancyText.Text = $"{shelf.OccupancyPercent:0.##}%";
            ShelfOccupancyBar.Value = (double)shelf.OccupancyPercent;
            ShelfOccupancyBar.Foreground = GetShelfStrongBrushByPercent(shelf.OccupancyPercent);

            ShelfStockGrid.ItemsSource = rows;
        }

        private void RefreshTopStats()
        {
            var allShelves = _allZoneGroups
                .SelectMany(x => x.Value)
                .ToList();

            var allStocks = allShelves
                .SelectMany(x => x.ShelfStocks ?? new List<ShelfStock>())
                .Where(x => x.IsActive && x.Quantity > 0)
                .ToList();

            TotalShelfCountText.Text = allShelves.Count.ToString();

            TotalProductCountText.Text = allStocks
                .Where(x => x.ProductId > 0)
                .Select(x => x.ProductId)
                .Distinct()
                .Count()
                .ToString();

            var avg = allShelves.Any()
                ? allShelves.Average(x => x.OccupancyPercent)
                : 0;

            AverageOccupancyText.Text = $"{avg:0.##}%";
        }

        private void ClearDetailPanel()
        {
            _selectedShelf = null;

            SelectedShelfTitleText.Text = "SEÇİLMİŞ RƏF: -";
            ShelfCodeText.Text = "-";
            ShelfMainProductText.Text = "-";
            ShelfMainQuantityText.Text = "-";
            OccupancyText.Text = "0%";
            ShelfOccupancyBar.Value = 0;
            ShelfStockGrid.ItemsSource = null;
        }

        private string GetRowLabel(int rowNumber)
        {
            if (rowNumber <= 0)
                return rowNumber.ToString();

            var index = rowNumber - 1;
            var result = "";

            do
            {
                result = (char)('A' + index % 26) + result;
                index = index / 26 - 1;
            }
            while (index >= 0);

            return result;
        }

        private Brush GetShelfBackgroundByPercent(decimal percent)
        {
            if (percent <= 0)
                return new LinearGradientBrush(
                    Color.FromRgb(248, 250, 252),
                    Color.FromRgb(229, 231, 235),
                    90);

            if (percent <= 30)
                return new SolidColorBrush(Color.FromRgb(220, 252, 231));

            if (percent <= 70)
                return new SolidColorBrush(Color.FromRgb(254, 249, 195));

            return new SolidColorBrush(Color.FromRgb(254, 226, 226));
        }

        private Brush GetShelfBorderByPercent(decimal percent)
        {
            if (percent <= 0)
                return new SolidColorBrush(Color.FromRgb(156, 163, 175));

            if (percent <= 30)
                return new SolidColorBrush(Color.FromRgb(34, 197, 94));

            if (percent <= 70)
                return new SolidColorBrush(Color.FromRgb(245, 158, 11));

            return new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }

        private Brush GetShelfTextByPercent(decimal percent)
        {
            if (percent <= 0)
                return new SolidColorBrush(Color.FromRgb(55, 65, 81));

            if (percent <= 30)
                return new SolidColorBrush(Color.FromRgb(21, 128, 61));

            if (percent <= 70)
                return new SolidColorBrush(Color.FromRgb(146, 64, 14));

            return new SolidColorBrush(Color.FromRgb(153, 27, 27));
        }

        private Brush GetShelfStrongBrushByPercent(decimal percent)
        {
            if (percent <= 0)
                return new SolidColorBrush(Color.FromRgb(156, 163, 175));

            if (percent <= 30)
                return new SolidColorBrush(Color.FromRgb(34, 197, 94));

            if (percent <= 70)
                return new SolidColorBrush(Color.FromRgb(245, 158, 11));

            return new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }

        public class ShelfStockRow
        {
            public string ProductName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = string.Empty;
            public string QuantityWithUnit { get; set; } = string.Empty;
        }
    }
}