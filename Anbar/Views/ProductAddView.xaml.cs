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
using System.Windows.Media;

namespace Anbar.Views
{
    public partial class ProductAddView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;
        private readonly AttributeService _attributeService;
        private readonly UnitService _unitService;

        private readonly int? _editProductId;

        private List<Category> _categories = new();
        private List<Unit> _units = new();
        private List<ProductAddAttributeRow> _selectedAttributes = new();

        private bool _isLoaded = false;
        private bool _isBindingCategory = false;
        private int _lastSelectedCategoryId = 0;

        private bool _isEditMode => _editProductId.HasValue;

        public ProductAddView() : this(null)
        {
        }

        public ProductAddView(int? productId)
        {
            InitializeComponent();

            _editProductId = productId;

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _productService = new ProductService(_context);
            _attributeService = new AttributeService(_context);
            _unitService = new UnitService(_context);

            Loaded += ProductAddView_Loaded;
        }

        private async void ProductAddView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isLoaded)
                    return;

                InitStaticCombos();

                await LoadUnitsAsync();
                await LoadCategoriesAsync();

                BuildDefaultProductTaxes();

                _isLoaded = true;

                if (_isEditMode)
                    await LoadProductForEditAsync(_editProductId!.Value);
                else
                    PrepareCreateMode();
            }
            catch (Exception ex)
            {
                ShowMessage($"Səhifə yüklənmədi: {ex.Message}", true);
            }
        }

        private void InitStaticCombos()
        {
            StatusCombo.ItemsSource = new List<string>
            {
                "Aktiv",
                "Passiv"
            };

            StatusCombo.SelectedIndex = 0;

            AttributeSetCombo.ItemsSource = new List<string>
            {
                "Standart"
            };

            AttributeSetCombo.SelectedIndex = 0;
        }

        private async Task LoadUnitsAsync()
        {
            var result = await _unitService.GetActiveAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            _units = result.Data ?? new List<Unit>();

            UnitCombo.ItemsSource = _units
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

            var defaultUnit = _units.FirstOrDefault(x => x.IsDefault);

            if (defaultUnit != null)
                UnitCombo.SelectedValue = defaultUnit.Id;
            else
                UnitCombo.SelectedIndex = -1;
        }

        private async Task LoadCategoriesAsync()
        {
            _categories = await _context.Categories
                .Include(x => x.DefaultUnit)
                .Where(x => x.IsActive)
                .OrderBy(x => x.ParentCategoryId)
                .ThenBy(x => x.Name)
                .ToListAsync();

            MainCategoryCombo.ItemsSource = _categories
                .Where(x => x.ParentCategoryId == null)
                .OrderBy(x => x.Name)
                .ToList();

            SubCategoryCombo.ItemsSource = null;
            SubCategoryCombo.SelectedIndex = -1;
            SubCategoryCombo.IsEnabled = false;
        }

        private void PrepareCreateMode()
        {
            PageTitleText.Text = "Yeni Məhsul Əlavə Et";
            SaveButton.Content = "💾  Yadda Saxla";

            LastCostText.Text = "0.00";
            AverageCostText.Text = "0.00";

            ShowMessage("Yeni məhsul əlavə edə bilərsiniz.", false);
        }

        private async Task LoadProductForEditAsync(int productId)
        {
            var result = await _productService.GetByIdAsync(productId);

            if (!result.IsSuccess || result.Data == null)
            {
                ShowMessage(result.Message, true);
                return;
            }

            var product = result.Data;

            PageTitleText.Text = "Məhsulu Redaktə Et";
            SaveButton.Content = "💾  Dəyişiklikləri Saxla";

            NameText.Text = product.Name;
            CodeText.Text = product.Code;
            CodeText.IsEnabled = false;

            BarcodeText.Text = product.Barcode ?? "";

            SelectProductCategory(product);
            SelectProductUnit(product);

            StatusCombo.SelectedIndex = product.IsActive && product.Status == ProductStatus.Active ? 0 : 1;

            DescriptionText.Text = product.Description ?? "";

            PurchasePriceText.Text = product.PurchasePrice.ToString("0.##", CultureInfo.InvariantCulture);
            SalePriceText.Text = product.SalePrice.ToString("0.##", CultureInfo.InvariantCulture);
            MinStockText.Text = product.MinStockQuantity.ToString("0.##", CultureInfo.InvariantCulture);

            LastCostText.Text = product.LastCostPrice.ToString("0.00", CultureInfo.InvariantCulture);
            AverageCostText.Text = product.AverageCostPrice.ToString("0.00", CultureInfo.InvariantCulture);

            IsVatApplicableCheck.IsChecked = product.IsVatApplicable;
            VatRateText.Text = product.VatRate.ToString("0.##", CultureInfo.InvariantCulture);
            IsPurchasePriceVatIncludedCheck.IsChecked = product.IsPurchasePriceVatIncluded;
            IsVatRecoverableCheck.IsChecked = product.IsVatRecoverable;
            IsExciseApplicableCheck.IsChecked = product.IsExciseApplicable;
            IsImportTaxExemptCheck.IsChecked = product.IsImportTaxExempt;

            WeightText.Text = product.Weight.ToString("0.###", CultureInfo.InvariantCulture);
            VolumeText.Text = product.Volume.ToString("0.###", CultureInfo.InvariantCulture);

            await LoadAttributeDefinitionsAsync(product.CategoryId);

            _selectedAttributes = product.Attributes
                .Where(x =>
                    x.IsActive &&
                    x.AttributeValue != null &&
                    x.AttributeValue.IsActive &&
                    x.AttributeValue.AttributeDefinition != null &&
                    x.AttributeValue.AttributeDefinition.IsActive)
                .Select(x => new ProductAddAttributeRow
                {
                    AttributeDefinitionId = x.AttributeValue.AttributeDefinitionId,
                    AttributeValueId = x.AttributeValueId,
                    DefinitionName = x.AttributeValue.AttributeDefinition.Name,
                    Value = x.AttributeValue.Value
                })
                .GroupBy(x => x.AttributeDefinitionId)
                .Select(x => x.First())
                .OrderBy(x => x.DefinitionName)
                .ToList();

            RefreshAttributesGrid();

            BindProductTaxesFromProduct(product);
            BindStockBatches(product);

            ShowMessage("Məhsul məlumatları yükləndi.", false);
        }

        private void SelectProductCategory(Product product)
        {
            try
            {
                _isBindingCategory = true;

                if (product.Category != null && product.Category.ParentCategoryId.HasValue)
                {
                    MainCategoryCombo.SelectedValue = product.Category.ParentCategoryId.Value;

                    var subCategories = _categories
                        .Where(x => x.ParentCategoryId == product.Category.ParentCategoryId.Value)
                        .OrderBy(x => x.Name)
                        .ToList();

                    SubCategoryCombo.ItemsSource = subCategories;
                    SubCategoryCombo.IsEnabled = subCategories.Any();
                    SubCategoryCombo.SelectedValue = product.CategoryId;
                }
                else
                {
                    MainCategoryCombo.SelectedValue = product.CategoryId;
                    SubCategoryCombo.ItemsSource = null;
                    SubCategoryCombo.SelectedIndex = -1;
                    SubCategoryCombo.IsEnabled = false;
                }

                _lastSelectedCategoryId = product.CategoryId;
            }
            finally
            {
                _isBindingCategory = false;
            }
        }

        private void SelectProductUnit(Product product)
        {
            if (product.UnitId.HasValue)
            {
                UnitCombo.SelectedValue = product.UnitId.Value;
                return;
            }

            var oldUnit = product.Unit?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(oldUnit))
            {
                var matchedUnit = _units.FirstOrDefault(x =>
                    x.Symbol.ToLower() == oldUnit ||
                    x.Name.ToLower() == oldUnit ||
                    x.Key.ToLower() == oldUnit);

                if (matchedUnit != null)
                {
                    UnitCombo.SelectedValue = matchedUnit.Id;
                    return;
                }
            }

            var defaultUnit = _units.FirstOrDefault(x => x.IsDefault);

            if (defaultUnit != null)
                UnitCombo.SelectedValue = defaultUnit.Id;
            else
                UnitCombo.SelectedIndex = -1;
        }

        private async void MainCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || _isBindingCategory)
                return;

            try
            {
                LoadSubCategoriesForSelectedMain();

                var selectedCategoryId = GetSelectedCategoryId();

                if (selectedCategoryId > 0)
                    await HandleCategoryChangedAsync(selectedCategoryId);
            }
            catch (Exception ex)
            {
                ShowMessage($"Ana kateqoriya seçilərkən xəta: {ex.Message}", true);
            }
        }

        private async void SubCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || _isBindingCategory)
                return;

            try
            {
                var selectedCategoryId = GetSelectedCategoryId();

                if (selectedCategoryId > 0)
                    await HandleCategoryChangedAsync(selectedCategoryId);
            }
            catch (Exception ex)
            {
                ShowMessage($"Alt kateqoriya seçilərkən xəta: {ex.Message}", true);
            }
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

        private async Task HandleCategoryChangedAsync(int categoryId)
        {
            if (categoryId <= 0)
                return;

            if (_lastSelectedCategoryId > 0 && _lastSelectedCategoryId != categoryId)
            {
                _selectedAttributes.Clear();
                RefreshAttributesGrid();
            }

            _lastSelectedCategoryId = categoryId;

            ApplyDefaultUnitFromCategory(categoryId);

            await LoadAttributeDefinitionsAsync(categoryId);
        }

        private void ApplyDefaultUnitFromCategory(int categoryId)
        {
            var category = _categories.FirstOrDefault(x => x.Id == categoryId);

            if (category != null && category.DefaultUnitId.HasValue)
            {
                UnitCombo.SelectedValue = category.DefaultUnitId.Value;
                return;
            }

            var defaultUnit = _units.FirstOrDefault(x => x.IsDefault);

            if (defaultUnit != null)
                UnitCombo.SelectedValue = defaultUnit.Id;
        }

        private async Task LoadAttributeDefinitionsAsync(int categoryId)
        {
            AttributeDefinitionCombo.ItemsSource = null;
            AttributeDefinitionCombo.SelectedIndex = -1;

            AttributeValueCombo.ItemsSource = null;
            AttributeValueCombo.SelectedIndex = -1;
            AttributeValueCombo.IsEnabled = false;

            var result = await _attributeService.GetDefinitionsByCategoryAsync(categoryId);

            if (!result.IsSuccess || result.Data == null)
            {
                ShowMessage(result.Message, true);
                return;
            }

            AttributeDefinitionCombo.ItemsSource = result.Data
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToList();
        }

        private async void AttributeDefinitionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                AttributeValueCombo.ItemsSource = null;
                AttributeValueCombo.SelectedIndex = -1;
                AttributeValueCombo.IsEnabled = false;

                if (AttributeDefinitionCombo.SelectedValue == null)
                    return;

                var definitionId = Convert.ToInt32(AttributeDefinitionCombo.SelectedValue);

                var result = await _attributeService.GetValuesByDefinitionAsync(definitionId);

                if (!result.IsSuccess || result.Data == null)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                var values = result.Data
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Value)
                    .ToList();

                AttributeValueCombo.ItemsSource = values;
                AttributeValueCombo.IsEnabled = values.Any();
            }
            catch (Exception ex)
            {
                ShowMessage($"Xüsusiyyət dəyərləri yüklənmədi: {ex.Message}", true);
            }
        }

        private void AddAttribute_Click(object sender, RoutedEventArgs e)
        {
            if (AttributeDefinitionCombo.SelectedItem is not AttributeDefinition definition)
            {
                ShowMessage("Xüsusiyyət tipi seçin.", true);
                return;
            }

            if (AttributeValueCombo.SelectedItem is not AttributeValue value)
            {
                ShowMessage("Xüsusiyyət dəyəri seçin.", true);
                return;
            }

            if (_selectedAttributes.Any(x => x.AttributeDefinitionId == definition.Id))
            {
                ShowMessage("Bu xüsusiyyət artıq əlavə edilib.", true);
                return;
            }

            _selectedAttributes.Add(new ProductAddAttributeRow
            {
                AttributeDefinitionId = definition.Id,
                AttributeValueId = value.Id,
                DefinitionName = definition.Name,
                Value = value.Value
            });

            _selectedAttributes = _selectedAttributes
                .OrderBy(x => x.DefinitionName)
                .ToList();

            RefreshAttributesGrid();

            AttributeDefinitionCombo.SelectedIndex = -1;
            AttributeValueCombo.ItemsSource = null;
            AttributeValueCombo.IsEnabled = false;

            ShowMessage("Xüsusiyyət əlavə edildi.", false);
        }

        private void RemoveAttribute_Click(object sender, RoutedEventArgs e)
        {
            if (AttributesGrid.SelectedItem is not ProductAddAttributeRow selected)
            {
                ShowMessage("Silmək üçün xüsusiyyət seçin.", true);
                return;
            }

            _selectedAttributes.Remove(selected);
            RefreshAttributesGrid();

            ShowMessage("Xüsusiyyət silindi.", false);
        }

        private void RefreshAttributesGrid()
        {
            AttributesGrid.ItemsSource = null;
            AttributesGrid.ItemsSource = _selectedAttributes;
        }

        private void BuildDefaultProductTaxes()
        {
            ProductTaxesGrid.ItemsSource = new List<ProductTaxRow>
            {
                new ProductTaxRow
                {
                    TaxType = "ƏDV",
                    Rate = "18.00",
                    IsApplicable = true,
                    IsRecoverable = true,
                    Description = "Standart ƏDV"
                },
                new ProductTaxRow
                {
                    TaxType = "Aksiz",
                    Rate = "0.00",
                    IsApplicable = false,
                    IsRecoverable = false,
                    Description = "Aksiz tətbiq olunmur"
                },
                new ProductTaxRow
                {
                    TaxType = "Gömrük vergisi",
                    Rate = "0.00",
                    IsApplicable = false,
                    IsRecoverable = false,
                    Description = "Gömrük vergisi tətbiq olunmur"
                }
            };
        }

        private void BindProductTaxesFromProduct(Product product)
        {
            ProductTaxesGrid.ItemsSource = new List<ProductTaxRow>
            {
                new ProductTaxRow
                {
                    TaxType = "ƏDV",
                    Rate = product.VatRate.ToString("0.00", CultureInfo.InvariantCulture),
                    IsApplicable = product.IsVatApplicable,
                    IsRecoverable = product.IsVatRecoverable,
                    Description = "Standart ƏDV"
                },
                new ProductTaxRow
                {
                    TaxType = "Aksiz",
                    Rate = "0.00",
                    IsApplicable = product.IsExciseApplicable,
                    IsRecoverable = false,
                    Description = product.IsExciseApplicable ? "Aksiz tətbiq olunur" : "Aksiz tətbiq olunmur"
                },
                new ProductTaxRow
                {
                    TaxType = "Gömrük vergisi",
                    Rate = "0.00",
                    IsApplicable = !product.IsImportTaxExempt,
                    IsRecoverable = false,
                    Description = product.IsImportTaxExempt ? "İdxal vergisindən azaddır" : "Gömrük vergisi tətbiq oluna bilər"
                }
            };
        }

        private void BindStockBatches(Product product)
        {
            StockBatchesGrid.ItemsSource = product.StockBatches?
                .OrderByDescending(x => x.Id)
                .ToList();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveButton.IsEnabled = false;

                var dto = BuildDtoFromForm();

                if (_isEditMode)
                {
                    dto.Id = _editProductId;

                    var updateResult = await _productService.UpdateAsync(dto);

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Məhsul yeniləndi.", false);
                    CloseParentWindow();
                }
                else
                {
                    var createResult = await _productService.CreateAsync(dto);

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Məhsul yaradıldı.", false);
                    CloseParentWindow();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Yadda saxlanmadı: {ex.Message}", true);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private ProductSaveDto BuildDtoFromForm()
        {
            var name = NameText.Text.Trim();
            var code = CodeText.Text.Trim();
            var barcode = BarcodeText.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Məhsul adı boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(code))
                throw new Exception("Məhsul kodu boş ola bilməz.");

            var categoryId = GetSelectedCategoryId();

            if (categoryId <= 0)
                throw new Exception("Kateqoriya seçin.");

            if (UnitCombo.SelectedValue == null)
                throw new Exception("Ölçü vahidi seçin.");

            var unitId = Convert.ToInt32(UnitCombo.SelectedValue);
            var selectedUnit = _units.FirstOrDefault(x => x.Id == unitId);

            var purchasePrice = ReadDecimal(PurchasePriceText.Text, "Alış qiyməti");
            var salePrice = ReadDecimal(SalePriceText.Text, "Satış qiyməti");
            var minStock = ReadDecimal(MinStockText.Text, "Minimum stok");
            var vatRate = ReadDecimal(VatRateText.Text, "ƏDV faizi");
            var weight = ReadDecimal(WeightText.Text, "Çəki");
            var volume = ReadDecimal(VolumeText.Text, "Həcm");

            return new ProductSaveDto
            {
                Name = name,
                Code = code,
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,

                CategoryId = categoryId,
                UnitId = unitId,
                Unit = selectedUnit?.Symbol ?? "ədəd",

                PurchasePrice = purchasePrice,
                SalePrice = salePrice,
                MinStockQuantity = minStock,

                IsVatApplicable = IsVatApplicableCheck.IsChecked == true,
                VatRate = vatRate,
                IsPurchasePriceVatIncluded = IsPurchasePriceVatIncludedCheck.IsChecked == true,
                IsVatRecoverable = IsVatRecoverableCheck.IsChecked == true,
                IsExciseApplicable = IsExciseApplicableCheck.IsChecked == true,
                IsImportTaxExempt = IsImportTaxExemptCheck.IsChecked == true,

                Weight = weight,
                Volume = volume,

                Description = DescriptionText.Text.Trim(),

                Status = StatusCombo.SelectedIndex == 0
                    ? ProductStatus.Active
                    : ProductStatus.Passive,

                AttributeValueIds = _selectedAttributes
                    .Select(x => x.AttributeValueId)
                    .Distinct()
                    .ToList()
            };
        }

        private decimal ReadDecimal(string value, string fieldName)
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseParentWindow();
        }

        private void CloseParentWindow()
        {
            Window.GetWindow(this)?.Close();
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError
                ? Brushes.Firebrick
                : Brushes.SeaGreen;
        }
    }

    public class ProductAddAttributeRow
    {
        public int AttributeDefinitionId { get; set; }
        public int AttributeValueId { get; set; }
        public string DefinitionName { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class ProductTaxRow
    {
        public string TaxType { get; set; } = "";
        public string Rate { get; set; } = "";
        public bool IsApplicable { get; set; }
        public bool IsRecoverable { get; set; }
        public string Description { get; set; } = "";
    }
}