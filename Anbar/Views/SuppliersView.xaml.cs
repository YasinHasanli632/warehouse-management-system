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

namespace Anbar.Views
{
    public partial class SuppliersView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly SupplierService _supplierService;

        // YENI:
        // Təchizatçı ödənişləri üçün servis.
        private readonly SupplierPaymentService _supplierPaymentService;

        private List<Supplier> _suppliers = new();
        private int? _selectedSupplierId = null;
        private bool _isClearingForm = false;

        public SuppliersView()
        {
            InitializeComponent();

            // YENI: Real SQL Server bağlantısı.
            // Sənin lokal server connection string-in.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _supplierService = new SupplierService(_context);

            // YENI:
            // SupplierPaymentService eyni DbContext ilə işləyir ki,
            // ödəniş və supplier borcu eyni database üzərində idarə olunsun.
            _supplierPaymentService = new SupplierPaymentService(_context);

            Loaded += SuppliersView_Loaded;
        }

        private async void SuppliersView_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareCombos();
            PreparePaymentDefaults();
            await LoadSuppliersAsync();
            ClearForm();
        }

        private void PrepareCombos()
        {
            CurrencyCombo.ItemsSource = Enum.GetValues(typeof(CurrencyType));
            PaymentTypeCombo.ItemsSource = Enum.GetValues(typeof(PaymentType));

            CurrencyCombo.SelectedItem = CurrencyType.AZN;
            PaymentTypeCombo.SelectedItem = PaymentType.Cash;

            // YENI:
            // Ödəniş yaratma paneli üçün ayrıca payment type combo.
            PaymentCreateTypeCombo.ItemsSource = Enum.GetValues(typeof(PaymentType));
            PaymentCreateTypeCombo.SelectedItem = PaymentType.Cash;
        }

        private void PreparePaymentDefaults()
        {
            PaymentDatePicker.SelectedDate = DateTime.Now;
            PaymentAmountText.Text = "";
            PaymentNoteText.Text = "";
            SupplierPaymentsGrid.ItemsSource = null;
            SelectedSupplierDebtText.Text = "0.00 AZN";
        }

        private async System.Threading.Tasks.Task LoadSuppliersAsync()
        {
            try
            {
                var result = await _supplierService.GetAllAsync();

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                _suppliers = result.Data ?? new List<Supplier>();

                ApplyFilter();
                ShowMessage("Təchizatçı siyahısı yükləndi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Təchizatçılar yüklənmədi: {ex.Message}", true);
            }
        }

        private void ApplyFilter()
        {
            var keyword = SearchText.Text?.Trim().ToLower() ?? "";

            var filtered = _suppliers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(x =>
                    (x.Name ?? "").ToLower().Contains(keyword) ||
                    (x.CompanyName ?? "").ToLower().Contains(keyword) ||
                    (x.Phone ?? "").ToLower().Contains(keyword) ||
                    (x.Email ?? "").ToLower().Contains(keyword) ||
                    (x.Voen ?? "").ToLower().Contains(keyword) ||
                    (x.BankName ?? "").ToLower().Contains(keyword));
            }

            var list = filtered
                .OrderBy(x => x.Name)
                .ToList();

            SuppliersGrid.ItemsSource = list;
            TotalCountText.Text = $"{list.Count} qeyd";

            // YENI:
            // Görünən supplier-lər üzrə ümumi borc.
            var totalDebt = list.Sum(x => x.DebtAmount);
            TotalDebtText.Text = $"Borc: {totalDebt:N2} AZN";
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();

            if (_selectedSupplierId.HasValue)
                await LoadSupplierPaymentsAsync(_selectedSupplierId.Value);
        }

        private void NewSupplier_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void SuppliersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isClearingForm)
                return;

            if (SuppliersGrid.SelectedItem is not Supplier supplier)
                return;

            _selectedSupplierId = supplier.Id;

            FormTitleText.Text = "Təchizatçını düzəlt";
            SaveButton.Content = "Dəyişiklikləri yadda saxla";
            DeactivateButton.IsEnabled = true;

            NameText.Text = supplier.Name ?? "";
            CompanyNameText.Text = supplier.CompanyName ?? "";
            PhoneText.Text = supplier.Phone ?? "";
            EmailText.Text = supplier.Email ?? "";
            AddressText.Text = supplier.Address ?? "";
            VoenText.Text = supplier.Voen ?? "";
            BankNameText.Text = supplier.BankName ?? "";
            BankAccountText.Text = supplier.BankAccount ?? "";
            CurrencyCombo.SelectedItem = supplier.Currency;
            PaymentTypeCombo.SelectedItem = supplier.PaymentType;
            NoteText.Text = supplier.Note ?? "";

            // YENI:
            // Seçilmiş supplier üçün cari borc göstərilir.
            SelectedSupplierDebtText.Text = $"{supplier.DebtAmount:N2} AZN";

            PaymentAmountText.Text = "";
            PaymentNoteText.Text = "";
            PaymentDatePicker.SelectedDate = DateTime.Now;
            PaymentCreateTypeCombo.SelectedItem = supplier.PaymentType;

            await LoadSupplierPaymentsAsync(supplier.Id);

            ShowMessage("Təchizatçı seçildi. Düzəliş və ödəniş edə bilərsiniz.", false);
        }

        private async System.Threading.Tasks.Task LoadSupplierPaymentsAsync(int supplierId)
        {
            try
            {
                var result = await _supplierPaymentService.GetBySupplierIdAsync(supplierId);

                if (!result.IsSuccess)
                {
                    SupplierPaymentsGrid.ItemsSource = null;
                    ShowMessage(result.Message, true);
                    return;
                }

                SupplierPaymentsGrid.ItemsSource = result.Data ?? new List<SupplierPayment>();
            }
            catch (Exception ex)
            {
                SupplierPaymentsGrid.ItemsSource = null;
                ShowMessage($"Ödəniş tarixçəsi yüklənmədi: {ex.Message}", true);
            }
        }

        private async void CreatePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedSupplierId == null)
                {
                    ShowMessage("Ödəniş etmək üçün əvvəl təchizatçı seçin.", true);
                    return;
                }

                if (!decimal.TryParse(PaymentAmountText.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
                {
                    if (!decimal.TryParse(PaymentAmountText.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                    {
                        ShowMessage("Ödəniş məbləği düzgün yazılmayıb.", true);
                        return;
                    }
                }

                if (amount <= 0)
                {
                    ShowMessage("Ödəniş məbləği 0-dan böyük olmalıdır.", true);
                    return;
                }

                var paymentType = PaymentCreateTypeCombo.SelectedItem is PaymentType selectedPaymentType
                    ? selectedPaymentType
                    : PaymentType.Cash;

                var paymentDate = PaymentDatePicker.SelectedDate ?? DateTime.Now;

                var result = await _supplierPaymentService.CreatePaymentAsync(
                    supplierId: _selectedSupplierId.Value,
                    amount: amount,
                    paymentType: paymentType,
                    paymentDate: paymentDate,
                    note: ToNull(PaymentNoteText.Text));

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                PaymentAmountText.Text = "";
                PaymentNoteText.Text = "";
                PaymentDatePicker.SelectedDate = DateTime.Now;

                await LoadSuppliersAsync();

                var selectedSupplier = _suppliers.FirstOrDefault(x => x.Id == _selectedSupplierId.Value);
                if (selectedSupplier != null)
                {
                    SelectedSupplierDebtText.Text = $"{selectedSupplier.DebtAmount:N2} AZN";
                    SuppliersGrid.SelectedItem = selectedSupplier;
                }

                await LoadSupplierPaymentsAsync(_selectedSupplierId.Value);

                ShowMessage("Təchizatçı ödənişi qeydə alındı və borc azaldıldı.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Ödəniş qeydə alınmadı: {ex.Message}", true);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = NameText.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowMessage("Təchizatçı adı boş ola bilməz.", true);
                    return;
                }

                var currency = CurrencyCombo.SelectedItem is CurrencyType selectedCurrency
                    ? selectedCurrency
                    : CurrencyType.AZN;

                var paymentType = PaymentTypeCombo.SelectedItem is PaymentType selectedPaymentType
                    ? selectedPaymentType
                    : PaymentType.Cash;

                if (_selectedSupplierId == null)
                {
                    var createResult = await _supplierService.CreateAsync(
                        name: name,
                        companyName: ToNull(CompanyNameText.Text),
                        phone: ToNull(PhoneText.Text),
                        email: ToNull(EmailText.Text),
                        address: ToNull(AddressText.Text),
                        voen: ToNull(VoenText.Text),
                        bankName: ToNull(BankNameText.Text),
                        bankAccount: ToNull(BankAccountText.Text),
                        currency: currency,
                        paymentType: paymentType,
                        note: ToNull(NoteText.Text));

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Təchizatçı yaradıldı.", false);
                }
                else
                {
                    var updateResult = await _supplierService.UpdateAsync(
                        id: _selectedSupplierId.Value,
                        name: name,
                        companyName: ToNull(CompanyNameText.Text),
                        phone: ToNull(PhoneText.Text),
                        email: ToNull(EmailText.Text),
                        address: ToNull(AddressText.Text),
                        voen: ToNull(VoenText.Text),
                        bankName: ToNull(BankNameText.Text),
                        bankAccount: ToNull(BankAccountText.Text),
                        currency: currency,
                        paymentType: paymentType,
                        note: ToNull(NoteText.Text));

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Təchizatçı yeniləndi.", false);
                }

                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private async void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedSupplierId == null)
                {
                    ShowMessage("Passiv etmək üçün təchizatçı seçin.", true);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Bu təchizatçı passiv edilsin?",
                    "Təsdiq",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await _supplierService.DeactivateAsync(_selectedSupplierId.Value);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                await LoadSuppliersAsync();
                ClearForm();
                ShowMessage("Təchizatçı passiv edildi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _isClearingForm = true;

            _selectedSupplierId = null;
            SuppliersGrid.SelectedItem = null;

            FormTitleText.Text = "Yeni təchizatçı";
            SaveButton.Content = "Yadda saxla";
            DeactivateButton.IsEnabled = false;

            NameText.Text = "";
            CompanyNameText.Text = "";
            PhoneText.Text = "";
            EmailText.Text = "";
            AddressText.Text = "";
            VoenText.Text = "";
            BankNameText.Text = "";
            BankAccountText.Text = "";
            CurrencyCombo.SelectedItem = CurrencyType.AZN;
            PaymentTypeCombo.SelectedItem = PaymentType.Cash;
            NoteText.Text = "";

            // YENI:
            // Payment panel təmizlənir.
            SelectedSupplierDebtText.Text = "0.00 AZN";
            PaymentAmountText.Text = "";
            PaymentDatePicker.SelectedDate = DateTime.Now;
            PaymentCreateTypeCombo.SelectedItem = PaymentType.Cash;
            PaymentNoteText.Text = "";
            SupplierPaymentsGrid.ItemsSource = null;

            ShowMessage("Yeni təchizatçı əlavə edə bilərsiniz.", false);

            _isClearingForm = false;
        }

        private string? ToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError
                ? Brushes.Firebrick
                : Brushes.SeaGreen;
        }
    }
}