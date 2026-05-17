using Anbar.Views;
using Anbar.Views.Settings;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Anbar
{
    public partial class MainWindow : Window
    {
        private bool _isSidebarOpen = true;
        private bool _isInvoicesOpen = false;

        private readonly List<Button> _navButtons;

        public MainWindow()
        {
            InitializeComponent();

            _navButtons = new List<Button>
            {
                DashboardButton,
                InvoicesButton,

                // YENI:
                // Qaimə alt menyuları da active/foreground sisteminə daxil edilir.
                AllInvoicesButton,
                InputInvoicesButton,
                OutputInvoicesButton,

                WarehouseMapButton,
                WarehousesButton,
                ShelvesButton,
                StockInButton,
                StockOutButton,
                TransferButton,
                ProductsButton,
                CategoriesButton,
                SuppliersButton,
                CustomersButton,
                ReportsButton,
                SettingsButton
            };

            CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            NavigateTo(
                new DashboardView(),
                "Dashboard",
                "Anbar ERP / WMS ümumi idarəetmə paneli",
                DashboardButton);

            // YENI:
            // Sistem ilk açılanda sidebar text/icon-ları qara qalmasın.
            NormalizeSidebarColors();
        }

        private void NavigateTo(UserControl view, string title, string subtitle, Button? activeButton = null)
        {
            MainContent.Content = view;

            PageTitleText.Text = title;
            PageSubtitleText.Text = subtitle;
            ActiveModuleText.Text = title;

            SetActiveButton(activeButton);
        }

        private void GlobalSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // YENI:
            // Global search sonradan view-lara bağlanacaq.

            var searchText = GlobalSearchBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
                return;
        }
        public void OpenView(UserControl view, string title, string subtitle, Button? activeButton = null)
        {
            NavigateTo(view, title, subtitle, activeButton);
        }
        private void SetActiveButton(Button? activeButton)
        {
            var normalBrush = new SolidColorBrush(Color.FromRgb(248, 250, 252));

            foreach (var button in _navButtons)
            {
                button.Background = Brushes.Transparent;
                button.Foreground = normalBrush;
                button.BorderBrush = Brushes.Transparent;

                SetChildrenForeground(button, normalBrush);
            }

            if (activeButton == null)
                return;

            activeButton.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            activeButton.Foreground = Brushes.White;
            activeButton.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));

            SetChildrenForeground(activeButton, Brushes.White);
        }

        private void NormalizeSidebarColors()
        {
            // YENI:
            // Açılışda bütün sidebar düymələrinin içindəki TextBlock-ları oxunaqlı rəngə çəkirik.
            var normalBrush = new SolidColorBrush(Color.FromRgb(248, 250, 252));

            foreach (var button in _navButtons)
            {
                SetChildrenForeground(button, normalBrush);
            }

            SetChildrenForeground(LogoutButton, normalBrush);

            // Dashboard aktiv açılır.
            SetChildrenForeground(DashboardButton, Brushes.White);
        }

        private void SetChildrenForeground(DependencyObject parent, Brush brush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBlock textBlock)
                    textBlock.Foreground = brush;

                SetChildrenForeground(child, brush);
            }
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarOpen = !_isSidebarOpen;

            if (_isSidebarOpen)
            {
                SidebarColumn.Width = new GridLength(274);

                LogoText.Visibility = Visibility.Visible;
                LogoSubText.Visibility = Visibility.Visible;

                ShowMenuText();

                AdminBox.Visibility = Visibility.Visible;
                SidebarToggleIcon.Text = "←";
            }
            else
            {
                SidebarColumn.Width = new GridLength(78);

                LogoText.Visibility = Visibility.Collapsed;
                LogoSubText.Visibility = Visibility.Collapsed;

                HideMenuText();

                AdminBox.Visibility = Visibility.Collapsed;

                InvoicesSubMenu.Visibility = Visibility.Collapsed;
                _isInvoicesOpen = false;
                InvoicesArrow.Text = "\uE70D";

                SidebarToggleIcon.Text = "→";
            }
        }

        private void ShowMenuText()
        {
            SectionMainText.Visibility = Visibility.Visible;
            SectionWarehouseText.Visibility = Visibility.Visible;
            SectionMasterText.Visibility = Visibility.Visible;
            SectionAnalysisText.Visibility = Visibility.Visible;

            DashboardText.Visibility = Visibility.Visible;
            InvoicesText.Visibility = Visibility.Visible;
            InvoicesArrow.Visibility = Visibility.Visible;
            AllInvoicesText.Visibility = Visibility.Visible;
            InputInvoicesText.Visibility = Visibility.Visible;
            OutputInvoicesText.Visibility = Visibility.Visible;

            WarehouseMapText.Visibility = Visibility.Visible;
            WarehousesText.Visibility = Visibility.Visible;
            ShelvesText.Visibility = Visibility.Visible;
            StockInText.Visibility = Visibility.Visible;
            StockOutText.Visibility = Visibility.Visible;
            TransferText.Visibility = Visibility.Visible;

            ProductsText.Visibility = Visibility.Visible;
            CategoriesText.Visibility = Visibility.Visible;
            SuppliersText.Visibility = Visibility.Visible;
            CustomersText.Visibility = Visibility.Visible;

            ReportsText.Visibility = Visibility.Visible;
            SettingsText.Visibility = Visibility.Visible;
            LogoutText.Visibility = Visibility.Visible;

            NormalizeSidebarColors();
        }

        private void HideMenuText()
        {
            SectionMainText.Visibility = Visibility.Collapsed;
            SectionWarehouseText.Visibility = Visibility.Collapsed;
            SectionMasterText.Visibility = Visibility.Collapsed;
            SectionAnalysisText.Visibility = Visibility.Collapsed;

            DashboardText.Visibility = Visibility.Collapsed;
            InvoicesText.Visibility = Visibility.Collapsed;
            InvoicesArrow.Visibility = Visibility.Collapsed;
            AllInvoicesText.Visibility = Visibility.Collapsed;
            InputInvoicesText.Visibility = Visibility.Collapsed;
            OutputInvoicesText.Visibility = Visibility.Collapsed;

            WarehouseMapText.Visibility = Visibility.Collapsed;
            WarehousesText.Visibility = Visibility.Collapsed;
            ShelvesText.Visibility = Visibility.Collapsed;
            StockInText.Visibility = Visibility.Collapsed;
            StockOutText.Visibility = Visibility.Collapsed;
            TransferText.Visibility = Visibility.Collapsed;

            ProductsText.Visibility = Visibility.Collapsed;
            CategoriesText.Visibility = Visibility.Collapsed;
            SuppliersText.Visibility = Visibility.Collapsed;
            CustomersText.Visibility = Visibility.Collapsed;

            ReportsText.Visibility = Visibility.Collapsed;
            SettingsText.Visibility = Visibility.Collapsed;
            LogoutText.Visibility = Visibility.Collapsed;
        }

        private void InvoicesDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSidebarOpen)
                return;

            _isInvoicesOpen = !_isInvoicesOpen;

            InvoicesSubMenu.Visibility = _isInvoicesOpen
                ? Visibility.Visible
                : Visibility.Collapsed;

            InvoicesArrow.Text = _isInvoicesOpen ? "\uE70E" : "\uE70D";

            SetActiveButton(InvoicesButton);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new DashboardView(),
                "Dashboard",
                "Bugünkü əməliyyatlar, stok vəziyyəti, borclar və kritik göstəricilər",
                DashboardButton);
        }

        private void AllInvoices_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new InvoicesView(),
                "Bütün qaimələr",
                "Giriş, çıxış, geri qaytarma və idxal qaimələrinin vahid idarə paneli",
                AllInvoicesButton);
        }

        private void InputInvoices_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new InputInvoicesView(),
                "Giriş qaimələri",
                "Təchizatçıdan mal girişi və alış qaimələri",
                InputInvoicesButton);
        }

        private void OutputInvoices_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new OutputInvoicesView(),
                "Çıxış qaimələri",
                "Müştəriyə satış, stok çıxışı və FIFO əsaslı əməliyyatlar",
                OutputInvoicesButton);
        }

        private void WarehouseMap_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new WarehouseMapView(),
                "Anbar xəritəsi",
                "Zona, sıra, rəf və doluluq vəziyyətinin vizual idarəsi",
                WarehouseMapButton);
        }

        private void Warehouses_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new WarehousesView(),
                "Anbarlar",
                "Əsas və əlavə anbarların idarə olunması",
                WarehousesButton);
        }

        private void Shelves_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new ShelvesView(),
                "Rəflər",
                "Rəf tutumu, statusu və doluluq nəzarəti",
                ShelvesButton);
        }

        private void StockIn_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new StockInView(),
                "Mal girişi",
                "Rəfə mal yerləşdirmə və stok artırma əməliyyatı",
                StockInButton);
        }

        private void StockOut_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new StockOutView(),
                "Mal çıxışı",
                "FIFO və stok qalığına əsaslanan çıxış əməliyyatı",
                StockOutButton);
        }

        private void Transfer_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new TransferView(),
                "Rəf transferi",
                "Məhsulun rəflər və anbarlar arasında hərəkəti",
                TransferButton);
        }

        private void Products_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new ProductsView(),
                "Məhsullar",
                "Məhsul kartları, barkod, vahid, atribut və vergi qaydaları",
                ProductsButton);
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new CategoriesView(),
                "Kateqoriyalar",
                "Kateqoriya, alt kateqoriya və məhsul xüsusiyyətlərinin idarəsi",
                CategoriesButton);
        }

        private void Suppliers_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new SuppliersView(),
                "Təchizatçılar",
                "Təchizatçı kartları, alış tarixçəsi və borc nəzarəti",
                SuppliersButton);
        }

        private void Customers_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new CustomersView(),
                "Müştərilər",
                "Müştəri kartları, satış tarixçəsi, limit və borc nəzarəti",
                CustomersButton);
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new ReportsView(),
                "Hesabatlar",
                "Stok, qaimə, FIFO, maya, mənfəət və borc hesabatları",
                ReportsButton);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                new SettingsView(),
                "Ayarlar",
                "Qaimə, stok, maya, vergi və idxal davranış ayarları",
                SettingsButton);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Sistemdən çıxmaq istəyirsiniz?",
                "Çıxış",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}