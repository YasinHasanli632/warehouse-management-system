using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Services
{
    // Bu servis Dashboard səhifəsi üçün statistik məlumatları hazırlayır.
    // UI-də görünən kartlar, son hərəkətlər, kritik stok və stok xülasəsi buradan gələcək.
    public class DashboardService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;

        // StockService rəf doluluqlarını yeniləmək üçün istifadə olunur.
        public DashboardService(AppDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        // Dashboard-un əsas xülasəsini qaytarır.
        public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync()
        {
            // Dashboard açılmamışdan əvvəl rəf statuslarını yeniləyirik.
            var shelfIds = await _context.Shelves
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var shelfId in shelfIds)
            {
                await _stockService.UpdateShelfStatusAsync(shelfId);
            }

            var totalProducts = await _context.Products
                .CountAsync(x => x.IsActive);

            var totalCategories = await _context.Categories
                .CountAsync(x => x.IsActive);

            var totalShelves = await _context.Shelves
                .CountAsync(x => x.IsActive);

            var emptyShelves = await _context.Shelves
                .CountAsync(x => x.IsActive && x.Status == ShelfStatus.Empty);

            var lowShelves = await _context.Shelves
                .CountAsync(x => x.IsActive && x.Status == ShelfStatus.Low);

            var fullShelves = await _context.Shelves
                .CountAsync(x => x.IsActive && x.Status == ShelfStatus.Full);

            var averageOccupancy = await _context.Shelves
      .Where(x => x.IsActive)
      .AverageAsync(x => (decimal?)x.OccupancyPercent) ?? 0;

            var totalSuppliers = await _context.Suppliers
                .CountAsync(x => x.IsActive);

            var totalCustomers = await _context.Customers
                .CountAsync(x => x.IsActive);

            var draftInvoices = await _context.Invoices
                .CountAsync(x => x.IsActive && x.Status == InvoiceStatus.Draft);

            var confirmedInvoices = await _context.Invoices
                .CountAsync(x => x.IsActive && x.Status == InvoiceStatus.Confirmed);

            var totalStockQuantity = await _context.ShelfStocks
     .Where(x => x.IsActive)
     .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            var criticalStocksResult = await GetCriticalStocksAsync();
            var criticalStockCount = criticalStocksResult.Data?.Count ?? 0;

            var summary = new DashboardSummaryDto
            {
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalShelves = totalShelves,
                EmptyShelves = emptyShelves,
                LowShelves = lowShelves,
                FullShelves = fullShelves,
                AverageOccupancyPercent = Math.Round(averageOccupancy, 2),
                TotalSuppliers = totalSuppliers,
                TotalCustomers = totalCustomers,
                DraftInvoices = draftInvoices,
                ConfirmedInvoices = confirmedInvoices,
                TotalStockQuantity = totalStockQuantity,
                CriticalStockCount = criticalStockCount
            };

            return Result<DashboardSummaryDto>.Success(summary, "Dashboard xülasəsi yükləndi.");
        }

        // Son stok hərəkətlərini gətirir.
        // Dashboard-da “Son əməliyyatlar” hissəsi üçün istifadə olunur.
        public async Task<Result<List<StockMovement>>> GetRecentMovementsAsync(int count = 10)
        {
            if (count <= 0)
            {
                count = 10;
            }

            var movements = await _context.StockMovements
                .Include(x => x.Product)
                .Include(x => x.Invoice)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Result<List<StockMovement>>.Success(movements, "Son hərəkətlər yükləndi.");
        }

        // Məhsullar üzrə ümumi stok xülasəsini qaytarır.
        public async Task<Result<List<ProductStockSummaryDto>>> GetStockSummaryAsync()
        {
            var products = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Shelf)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var result = products.Select(product =>
            {
                var totalQuantity = product.ShelfStocks
                    .Where(x => x.IsActive)
                    .Sum(x => x.Quantity);

                var shelfCount = product.ShelfStocks
                    .Where(x => x.IsActive && x.Quantity > 0)
                    .Select(x => x.ShelfId)
                    .Distinct()
                    .Count();

                return new ProductStockSummaryDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code,
                    CategoryName = product.Category?.Name ?? "",
                    Unit = product.Unit,
                    TotalQuantity = totalQuantity,
                    MinStockQuantity = product.MinStockQuantity,
                    ShelfCount = shelfCount,
                    IsCritical = totalQuantity <= product.MinStockQuantity
                };
            }).ToList();

            return Result<List<ProductStockSummaryDto>>.Success(result, "Stok xülasəsi yükləndi.");
        }

        // Kritik stokda olan məhsulları qaytarır.
        // Məhsulun ümumi rəf qalığı minimum stok limitindən azdırsa kritik sayılır.
        public async Task<Result<List<ProductStockSummaryDto>>> GetCriticalStocksAsync()
        {
            var stockSummaryResult = await GetStockSummaryAsync();

            if (!stockSummaryResult.IsSuccess || stockSummaryResult.Data == null)
            {
                return Result<List<ProductStockSummaryDto>>.Fail("Kritik stok məlumatları yüklənə bilmədi.");
            }

            var criticalStocks = stockSummaryResult.Data
                .Where(x => x.IsCritical)
                .OrderBy(x => x.TotalQuantity)
                .ToList();

            return Result<List<ProductStockSummaryDto>>.Success(criticalStocks, "Kritik stoklar yükləndi.");
        }
    }

    // Dashboard kartlarında göstəriləcək əsas statistik model.
    public class DashboardSummaryDto
    {
        // Aktiv məhsul sayı.
        public int TotalProducts { get; set; }

        // Aktiv kateqoriya sayı.
        public int TotalCategories { get; set; }

        // Aktiv rəf sayı.
        public int TotalShelves { get; set; }

        // Boş rəf sayı.
        public int EmptyShelves { get; set; }

        // Az dolu rəf sayı.
        public int LowShelves { get; set; }

        // Tam dolu rəf sayı.
        public int FullShelves { get; set; }

        // Rəflərin orta doluluq faizi.
        public decimal AverageOccupancyPercent { get; set; }

        // Təchizatçı sayı.
        public int TotalSuppliers { get; set; }

        // Müştəri sayı.
        public int TotalCustomers { get; set; }

        // Draft qaimə sayı.
        public int DraftInvoices { get; set; }

        // Təsdiqlənmiş qaimə sayı.
        public int ConfirmedInvoices { get; set; }

        // Bütün rəflərdə ümumi stok miqdarı.
        public decimal TotalStockQuantity { get; set; }

        // Kritik stokda olan məhsul sayı.
        public int CriticalStockCount { get; set; }
    }

    // Məhsul üzrə stok xülasəsi üçün model.
    public class ProductStockSummaryDto
    {
        // Məhsul ID-si.
        public int ProductId { get; set; }

        // Məhsul adı.
        public string ProductName { get; set; } = string.Empty;

        // Məhsul kodu.
        public string ProductCode { get; set; } = string.Empty;

        // Kateqoriya adı.
        public string CategoryName { get; set; } = string.Empty;

        // Ölçü vahidi.
        public string Unit { get; set; } = string.Empty;

        // Məhsulun bütün rəflərdəki ümumi miqdarı.
        public decimal TotalQuantity { get; set; }

        // Məhsulun minimum stok limiti.
        public decimal MinStockQuantity { get; set; }

        // Məhsul neçə rəfdə yerləşir.
        public int ShelfCount { get; set; }

        // Kritik stokdadırmı.
        public bool IsCritical { get; set; }
    }
}
