using Anbar.Data;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Anbar.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DashboardService _dashboardService;

        public DashboardView()
        {
            InitializeComponent();

            // YENI: Real database bağlantısı.
            // Burada öz SQL Server adını yaz.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var context = new AppDbContext(options);

            var stockService = new StockService(context);
            _dashboardService = new DashboardService(context, stockService);

            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var summaryResult = await _dashboardService.GetDashboardSummaryAsync();

                if (summaryResult.IsSuccess && summaryResult.Data != null)
                {
                    TotalProductsText.Text = summaryResult.Data.TotalProducts.ToString();
                    TotalShelvesText.Text = summaryResult.Data.TotalShelves.ToString();
                    OccupancyText.Text = $"{summaryResult.Data.AverageOccupancyPercent}%";
                    CriticalStockText.Text = summaryResult.Data.CriticalStockCount.ToString();
                }

                var movementsResult = await _dashboardService.GetRecentMovementsAsync(10);
                if (movementsResult.IsSuccess)
                {
                    RecentMovementsGrid.ItemsSource = movementsResult.Data;
                }

                var criticalResult = await _dashboardService.GetCriticalStocksAsync();
                if (criticalResult.IsSuccess)
                {
                    CriticalStocksGrid.ItemsSource = criticalResult.Data;
                }

                var stockResult = await _dashboardService.GetStockSummaryAsync();
                if (stockResult.IsSuccess)
                {
                    StockSummaryGrid.ItemsSource = stockResult.Data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Dashboard məlumatları yüklənmədi:\n{ex.Message}",
                    "Xəta",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}