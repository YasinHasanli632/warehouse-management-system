using Anbar.Data;
using Anbar.Services;
using Anbar.Views.Settings.Sections;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Anbar.Views.Settings
{
    public partial class SettingsView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;

        private SettingsUiModel? _model;
        private ISettingsSection? _activeSection;

        public SettingsView()
        {
            InitializeComponent();

            // YENI:
            // Mövcud strukturuna uyğun hələlik connection string burada saxlanılır.
            // Sonra AppDbContextFactory və ya centralized config-ə keçirəcəyik.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new AppDbContext(options);
            _settingsService = new SettingsService(_context);

            Loaded += SettingsView_Loaded;
            Unloaded += SettingsView_Unloaded;
        }

        private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettingsAsync();
        }

        private void SettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            _context.Dispose();
        }

        private async System.Threading.Tasks.Task LoadSettingsAsync()
        {
            try
            {
                ShowMessage("Ayarlar yüklənir...", false);

                var result = await _settingsService.GetAllSettingsForUiAsync();

                if (!result.IsSuccess || result.Data == null)
                {
                    ShowMessage(result.Message, true);
                    return;
                }

                _model = result.Data;

                OpenGeneral();

                ShowMessage("Ayarlar yükləndi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Ayarlar yüklənmədi: {ex.Message}", true);
                MessageBox.Show(ex.Message, "Settings xətası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetActiveButton(Button selectedButton)
        {
            ResetButton(GeneralBtn);
            ResetButton(InvoiceBtn);
            ResetButton(StockBtn);
            ResetButton(CostBtn);
            ResetButton(LocalBtn);
            ResetButton(ImportBtn);
            ResetButton(TaxBtn);
            ResetButton(NumberingBtn);
            ResetButton(PaymentBtn);
            ResetButton(DocumentBtn);
            ResetButton(PermissionBtn);
            ResetButton(SystemBtn);

            selectedButton.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            selectedButton.Foreground = Brushes.White;
            selectedButton.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        }

        private static void ResetButton(Button button)
        {
            button.Background = Brushes.White;
            button.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        }

        private void SetSection(UserControl section, Button selectedButton)
        {
            if (_model == null)
            {
                ShowMessage("Settings modeli hələ yüklənməyib.", true);
                return;
            }

            // YENI:
            // Aktiv section-dan çıxanda onun dəyişikliklərini modelə yazırıq.
            _activeSection?.ApplyChanges();

            SettingsContent.Content = section;

            if (section is ISettingsSection settingsSection)
            {
                _activeSection = settingsSection;
                settingsSection.Bind();
            }
            else
            {
                _activeSection = null;
            }

            SetActiveButton(selectedButton);
        }

        private void OpenGeneral()
        {
            if (_model == null)
                return;

            SetSection(new GeneralSettingsSection(_model), GeneralBtn);
        }

        private void OpenInvoice()
        {
            if (_model == null)
                return;

            SetSection(new InvoiceSettingsSection(_model), InvoiceBtn);
        }

        private void OpenStock()
        {
            if (_model == null)
                return;

            SetSection(new StockSettingsSection(_model), StockBtn);
        }

        private void OpenCost()
        {
            if (_model == null)
                return;

            SetSection(new CostSettingsSection(_model), CostBtn);
        }

        private void OpenLocal()
        {
            if (_model == null)
                return;

            SetSection(new LocalPurchaseSettingsSection(_model), LocalBtn);
        }

        private void OpenImport()
        {
            if (_model == null)
                return;

            SetSection(new ImportSettingsSection(_model), ImportBtn);
        }

        private void OpenTax()
        {
            if (_model == null)
                return;

            SetSection(new TaxSettingsSection(_model), TaxBtn);
        }

        private void OpenNumbering()
        {
            if (_model == null)
                return;

            SetSection(new NumberingSettingsSection(_model), NumberingBtn);
        }

        private void OpenPayment()
        {
            if (_model == null)
                return;

            SetSection(new PaymentSettingsSection(_model), PaymentBtn);
        }

        private void OpenDocument()
        {
            if (_model == null)
                return;

            SetSection(new DocumentSettingsSection(_model), DocumentBtn);
        }

        private void OpenPermission()
        {
            if (_model == null)
                return;

            SetSection(new PermissionSettingsSection(_model), PermissionBtn);
        }

        private void OpenSystem()
        {
            if (_model == null)
                return;

            SetSection(new SystemSettingsSection(_model), SystemBtn);
        }

        private async void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_model == null)
                {
                    ShowMessage("Settings modeli yüklənməyib.", true);
                    return;
                }

                // YENI:
                // Hazırda açıq olan section-un son dəyişikliklərini modelə yazırıq.
                _activeSection?.ApplyChanges();

                ShowMessage("Ayarlar yadda saxlanılır...", false);

                var result = await _settingsService.SaveAllSettingsAsync(_model);

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    MessageBox.Show(result.Message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowMessage("Ayarlar uğurla yadda saxlanıldı.", false);
                MessageBox.Show("Ayarlar uğurla yadda saxlanıldı.", "Uğurlu", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadSettingsAsync();
            }
            catch (Exception ex)
            {
                ShowMessage($"Yadda saxlanılmadı: {ex.Message}", true);
                MessageBox.Show(ex.Message, "Settings save xətası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show(
                    "Default ayarlar bərpa edilsin?",
                    "Təsdiq",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                ShowMessage("Default ayarlar bərpa edilir...", false);

                var result = await _settingsService.ResetToDefaultAsync();

                if (!result.IsSuccess)
                {
                    ShowMessage(result.Message, true);
                    MessageBox.Show(result.Message, "Diqqət", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LoadSettingsAsync();

                ShowMessage("Default ayarlar bərpa edildi.", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Default bərpa xətası: {ex.Message}", true);
                MessageBox.Show(ex.Message, "Settings reset xətası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError ? Brushes.Firebrick : Brushes.SeaGreen;
        }

        private void General_Click(object sender, RoutedEventArgs e) => OpenGeneral();
        private void Invoice_Click(object sender, RoutedEventArgs e) => OpenInvoice();
        private void Stock_Click(object sender, RoutedEventArgs e) => OpenStock();
        private void Cost_Click(object sender, RoutedEventArgs e) => OpenCost();
        private void Local_Click(object sender, RoutedEventArgs e) => OpenLocal();
        private void Import_Click(object sender, RoutedEventArgs e) => OpenImport();
        private void Tax_Click(object sender, RoutedEventArgs e) => OpenTax();
        private void Numbering_Click(object sender, RoutedEventArgs e) => OpenNumbering();
        private void Payment_Click(object sender, RoutedEventArgs e) => OpenPayment();
        private void Document_Click(object sender, RoutedEventArgs e) => OpenDocument();
        private void Permission_Click(object sender, RoutedEventArgs e) => OpenPermission();
        private void System_Click(object sender, RoutedEventArgs e) => OpenSystem();
    }
}