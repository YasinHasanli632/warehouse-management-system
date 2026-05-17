using Anbar.Data;
using Anbar.Entities;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Anbar.Views
{
    public partial class CategoriesView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly CategoryService _categoryService;
        private readonly AttributeService _attributeService;

        // YENI:
        // Ölçü vahidlərini ComboBox-a gətirmək üçün servis.
        private readonly UnitService _unitService;

        private List<Category> _categories = new();
        private List<AttributeDefinition> _attributeDefinitions = new();

        // YENI:
        // Aktiv ölçü vahidləri burada saxlanır.
        private List<Unit> _units = new();

        private int? _selectedCategoryId = null;
        private int? _editingDefinitionId = null;
        private int? _editingValueId = null;
        private bool _isClearingForm = false;

        public CategoriesView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _categoryService = new CategoryService(_context);
            _attributeService = new AttributeService(_context);

            // YENI:
            // UnitService əlavə edildi.
            _unitService = new UnitService(_context);

            Loaded += CategoriesView_Loaded;
        }

        private async void CategoriesView_Loaded(object sender, RoutedEventArgs e)
        {
            CategoryTypeCombo.SelectedIndex = 0;

            // YENI:
            // Əvvəl ölçü vahidləri yüklənir, sonra kateqoriyalar.
            await LoadUnitsAsync();

            await LoadCategoriesAsync();
            ClearForm();
        }

        // YENI:
        // Aktiv ölçü vahidlərini gətirir və DefaultUnitCombo içində göstərir.
        private async System.Threading.Tasks.Task LoadUnitsAsync()
        {
            var result = await _unitService.GetActiveAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            _units = result.Data ?? new List<Unit>();

            DefaultUnitCombo.ItemsSource = _units
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();
        }

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            var result = await _categoryService.GetAllAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            _categories = result.Data ?? new List<Category>();

            ParentCategoryCombo.ItemsSource = _categories
                .Where(x => x.ParentCategoryId == null && x.IsActive)
                .OrderBy(x => x.Name)
                .ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var keyword = SearchText.Text?.Trim().ToLower() ?? "";

            var rows = _categories
                .Where(x =>
                    x.IsActive &&
                    (
                        string.IsNullOrWhiteSpace(keyword) ||
                        x.Name.ToLower().Contains(keyword) ||
                        (x.ParentCategory != null && x.ParentCategory.Name.ToLower().Contains(keyword)) ||
                        (x.Description != null && x.Description.ToLower().Contains(keyword)) ||
                        (x.DefaultUnit != null && x.DefaultUnit.Name.ToLower().Contains(keyword)) ||
                        (x.DefaultUnit != null && x.DefaultUnit.Symbol.ToLower().Contains(keyword))
                    ))
                .OrderBy(x => x.ParentCategoryId == null ? 0 : 1)
                .ThenBy(x => x.ParentCategory?.Name)
                .ThenBy(x => x.Name)
                .Select(x => new CategoryRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    TypeText = x.ParentCategoryId == null ? "Əsas" : "Alt",
                    ParentName = x.ParentCategory?.Name ?? "-",

                    // YENI:
                    // DataGrid-də ölçü vahidi göstərilir.
                    DefaultUnitText = x.DefaultUnit != null
                        ? $"{x.DefaultUnit.Name} ({x.DefaultUnit.Symbol})"
                        : "-",

                    SubCount = x.SubCategories?.Count(s => s.IsActive) ?? 0,
                    ProductCount = x.Products?.Count(p => p.IsActive) ?? 0,
                    Description = x.Description ?? ""
                })
                .ToList();

            CategoriesGrid.ItemsSource = rows;
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CategoryTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ParentCategoryCombo == null)
                return;

            var isSub = IsSubCategorySelected();

            ParentCategoryCombo.IsEnabled = isSub;

            if (!isSub)
                ParentCategoryCombo.SelectedIndex = -1;
        }

        private bool IsSubCategorySelected()
        {
            if (CategoryTypeCombo.SelectedItem is ComboBoxItem item)
                return item.Tag?.ToString() == "Sub";

            return false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = NameText.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowMessage("Kateqoriya adı boş ola bilməz.", true);
                    return;
                }

                int? parentCategoryId = null;

                if (IsSubCategorySelected())
                {
                    if (ParentCategoryCombo.SelectedValue == null)
                    {
                        ShowMessage("Alt kateqoriya üçün ana kateqoriya seçin.", true);
                        return;
                    }

                    parentCategoryId = Convert.ToInt32(ParentCategoryCombo.SelectedValue);
                }

                // YENI:
                // Kateqoriya üçün seçilmiş default ölçü vahidi.
                int? defaultUnitId = null;

                if (DefaultUnitCombo.SelectedValue != null)
                {
                    defaultUnitId = Convert.ToInt32(DefaultUnitCombo.SelectedValue);
                }

                if (_selectedCategoryId == null)
                {
                    var createResult = await _categoryService.CreateAsync(
                        name: name,
                        description: ToNull(DescriptionText.Text),
                        parentCategoryId: parentCategoryId,
                        defaultUnitId: defaultUnitId); // YENI

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Kateqoriya yaradıldı. Xüsusiyyət əlavə etmək üçün siyahıdan seçin.", false);
                }
                else
                {
                    var updateResult = await _categoryService.UpdateAsync(
                        id: _selectedCategoryId.Value,
                        name: name,
                        description: ToNull(DescriptionText.Text),
                        parentCategoryId: parentCategoryId,
                        defaultUnitId: defaultUnitId); // YENI

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Kateqoriya yeniləndi.", false);
                }

                await LoadCategoriesAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private async void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategoryId == null)
            {
                ShowMessage("Passiv etmək üçün kateqoriya seçin.", true);
                return;
            }

            var confirm = MessageBox.Show(
                "Bu kateqoriya passiv edilsin?",
                "Təsdiq",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _categoryService.DeactivateAsync(_selectedCategoryId.Value);

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            await LoadCategoriesAsync();
            ClearForm();

            ShowMessage("Kateqoriya passiv edildi.", false);
        }

        private async void CategoriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isClearingForm)
                return;

            if (CategoriesGrid.SelectedItem is not CategoryRow row)
                return;

            var category = _categories.FirstOrDefault(x => x.Id == row.Id);

            if (category == null)
                return;

            _selectedCategoryId = category.Id;

            FormTitleText.Text = "Kateqoriyanı düzəlt";
            SaveButton.Content = "Dəyişiklikləri yadda saxla";
            DeactivateButton.IsEnabled = true;

            NameText.Text = category.Name;
            DescriptionText.Text = category.Description ?? "";

            // YENI:
            // Seçilmiş kateqoriyanın ölçü vahidi formda göstərilir.
            if (category.DefaultUnitId.HasValue)
                DefaultUnitCombo.SelectedValue = category.DefaultUnitId.Value;
            else
                DefaultUnitCombo.SelectedIndex = -1;

            if (category.ParentCategoryId.HasValue)
            {
                CategoryTypeCombo.SelectedIndex = 1;
                ParentCategoryCombo.IsEnabled = true;
                ParentCategoryCombo.SelectedValue = category.ParentCategoryId.Value;
            }
            else
            {
                CategoryTypeCombo.SelectedIndex = 0;
                ParentCategoryCombo.IsEnabled = false;
                ParentCategoryCombo.SelectedIndex = -1;
            }

            ResetAttributeEditState();
            await LoadAttributesAsync(category.Id);

            ShowMessage("Kateqoriya seçildi. İndi xüsusiyyət və dəyər əlavə/düzəliş edə bilərsiniz.", false);
        }

        private async System.Threading.Tasks.Task LoadAttributesAsync(int categoryId)
        {
            var result = await _attributeService.GetDefinitionsByCategoryAsync(categoryId);

            if (!result.IsSuccess || result.Data == null)
            {
                AttributesGrid.ItemsSource = null;
                AttributeValuesGrid.ItemsSource = null;
                _attributeDefinitions = new List<AttributeDefinition>();
                ShowMessage(result.Message, true);
                return;
            }

            _attributeDefinitions = result.Data;

            var rows = _attributeDefinitions
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new AttributeDefinitionRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    ValuesText = x.Values != null && x.Values.Any(v => v.IsActive)
                        ? string.Join(", ", x.Values.Where(v => v.IsActive).OrderBy(v => v.Value).Select(v => v.Value))
                        : "-"
                })
                .ToList();

            AttributesGrid.ItemsSource = rows;

            if (AttributesGrid.SelectedItem is AttributeDefinitionRow selected)
                LoadValuesGrid(selected.Id);
            else
                AttributeValuesGrid.ItemsSource = null;
        }

        private void LoadValuesGrid(int definitionId)
        {
            var definition = _attributeDefinitions.FirstOrDefault(x => x.Id == definitionId);

            if (definition == null)
            {
                AttributeValuesGrid.ItemsSource = null;
                return;
            }

            var rows = definition.Values
                .Where(x => x.IsActive)
                .OrderBy(x => x.Value)
                .Select(x => new AttributeValueRow
                {
                    Id = x.Id,
                    AttributeDefinitionId = x.AttributeDefinitionId,
                    Value = x.Value
                })
                .ToList();

            AttributeValuesGrid.ItemsSource = rows;
        }

        private void AttributesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AttributesGrid.SelectedItem is not AttributeDefinitionRow selectedDefinition)
                return;

            LoadValuesGrid(selectedDefinition.Id);
            _editingValueId = null;
            AttributeValueText.Text = "";
            AddOrUpdateValueButton.Content = "Dəyər əlavə et";
        }

        private void AttributeValuesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AttributeValuesGrid.SelectedItem is not AttributeValueRow selectedValue)
                return;

            _editingValueId = null;
        }

        private async void AddOrUpdateDefinition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategoryId == null)
                {
                    ShowMessage("Əvvəl siyahıdan kateqoriya seçin. Yeni yaradılan kateqoriya üçün də əvvəl yadda saxlayın, sonra seçin.", true);
                    return;
                }

                var name = AttributeDefinitionText.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowMessage("Xüsusiyyət adı boş ola bilməz.", true);
                    return;
                }

                if (_editingDefinitionId == null)
                {
                    var createResult = await _attributeService.CreateDefinitionAsync(_selectedCategoryId.Value, name);

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Xüsusiyyət əlavə edildi.", false);
                }
                else
                {
                    var updateResult = await UpdateDefinitionAsync(_editingDefinitionId.Value, name);

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Xüsusiyyət yeniləndi.", false);
                }

                AttributeDefinitionText.Text = "";
                _editingDefinitionId = null;
                AddOrUpdateDefinitionButton.Content = "Əlavə et";

                await LoadAttributesAsync(_selectedCategoryId.Value);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private void EditDefinition_Click(object sender, RoutedEventArgs e)
        {
            if (AttributesGrid.SelectedItem is not AttributeDefinitionRow selectedDefinition)
            {
                ShowMessage("Redaktə etmək üçün xüsusiyyət seçin.", true);
                return;
            }

            _editingDefinitionId = selectedDefinition.Id;
            AttributeDefinitionText.Text = selectedDefinition.Name;
            AddOrUpdateDefinitionButton.Content = "Yenilə";

            ShowMessage("Xüsusiyyət adı redaktə rejimindədir. Dəyişib 'Yenilə' düyməsinə basın.", false);
        }

        private async void DeleteDefinition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategoryId == null)
                {
                    ShowMessage("Əvvəl kateqoriya seçin.", true);
                    return;
                }

                if (AttributesGrid.SelectedItem is not AttributeDefinitionRow selectedDefinition)
                {
                    ShowMessage("Silmək üçün xüsusiyyət seçin.", true);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"'{selectedDefinition.Name}' xüsusiyyəti passiv edilsin? Əgər məhsullarda istifadə olunubsa sistem icazə verməyəcək.",
                    "Təsdiq",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await DeactivateDefinitionAsync(selectedDefinition.Id);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                ResetAttributeEditState();
                await LoadAttributesAsync(_selectedCategoryId.Value);

                ShowMessage("Xüsusiyyət passiv edildi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private async void AddOrUpdateValue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategoryId == null)
                {
                    ShowMessage("Əvvəl kateqoriya seçin.", true);
                    return;
                }

                if (AttributesGrid.SelectedItem is not AttributeDefinitionRow selectedDefinition)
                {
                    ShowMessage("Dəyər əlavə etmək üçün əvvəl aşağıdakı siyahıdan xüsusiyyət seçin.", true);
                    return;
                }

                var value = AttributeValueText.Text.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    ShowMessage("Dəyər boş ola bilməz.", true);
                    return;
                }

                if (_editingValueId == null)
                {
                    var createResult = await _attributeService.CreateValueAsync(selectedDefinition.Id, value);

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Dəyər əlavə edildi.", false);
                }
                else
                {
                    var updateResult = await UpdateValueAsync(_editingValueId.Value, value);

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Dəyər yeniləndi.", false);
                }

                AttributeValueText.Text = "";
                _editingValueId = null;
                AddOrUpdateValueButton.Content = "Dəyər əlavə et";

                await LoadAttributesAsync(_selectedCategoryId.Value);

                var definitionRow = _attributeDefinitions.FirstOrDefault(x => x.Id == selectedDefinition.Id);
                if (definitionRow != null)
                    LoadValuesGrid(selectedDefinition.Id);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private void EditValue_Click(object sender, RoutedEventArgs e)
        {
            if (AttributeValuesGrid.SelectedItem is not AttributeValueRow selectedValue)
            {
                ShowMessage("Redaktə etmək üçün dəyər seçin.", true);
                return;
            }

            _editingValueId = selectedValue.Id;
            AttributeValueText.Text = selectedValue.Value;
            AddOrUpdateValueButton.Content = "Dəyəri yenilə";

            ShowMessage("Dəyər redaktə rejimindədir. Dəyişib 'Dəyəri yenilə' düyməsinə basın.", false);
        }

        private async void DeleteValue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategoryId == null)
                {
                    ShowMessage("Əvvəl kateqoriya seçin.", true);
                    return;
                }

                if (AttributeValuesGrid.SelectedItem is not AttributeValueRow selectedValue)
                {
                    ShowMessage("Silmək üçün dəyər seçin.", true);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"'{selectedValue.Value}' dəyəri passiv edilsin? Əgər məhsullarda istifadə olunubsa sistem icazə verməyəcək.",
                    "Təsdiq",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await DeactivateValueAsync(selectedValue.Id);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                AttributeValueText.Text = "";
                _editingValueId = null;
                AddOrUpdateValueButton.Content = "Dəyər əlavə et";

                await LoadAttributesAsync(_selectedCategoryId.Value);

                ShowMessage("Dəyər passiv edildi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private async System.Threading.Tasks.Task<Anbar.Entities.Common.Result<bool>> UpdateDefinitionAsync(int definitionId, string name)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Anbar.Entities.Common.Result<bool>.Fail("Xüsusiyyət adı boş ola bilməz.");

            var definition = await _context.AttributeDefinitions
                .FirstOrDefaultAsync(x => x.Id == definitionId && x.IsActive);

            if (definition == null)
                return Anbar.Entities.Common.Result<bool>.Fail("Xüsusiyyət tapılmadı.");

            var exists = await _context.AttributeDefinitions
                .AnyAsync(x =>
                    x.Id != definitionId &&
                    x.CategoryId == definition.CategoryId &&
                    x.IsActive &&
                    x.Name.ToLower() == name.ToLower());

            if (exists)
                return Anbar.Entities.Common.Result<bool>.Fail("Bu kateqoriyada belə xüsusiyyət artıq mövcuddur.");

            definition.Name = name;
            definition.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Anbar.Entities.Common.Result<bool>.Success(true);
        }

        private async System.Threading.Tasks.Task<Anbar.Entities.Common.Result<bool>> DeactivateDefinitionAsync(int definitionId)
        {
            var definition = await _context.AttributeDefinitions
                .Include(x => x.Values)
                .FirstOrDefaultAsync(x => x.Id == definitionId && x.IsActive);

            if (definition == null)
                return Anbar.Entities.Common.Result<bool>.Fail("Xüsusiyyət tapılmadı.");

            var usedInProduct = await _context.ProductAttributes
                .AnyAsync(x => x.AttributeDefinitionId == definitionId && x.IsActive);

            if (usedInProduct)
                return Anbar.Entities.Common.Result<bool>.Fail("Bu xüsusiyyət məhsullarda istifadə olunub. Məlumat itməsin deyə silmək olmaz. Lazımdırsa əvvəl məhsullardan dəyişin.");

            definition.IsActive = false;
            definition.UpdatedAt = DateTime.Now;

            foreach (var value in definition.Values.Where(x => x.IsActive))
            {
                value.IsActive = false;
                value.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Anbar.Entities.Common.Result<bool>.Success(true);
        }

        private async System.Threading.Tasks.Task<Anbar.Entities.Common.Result<bool>> UpdateValueAsync(int valueId, string valueText)
        {
            valueText = valueText.Trim();

            if (string.IsNullOrWhiteSpace(valueText))
                return Anbar.Entities.Common.Result<bool>.Fail("Dəyər boş ola bilməz.");

            var value = await _context.AttributeValues
                .FirstOrDefaultAsync(x => x.Id == valueId && x.IsActive);

            if (value == null)
                return Anbar.Entities.Common.Result<bool>.Fail("Dəyər tapılmadı.");

            var exists = await _context.AttributeValues
                .AnyAsync(x =>
                    x.Id != valueId &&
                    x.AttributeDefinitionId == value.AttributeDefinitionId &&
                    x.IsActive &&
                    x.Value.ToLower() == valueText.ToLower());

            if (exists)
                return Anbar.Entities.Common.Result<bool>.Fail("Bu xüsusiyyət üçün belə dəyər artıq mövcuddur.");

            value.Value = valueText;
            value.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Anbar.Entities.Common.Result<bool>.Success(true);
        }

        private async System.Threading.Tasks.Task<Anbar.Entities.Common.Result<bool>> DeactivateValueAsync(int valueId)
        {
            var value = await _context.AttributeValues
                .FirstOrDefaultAsync(x => x.Id == valueId && x.IsActive);

            if (value == null)
                return Anbar.Entities.Common.Result<bool>.Fail("Dəyər tapılmadı.");

            var usedInProduct = await _context.ProductAttributes
                .AnyAsync(x => x.AttributeValueId == valueId && x.IsActive);

            if (usedInProduct)
                return Anbar.Entities.Common.Result<bool>.Fail("Bu dəyər məhsullarda istifadə olunub. Məlumat itməsin deyə silmək olmaz. Lazımdırsa əvvəl məhsullardan dəyişin.");

            value.IsActive = false;
            value.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Anbar.Entities.Common.Result<bool>.Success(true);
        }

        private void NewCategory_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _isClearingForm = true;

            _selectedCategoryId = null;
            CategoriesGrid.SelectedItem = null;

            FormTitleText.Text = "Yeni kateqoriya";
            SaveButton.Content = "Yadda saxla";
            DeactivateButton.IsEnabled = false;

            CategoryTypeCombo.SelectedIndex = 0;
            ParentCategoryCombo.SelectedIndex = -1;
            ParentCategoryCombo.IsEnabled = false;

            // YENI:
            // Yeni kateqoriya formunda ölçü vahidi boş saxlanır.
            // İstəsən burada default vahidi avtomatik seçdirə bilərik.
            DefaultUnitCombo.SelectedIndex = -1;

            NameText.Text = "";
            DescriptionText.Text = "";

            AttributeDefinitionText.Text = "";
            AttributeValueText.Text = "";
            AttributesGrid.ItemsSource = null;
            AttributeValuesGrid.ItemsSource = null;
            _attributeDefinitions = new List<AttributeDefinition>();

            ResetAttributeEditState();

            ShowMessage("Yeni kateqoriya əlavə edə bilərsiniz.", false);

            _isClearingForm = false;
        }

        private void ResetAttributeEditState()
        {
            _editingDefinitionId = null;
            _editingValueId = null;

            if (AddOrUpdateDefinitionButton != null)
                AddOrUpdateDefinitionButton.Content = "Əlavə et";

            if (AddOrUpdateValueButton != null)
                AddOrUpdateValueButton.Content = "Dəyər əlavə et";

            if (AttributeDefinitionText != null)
                AttributeDefinitionText.Text = "";

            if (AttributeValueText != null)
                AttributeValueText.Text = "";
        }

        private string? ToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError ? Brushes.Firebrick : Brushes.SeaGreen;
        }
    }

    public class CategoryRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string TypeText { get; set; } = "";

        public string ParentName { get; set; } = "";

        // YENI:
        // DataGrid üçün ölçü vahidi mətni.
        public string DefaultUnitText { get; set; } = "";

        public int SubCount { get; set; }

        public int ProductCount { get; set; }

        public string Description { get; set; } = "";
    }

    public class AttributeDefinitionRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string ValuesText { get; set; } = "";
    }

    public class AttributeValueRow
    {
        public int Id { get; set; }

        public int AttributeDefinitionId { get; set; }

        public string Value { get; set; } = "";
    }
}