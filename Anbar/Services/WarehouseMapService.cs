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
    // Bu servis anbar xəritəsi üçün data hazırlayır.
    // WPF UI-də A-01, A-02 kimi rəf kartları bu servisdən gələn dataya görə göstəriləcək.
    public class WarehouseMapService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;

        // StockService burada rəf statuslarını yeniləmək üçün istifadə olunur.
        public WarehouseMapService(AppDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        // Bütün anbar xəritəsini qaytarır.
        // Rəfləri Zone üzrə qruplaşdırır: A, B, C və s.
        public async Task<Result<Dictionary<string, List<Shelf>>>> GetWarehouseMapAsync()
        {
            // Əvvəl bütün aktiv rəflərin statusunu yeniləyirik ki,
            // UI-də doluluq və rənglər düzgün görünsün.
            var shelfIds = await _context.Shelves
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var shelfId in shelfIds)
            {
                await _stockService.UpdateShelfStatusAsync(shelfId);
            }

            // Rəfləri məhsul qalıqları ilə birlikdə gətiririk.
            var shelves = await _context.Shelves
                .Include(x => x.Warehouse)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Zone)
                .ThenBy(x => x.RowNumber)
                .ToListAsync();

            // UI üçün zonalara görə qruplaşdırırıq.
            var groupedShelves = shelves
                .GroupBy(x => x.Zone)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );

            return Result<Dictionary<string, List<Shelf>>>.Success(groupedShelves, "Anbar xəritəsi yükləndi.");
        }

        // ID ilə rəf detalını qaytarır.
        // Rəfə klik edəndə sağ paneldə göstəriləcək data buradan gəlir.
        public async Task<Result<Shelf>> GetShelfDetailAsync(int shelfId)
        {
            if (shelfId <= 0)
            {
                return Result<Shelf>.Fail("Rəf düzgün seçilməyib.");
            }

            // Detal açılmazdan əvvəl statusu yeniləyirik.
            var statusResult = await _stockService.UpdateShelfStatusAsync(shelfId);

            if (!statusResult.IsSuccess)
            {
                return Result<Shelf>.Fail(statusResult.Message);
            }

            var shelf = await _context.Shelves
                .Include(x => x.Warehouse)
                .Include(x => x.ShelfStocks.Where(s => s.IsActive))
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == shelfId && x.IsActive);

            if (shelf == null)
            {
                return Result<Shelf>.Fail("Rəf tapılmadı.");
            }

            return Result<Shelf>.Success(shelf, "Rəf detalları yükləndi.");
        }

        // Rəf kodu ilə detal qaytarır.
        // Məsələn UI-də A-01 axtarışı üçün istifadə edilə bilər.
        public async Task<Result<Shelf>> GetShelfDetailByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Result<Shelf>.Fail("Rəf kodu boş ola bilməz.");
            }

            var normalizedCode = code.Trim().ToUpper();

            var shelfId = await _context.Shelves
                .Where(x => x.IsActive && x.Code.ToUpper() == normalizedCode)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (shelfId <= 0)
            {
                return Result<Shelf>.Fail("Rəf tapılmadı.");
            }

            return await GetShelfDetailAsync(shelfId);
        }

        // Seçilmiş rəfdə olan məhsulları qaytarır.
        // Sağ paneldə "Rəfdəki məhsullar" listi üçün istifadə olunur.
        public async Task<Result<List<ShelfStock>>> GetShelfProductsAsync(int shelfId)
        {
            if (shelfId <= 0)
            {
                return Result<List<ShelfStock>>.Fail("Rəf düzgün seçilməyib.");
            }

            var shelfExists = await _context.Shelves
                .AnyAsync(x => x.Id == shelfId && x.IsActive);

            if (!shelfExists)
            {
                return Result<List<ShelfStock>>.Fail("Rəf tapılmadı.");
            }

            var products = await _context.ShelfStocks
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Shelf)
                .Where(x => x.ShelfId == shelfId && x.IsActive && x.Quantity > 0)
                .OrderBy(x => x.Product.Name)
                .ToListAsync();

            return Result<List<ShelfStock>>.Success(products, "Rəfdəki məhsullar yükləndi.");
        }

        // Rəfin UI-də hansı rənglə görünəcəyini qaytarır.
        // WPF tərəfdə statusa görə rəng vermək üçün istifadə edə bilərsən.
        public string GetShelfColor(ShelfStatus status)
        {
            return status switch
            {
                ShelfStatus.Empty => "#E5E7EB",   // Boş rəf - boz
                ShelfStatus.Low => "#FBBF24",     // Az dolu - sarı
                ShelfStatus.Normal => "#22C55E",  // Normal - yaşıl
                ShelfStatus.Full => "#EF4444",    // Dolu - qırmızı
                _ => "#E5E7EB"
            };
        }

        // Rəfin status adını Azərbaycan dilində qaytarır.
        // UI-də status text kimi göstərmək üçün istifadə olunur.
        public string GetShelfStatusText(ShelfStatus status)
        {
            return status switch
            {
                ShelfStatus.Empty => "Boş",
                ShelfStatus.Low => "Az dolu",
                ShelfStatus.Normal => "Normal",
                ShelfStatus.Full => "Dolu",
                _ => "Naməlum"
            };
        }
    }

}
