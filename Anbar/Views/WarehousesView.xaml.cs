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
    public partial class WarehousesView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly WarehouseService _warehouseService;

        private List<Warehouse> _warehouses = new();
        private List<WarehouseListRow> _rows = new();

        private int? _selectedWarehouseId = null;
        private bool _isClearingForm = false;

        public WarehousesView()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _warehouseService = new WarehouseService(_context);

            Loaded += WarehousesView_Loaded;
        }

        private async void WarehousesView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadWarehousesAsync();
            ClearForm();
        }

        private async System.Threading.Tasks.Task LoadWarehousesAsync()
        {
            var result = await _warehouseService.GetAllAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            _warehouses = result.Data ?? new List<Warehouse>();

            _rows = _warehouses
                .Select(x => new WarehouseListRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Address = x.Address ?? "",
                    Description = x.Description ?? "",
                    CreatedAtText = x.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                })
                .ToList();

            ApplyFilter();

            ShowMessage("Anbarlar yükləndi.", false);
        }

        private void ApplyFilter()
        {
            var keyword = SearchText.Text?.Trim().ToLower() ?? "";

            var filtered = _rows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(x =>
                    x.Name.ToLower().Contains(keyword) ||
                    x.Code.ToLower().Contains(keyword) ||
                    x.Address.ToLower().Contains(keyword) ||
                    x.Description.ToLower().Contains(keyword));
            }

            WarehousesGrid.ItemsSource = filtered
                .OrderBy(x => x.Name)
                .ToList();
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadWarehousesAsync();
        }

        private void NewWarehouse_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void WarehousesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isClearingForm)
                return;

            if (WarehousesGrid.SelectedItem is not WarehouseListRow row)
                return;

            var warehouse = _warehouses.FirstOrDefault(x => x.Id == row.Id);

            if (warehouse == null)
                return;

            _selectedWarehouseId = warehouse.Id;

            FormTitleText.Text = "Anbarı düzəlt";
            SaveButton.Content = "Dəyişiklikləri yadda saxla";
            DeactivateButton.IsEnabled = true;

            NameText.Text = warehouse.Name;
            CodeText.Text = warehouse.Code;
            AddressText.Text = warehouse.Address ?? "";
            DescriptionText.Text = warehouse.Description ?? "";

            ShowMessage("Anbar seçildi. Düzəliş edə bilərsiniz.", false);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = NameText.Text.Trim();
                var code = CodeText.Text.Trim();
                var address = AddressText.Text.Trim();
                var description = DescriptionText.Text.Trim();

                if (_selectedWarehouseId == null)
                {
                    var createResult = await _warehouseService.CreateAsync(
                        name,
                        code,
                        address,
                        description);

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Anbar yaradıldı.", false);
                }
                else
                {
                    var updateResult = await _warehouseService.UpdateAsync(
                        _selectedWarehouseId.Value,
                        name,
                        code,
                        address,
                        description);

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Anbar yeniləndi.", false);
                }

                await LoadWarehousesAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private async void CreateDefaultWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var result = await _warehouseService.EnsureDefaultWarehouseAsync();

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            await LoadWarehousesAsync();

            ShowMessage(result.Message, false);
        }

        private async void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedWarehouseId == null)
            {
                ShowMessage("Passiv etmək üçün anbar seçin.", true);
                return;
            }

            var confirm = MessageBox.Show(
                "Bu anbar passiv edilsin?",
                "Təsdiq",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _warehouseService.DeactivateAsync(_selectedWarehouseId.Value);

            if (!result.IsSuccess)
            {
                ShowMessage(result.Message, true);
                return;
            }

            await LoadWarehousesAsync();
            ClearForm();

            ShowMessage("Anbar passiv edildi.", false);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _isClearingForm = true;

            _selectedWarehouseId = null;
            WarehousesGrid.SelectedItem = null;

            FormTitleText.Text = "Yeni anbar";
            SaveButton.Content = "Yadda saxla";
            DeactivateButton.IsEnabled = false;

            NameText.Text = "";
            CodeText.Text = "";
            AddressText.Text = "";
            DescriptionText.Text = "";

            ShowMessage("Yeni anbar əlavə edə bilərsiniz.", false);

            _isClearingForm = false;
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError
                ? Brushes.Firebrick
                : Brushes.SeaGreen;
        }
    }

    public class WarehouseListRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Code { get; set; } = "";

        public string Address { get; set; } = "";

        public string Description { get; set; } = "";

        public string CreatedAtText { get; set; } = "";
    }
}