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
    public partial class ShelvesView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ShelfService _shelfService;
        private readonly StockService _stockService;

        private List<Shelf> _shelves = new();
        private List<ShelfListRow> _shelfRows = new();
        private List<ShelfAttributeDefinition> _shelfAttributeDefinitions = new();
        private List<ShelfAttributeInputRow> _selectedAttributeRows = new();

        private int? _selectedShelfId = null;
        private bool _isClearingForm = false;

        public ShelvesView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _stockService = new StockService(_context);
            _shelfService = new ShelfService(_context, _stockService);

            Loaded += ShelvesView_Loaded;
        }

        private async void ShelvesView_Loaded(object sender, RoutedEventArgs e)
        {
            await EnsureDefaultShelfAttributeDefinitionsAsync();
            await LoadWarehousesAsync();
            await LoadShelfAttributeDefinitionsAsync();
            await LoadShelvesAsync();
            ClearForm();
        }

        private async System.Threading.Tasks.Task EnsureDefaultShelfAttributeDefinitionsAsync()
        {
            var defaults = new[]
            {
                new { Name = "Maksimum çəki", Key = "MaxWeightKg", Unit = "kg", IsLimit = true, IsNumeric = true },
                new { Name = "Maksimum həcm", Key = "MaxVolumeM3", Unit = "m³", IsLimit = true, IsNumeric = true },
                new { Name = "Hündürlük", Key = "HeightCm", Unit = "cm", IsLimit = false, IsNumeric = true },
                new { Name = "En", Key = "WidthCm", Unit = "cm", IsLimit = false, IsNumeric = true },
                new { Name = "Dərinlik", Key = "DepthCm", Unit = "cm", IsLimit = false, IsNumeric = true },
                new { Name = "Rəf tipi", Key = "ShelfType", Unit = "", IsLimit = false, IsNumeric = false }
            };

            foreach (var item in defaults)
            {
                var exists = await _context.ShelfAttributeDefinitions
                    .AnyAsync(x => x.Key.ToLower() == item.Key.ToLower() && x.IsActive);

                if (exists)
                    continue;

                await _context.ShelfAttributeDefinitions.AddAsync(new ShelfAttributeDefinition
                {
                    Name = item.Name,
                    Key = item.Key,
                    Unit = item.Unit,
                    IsLimit = item.IsLimit,
                    IsNumeric = item.IsNumeric,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task LoadWarehousesAsync()
        {
            var warehouses = await _context.Warehouses
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            WarehouseCombo.ItemsSource = warehouses;
        }

        private async System.Threading.Tasks.Task LoadShelfAttributeDefinitionsAsync()
        {
            var result = await _shelfService.GetShelfAttributeDefinitionsAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            _shelfAttributeDefinitions = result.Data ?? new List<ShelfAttributeDefinition>();

            ShelfAttributeDefinitionCombo.ItemsSource = _shelfAttributeDefinitions
                .Select(x => new ShelfAttributeDefinitionComboRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    Key = x.Key,
                    Unit = x.Unit ?? "",
                    IsLimit = x.IsLimit,
                    IsNumeric = x.IsNumeric,
                    DisplayName = string.IsNullOrWhiteSpace(x.Unit)
                        ? $"{x.Name} ({x.Key})"
                        : $"{x.Name} ({x.Unit})"
                })
                .ToList();

            ShelfAttributeDefinitionCombo.SelectedIndex =
                ShelfAttributeDefinitionCombo.Items.Count > 0 ? 0 : -1;
        }

        private async System.Threading.Tasks.Task LoadShelvesAsync()
        {
            var recalc = await _shelfService.RecalculateAllShelvesAsync();

            if (!recalc.IsSuccess)
            {
                ShowMessage(recalc.Message, true);
                return;
            }

            var result = await _shelfService.GetAllAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            _shelves = result.Data ?? new List<Shelf>();

            _shelfRows = _shelves
                .Select(ToShelfListRow)
                .ToList();

            ApplyFilter();

            ShowMessage("Rəflər yükləndi.", false);
        }

        private ShelfListRow ToShelfListRow(Shelf shelf)
        {
            var activeStocks = shelf.ShelfStocks?
                .Where(x => x.IsActive)
                .ToList() ?? new List<ShelfStock>();

            var totalQuantity = activeStocks.Sum(x => x.Quantity);

            return new ShelfListRow
            {
                Id = shelf.Id,
                Code = shelf.Code,
                Zone = shelf.Zone,
                RowNumber = shelf.RowNumber,
                WarehouseName = shelf.Warehouse?.Name ?? "",
                Capacity = shelf.Capacity,
                TotalQuantity = totalQuantity,
                OccupancyPercent = shelf.OccupancyPercent,
                AttributeSummary = GetShelfAttributesText(shelf),
                StatusText = shelf.Status.ToString()
            };
        }

        private void ApplyFilter()
        {
            var keyword = SearchText.Text?.Trim().ToLower() ?? "";

            var filtered = _shelfRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(x =>
                    (x.Code ?? "").ToLower().Contains(keyword) ||
                    (x.Zone ?? "").ToLower().Contains(keyword) ||
                    (x.WarehouseName ?? "").ToLower().Contains(keyword) ||
                    (x.AttributeSummary ?? "").ToLower().Contains(keyword) ||
                    (x.StatusText ?? "").ToLower().Contains(keyword));
            }

            ShelvesGrid.ItemsSource = filtered
                .OrderBy(x => x.Zone)
                .ThenBy(x => x.RowNumber)
                .ToList();
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadWarehousesAsync();
            await LoadShelfAttributeDefinitionsAsync();
            await LoadShelvesAsync();
        }

        private void NewShelf_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void RecalculateSelectedShelf_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShelfId == null)
            {
                ShowMessage("Əvvəl rəf seçin.", true);
                return;
            }

            var result = await _shelfService.RecalculateShelfAsync(_selectedShelfId.Value);

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            await LoadShelvesAsync();
            ShowMessage("Seçilmiş rəfin statusu yenidən hesablandı.", false);
        }

        private void ShelvesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isClearingForm)
                return;

            if (ShelvesGrid.SelectedItem is not ShelfListRow row)
                return;

            var shelf = _shelves.FirstOrDefault(x => x.Id == row.Id);

            if (shelf == null)
                return;

            _selectedShelfId = shelf.Id;

            FormTitleText.Text = "Rəfi düzəlt";
            SaveButton.Content = "Dəyişiklikləri yadda saxla";
            DeactivateButton.IsEnabled = true;

            CodeText.Text = shelf.Code;
            ZoneText.Text = shelf.Zone;
            RowNumberText.Text = shelf.RowNumber.ToString();
            CapacityText.Text = shelf.Capacity.ToString("0.##", CultureInfo.InvariantCulture);
            WarehouseCombo.SelectedValue = shelf.WarehouseId;

            LoadShelfAttributeRowsFromShelf(shelf);
            BindShelfProducts(shelf);
            UpdateSelectedShelfSummary(shelf);

            ShowMessage("Rəf seçildi. Düzəliş edə bilərsiniz.", false);
        }

        private void LoadShelfAttributeRowsFromShelf(Shelf shelf)
        {
            _selectedAttributeRows = shelf.AttributeValues?
                .Where(x => x.IsActive && x.ShelfAttributeDefinition != null)
                .OrderBy(x => x.ShelfAttributeDefinition.Name)
                .Select(x => new ShelfAttributeInputRow
                {
                    ShelfAttributeDefinitionId = x.ShelfAttributeDefinitionId,
                    Name = x.ShelfAttributeDefinition.Name,
                    Key = x.ShelfAttributeDefinition.Key,
                    Unit = x.ShelfAttributeDefinition.Unit ?? "",
                    IsLimit = x.ShelfAttributeDefinition.IsLimit,
                    IsNumeric = x.ShelfAttributeDefinition.IsNumeric,
                    NumericValue = x.NumericValue,
                    TextValue = x.TextValue,
                    DisplayValue = BuildAttributeDisplayValue(
                        x.NumericValue,
                        x.TextValue,
                        x.ShelfAttributeDefinition.Unit),
                    IsLimitText = x.ShelfAttributeDefinition.IsLimit ? "Bəli" : "Xeyr"
                })
                .ToList() ?? new List<ShelfAttributeInputRow>();

            RefreshShelfAttributesGrid();
        }

        private void BindShelfProducts(Shelf shelf)
        {
            var rows = shelf.ShelfStocks?
                .Where(x => x.IsActive && x.Quantity > 0)
                .OrderBy(x => x.Product?.Name)
                .Select(x => new ShelfProductRow
                {
                    ProductCode = x.Product?.Code ?? "",
                    ProductName = x.Product?.Name ?? "",
                    CategoryName = x.Product?.Category?.Name ?? "",
                    AttributesText = x.Product == null ? "" : GetProductAttributesText(x.Product),
                    Quantity = x.Quantity
                })
                .ToList() ?? new List<ShelfProductRow>();

            ShelfProductsGrid.ItemsSource = rows;
        }

        private void UpdateSelectedShelfSummary(Shelf shelf)
        {
            var totalQuantity = shelf.ShelfStocks?
                .Where(x => x.IsActive)
                .Sum(x => x.Quantity) ?? 0;

            var attributes = GetShelfAttributesText(shelf);

            SelectedShelfSummaryText.Text =
                $"Kod: {shelf.Code} | Zona: {shelf.Zone} | Say limiti: {shelf.Capacity:0.##} | " +
                $"Mövcud stok: {totalQuantity:0.##} | Doluluq: {shelf.OccupancyPercent:0.##}% | Status: {shelf.Status}" +
                (string.IsNullOrWhiteSpace(attributes) ? "" : $" | Limitlər: {attributes}");
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WarehouseCombo.SelectedValue == null)
                {
                    ShowMessage("Anbar seçin.", true);
                    return;
                }

                var code = CodeText.Text.Trim();
                var zone = ZoneText.Text.Trim();

                if (!int.TryParse(RowNumberText.Text.Trim(), out var rowNumber))
                {
                    ShowMessage("Sıra nömrəsi düzgün deyil.", true);
                    return;
                }

                var capacity = ReadDecimalAllowZero(CapacityText.Text, "Say limiti");
                var warehouseId = Convert.ToInt32(WarehouseCombo.SelectedValue);
                var attributes = BuildShelfAttributeInputs();

                if (_selectedShelfId == null)
                {
                    var createResult = await _shelfService.CreateAsync(
                        code,
                        zone,
                        rowNumber,
                        capacity,
                        warehouseId,
                        attributes);

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Rəf yaradıldı.", false);
                }
                else
                {
                    var updateResult = await _shelfService.UpdateAsync(
                        _selectedShelfId.Value,
                        code,
                        zone,
                        rowNumber,
                        capacity,
                        attributes);

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Rəf yeniləndi.", false);
                }

                await LoadShelvesAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private List<ShelfAttributeInput> BuildShelfAttributeInputs()
        {
            return _selectedAttributeRows
                .Select(x => new ShelfAttributeInput
                {
                    ShelfAttributeDefinitionId = x.ShelfAttributeDefinitionId,
                    NumericValue = x.NumericValue,
                    TextValue = x.TextValue
                })
                .ToList();
        }

        private void AddShelfAttribute_Click(object sender, RoutedEventArgs e)
        {
            if (ShelfAttributeDefinitionCombo.SelectedItem is not ShelfAttributeDefinitionComboRow selectedDefinition)
            {
                ShowMessage("Əvvəl rəf xüsusiyyəti seçin.", true);
                return;
            }

            var alreadyExists = _selectedAttributeRows.Any(x =>
                x.ShelfAttributeDefinitionId == selectedDefinition.Id);

            if (alreadyExists)
            {
                ShowMessage("Bu xüsusiyyət artıq əlavə olunub.", true);
                return;
            }

            decimal? numericValue = null;
            string? textValue = null;

            if (selectedDefinition.IsNumeric)
            {
                numericValue = ReadDecimalAllowZero(AttributeNumericValueText.Text, selectedDefinition.Name);

                if (numericValue <= 0)
                {
                    ShowMessage($"{selectedDefinition.Name} 0-dan böyük olmalıdır.", true);
                    return;
                }
            }
            else
            {
                textValue = AttributeTextValueText.Text?.Trim();

                if (string.IsNullOrWhiteSpace(textValue))
                {
                    ShowMessage($"{selectedDefinition.Name} üçün mətn dəyəri yazın.", true);
                    return;
                }
            }

            var row = new ShelfAttributeInputRow
            {
                ShelfAttributeDefinitionId = selectedDefinition.Id,
                Name = selectedDefinition.Name,
                Key = selectedDefinition.Key,
                Unit = selectedDefinition.Unit,
                IsLimit = selectedDefinition.IsLimit,
                IsNumeric = selectedDefinition.IsNumeric,
                NumericValue = numericValue,
                TextValue = textValue,
                DisplayValue = BuildAttributeDisplayValue(numericValue, textValue, selectedDefinition.Unit),
                IsLimitText = selectedDefinition.IsLimit ? "Bəli" : "Xeyr"
            };

            _selectedAttributeRows.Add(row);

            AttributeNumericValueText.Text = "";
            AttributeTextValueText.Text = "";

            RefreshShelfAttributesGrid();
            ShowMessage("Rəf xüsusiyyəti əlavə olundu.", false);
        }

        private void RemoveShelfAttribute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not ShelfAttributeInputRow row)
                return;

            _selectedAttributeRows.Remove(row);
            RefreshShelfAttributesGrid();

            ShowMessage("Rəf xüsusiyyəti silindi.", false);
        }

        private void RefreshShelfAttributesGrid()
        {
            ShelfAttributesGrid.ItemsSource = null;
            ShelfAttributesGrid.ItemsSource = _selectedAttributeRows
                .OrderBy(x => x.Name)
                .ToList();
        }

        private async void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShelfId == null)
            {
                ShowMessage("Passiv etmək üçün rəf seçin.", true);
                return;
            }

            var confirm = MessageBox.Show(
                "Bu rəf passiv edilsin?",
                "Təsdiq",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _shelfService.DeactivateAsync(_selectedShelfId.Value);

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            await LoadShelvesAsync();
            ClearForm();
            ShowMessage("Rəf passiv edildi.", false);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _isClearingForm = true;

            _selectedShelfId = null;
            ShelvesGrid.SelectedItem = null;
            ShelfProductsGrid.ItemsSource = null;

            FormTitleText.Text = "Yeni rəf";
            SaveButton.Content = "Yadda saxla";
            DeactivateButton.IsEnabled = false;

            CodeText.Text = "";
            ZoneText.Text = "";
            RowNumberText.Text = "1";
            CapacityText.Text = "100";
            WarehouseCombo.SelectedIndex = WarehouseCombo.Items.Count > 0 ? 0 : -1;

            _selectedAttributeRows = new List<ShelfAttributeInputRow>();
            RefreshShelfAttributesGrid();

            AttributeNumericValueText.Text = "";
            AttributeTextValueText.Text = "";
            ShelfAttributeDefinitionCombo.SelectedIndex =
                ShelfAttributeDefinitionCombo.Items.Count > 0 ? 0 : -1;

            SelectedShelfSummaryText.Text = "Rəf seçilməyib.";

            ShowMessage("Yeni rəf əlavə edə bilərsiniz.", false);

            _isClearingForm = false;
        }

        private decimal ReadDecimalAllowZero(string value, string fieldName)
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

        private string BuildAttributeDisplayValue(decimal? numericValue, string? textValue, string? unit)
        {
            if (numericValue.HasValue)
            {
                var unitText = string.IsNullOrWhiteSpace(unit) ? "" : $" {unit}";
                return $"{numericValue.Value:0.##}{unitText}";
            }

            return textValue ?? "";
        }

        private string GetShelfAttributesText(Shelf shelf)
        {
            if (shelf.AttributeValues == null || !shelf.AttributeValues.Any())
                return "";

            var values = shelf.AttributeValues
                .Where(x =>
                    x.IsActive &&
                    x.ShelfAttributeDefinition != null &&
                    x.ShelfAttributeDefinition.IsActive)
                .OrderBy(x => x.ShelfAttributeDefinition.Name)
                .Select(x =>
                {
                    var value = BuildAttributeDisplayValue(
                        x.NumericValue,
                        x.TextValue,
                        x.ShelfAttributeDefinition.Unit);

                    return $"{x.ShelfAttributeDefinition.Name}: {value}";
                })
                .ToList();

            return string.Join(" / ", values);
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
            MessageText.Foreground = isError
                ? Brushes.Firebrick
                : Brushes.SeaGreen;
        }
    }

    public class ShelfListRow
    {
        public int Id { get; set; }

        public string Code { get; set; } = "";

        public string Zone { get; set; } = "";

        public int RowNumber { get; set; }

        public string WarehouseName { get; set; } = "";

        public decimal Capacity { get; set; }

        public decimal TotalQuantity { get; set; }

        public decimal OccupancyPercent { get; set; }

        public string AttributeSummary { get; set; } = "";

        public string StatusText { get; set; } = "";
    }

    public class ShelfProductRow
    {
        public string ProductCode { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public string AttributesText { get; set; } = "";

        public decimal Quantity { get; set; }
    }

    public class ShelfAttributeDefinitionComboRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Key { get; set; } = "";

        public string Unit { get; set; } = "";

        public bool IsLimit { get; set; }

        public bool IsNumeric { get; set; }

        public string DisplayName { get; set; } = "";
    }

    public class ShelfAttributeInputRow
    {
        public int ShelfAttributeDefinitionId { get; set; }

        public string Name { get; set; } = "";

        public string Key { get; set; } = "";

        public string Unit { get; set; } = "";

        public bool IsLimit { get; set; }

        public bool IsNumeric { get; set; }

        public decimal? NumericValue { get; set; }

        public string? TextValue { get; set; }

        public string DisplayValue { get; set; } = "";

        public string IsLimitText { get; set; } = "";
    }
}