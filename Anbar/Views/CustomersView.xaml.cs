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
    public partial class CustomersView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly CustomerService _customerService;

        private List<Customer> _customers = new();
        private int? _selectedCustomerId = null;
        private bool _isClearingForm = false;

        public CustomersView()
        {
            InitializeComponent();

            // YENI: Mövcud layihədəki connection string ilə eyni saxladım.
            // Əgər səndə server adı fərqlidirsə, yalnız buranı dəyiş.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _customerService = new CustomerService(_context);

            Loaded += CustomersView_Loaded;
        }

        private async void CustomersView_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareCombos();
            await LoadCustomersAsync();
            ClearForm();
        }

        private void PrepareCombos()
        {
            CurrencyCombo.ItemsSource = Enum.GetValues(typeof(CurrencyType));
            PaymentTypeCombo.ItemsSource = Enum.GetValues(typeof(PaymentType));

            CurrencyCombo.SelectedItem = CurrencyType.AZN;
            PaymentTypeCombo.SelectedItem = PaymentType.Cash;
        }

        private async System.Threading.Tasks.Task LoadCustomersAsync()
        {
            try
            {
                var result = await _customerService.GetAllAsync();

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                _customers = result.Data ?? new List<Customer>();

                ApplyFilter();
                UpdateStats();

                ShowMessage("Müştəri siyahısı yükləndi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Müştərilər yüklənmədi: {ex.Message}", true);
            }
        }

        private void ApplyFilter()
        {
            var keyword = SearchText.Text?.Trim().ToLower() ?? "";

            var filtered = _customers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(x =>
                    (x.Name ?? "").ToLower().Contains(keyword) ||
                    (x.CompanyName ?? "").ToLower().Contains(keyword) ||
                    (x.Phone ?? "").ToLower().Contains(keyword) ||
                    (x.Email ?? "").ToLower().Contains(keyword) ||
                    (x.Voen ?? "").ToLower().Contains(keyword) ||
                    (x.BankName ?? "").ToLower().Contains(keyword) ||
                    (x.Address ?? "").ToLower().Contains(keyword));
            }

            var list = filtered
                .OrderBy(x => x.Name)
                .ToList();

            CustomersGrid.ItemsSource = list;
            TotalCountText.Text = $"{list.Count} qeyd";
        }

        private void UpdateStats()
        {
            var totalCustomers = _customers.Count;
            var debtCustomers = _customers.Count(x => x.DebtAmount > 0);
            var totalDebt = _customers.Sum(x => x.DebtAmount);
            var totalCredit = _customers.Sum(x => x.CreditLimit);

            TotalCustomersText.Text = totalCustomers.ToString();
            DebtCustomersText.Text = debtCustomers.ToString();
            TotalDebtText.Text = $"{totalDebt:N2} AZN";
            TotalCreditText.Text = $"{totalCredit:N2} AZN";
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadCustomersAsync();
        }

        private void NewCustomer_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isClearingForm)
                return;

            if (CustomersGrid.SelectedItem is not Customer customer)
                return;

            _selectedCustomerId = customer.Id;

            FormTitleText.Text = "Müştərini düzəlt";
            SaveButton.Content = "Dəyişiklikləri yadda saxla";
            DeactivateButton.IsEnabled = true;

            NameText.Text = customer.Name ?? "";
            CompanyNameText.Text = customer.CompanyName ?? "";
            PhoneText.Text = customer.Phone ?? "";
            EmailText.Text = customer.Email ?? "";
            AddressText.Text = customer.Address ?? "";
            VoenText.Text = customer.Voen ?? "";
            BankNameText.Text = customer.BankName ?? "";
            BankAccountText.Text = customer.BankAccount ?? "";
            CurrencyCombo.SelectedItem = customer.Currency;
            PaymentTypeCombo.SelectedItem = customer.PaymentType;
            CreditLimitText.Text = customer.CreditLimit.ToString("0.##", CultureInfo.InvariantCulture);
            DebtAmountText.Text = $"{customer.DebtAmount:N2} AZN";
            NoteText.Text = customer.Note ?? "";

            ShowMessage("Müştəri seçildi. Düzəliş edə bilərsiniz.", false);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = NameText.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowMessage("Müştəri adı boş ola bilməz.", true);
                    return;
                }

                var creditLimit = ParseDecimal(CreditLimitText.Text);

                if (creditLimit < 0)
                {
                    ShowMessage("Kredit limiti mənfi ola bilməz.", true);
                    return;
                }

                var currency = CurrencyCombo.SelectedItem is CurrencyType selectedCurrency
                    ? selectedCurrency
                    : CurrencyType.AZN;

                var paymentType = PaymentTypeCombo.SelectedItem is PaymentType selectedPaymentType
                    ? selectedPaymentType
                    : PaymentType.Cash;

                if (_selectedCustomerId == null)
                {
                    var createResult = await _customerService.CreateAsync(
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
                        creditLimit: creditLimit,
                        note: ToNull(NoteText.Text));

                    if (!createResult.IsSuccess)
                    {
                        ShowMessage(createResult.Message, true);
                        return;
                    }

                    ShowMessage("Müştəri yaradıldı.", false);
                }
                else
                {
                    var updateResult = await _customerService.UpdateAsync(
                        id: _selectedCustomerId.Value,
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
                        creditLimit: creditLimit,
                        note: ToNull(NoteText.Text));

                    if (!updateResult.IsSuccess)
                    {
                        ShowMessage(updateResult.Message, true);
                        return;
                    }

                    ShowMessage("Müştəri yeniləndi.", false);
                }

                await LoadCustomersAsync();
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
                if (_selectedCustomerId == null)
                {
                    ShowMessage("Passiv etmək üçün müştəri seçin.", true);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Bu müştəri passiv edilsin?",
                    "Təsdiq",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var result = await _customerService.DeactivateAsync(_selectedCustomerId.Value);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                await LoadCustomersAsync();
                ClearForm();

                ShowMessage("Müştəri passiv edildi.", false);
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

            _selectedCustomerId = null;
            CustomersGrid.SelectedItem = null;

            FormTitleText.Text = "Yeni müştəri";
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
            CreditLimitText.Text = "0";
            DebtAmountText.Text = "0.00 AZN";
            NoteText.Text = "";

            ShowMessage("Yeni müştəri əlavə edə bilərsiniz.", false);

            _isClearingForm = false;
        }

        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Trim().Replace(",", ".");

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0;
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