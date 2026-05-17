using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anbar.Services
{
    public class WarehouseService
    {
        private readonly AppDbContext _context;

        public WarehouseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<Warehouse>>> GetAllAsync()
        {
            var warehouses = await _context.Warehouses
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<Warehouse>>.Success(warehouses, "Anbarlar yükləndi.");
        }

        public async Task<Result<Warehouse>> GetByIdAsync(int id)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (warehouse == null)
                return Result<Warehouse>.Fail("Anbar tapılmadı.");

            return Result<Warehouse>.Success(warehouse);
        }

        public async Task<Result<Warehouse>> CreateAsync(string name, string code, string? address, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<Warehouse>.Fail("Anbar adı boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(code))
                return Result<Warehouse>.Fail("Anbar kodu boş ola bilməz.");

            var normalizedCode = code.Trim().ToUpper();

            var codeExists = await _context.Warehouses
                .AnyAsync(x => x.Code.ToUpper() == normalizedCode && x.IsActive);

            if (codeExists)
                return Result<Warehouse>.Fail("Bu anbar kodu artıq mövcuddur.");

            var warehouse = new Warehouse
            {
                Name = name.Trim(),
                Code = normalizedCode,
                Address = address?.Trim(),
                Description = description?.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.Warehouses.AddAsync(warehouse);
            await _context.SaveChangesAsync();

            return Result<Warehouse>.Success(warehouse, "Anbar yaradıldı.");
        }

        public async Task<Result<Warehouse>> UpdateAsync(int id, string name, string code, string? address, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<Warehouse>.Fail("Anbar adı boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(code))
                return Result<Warehouse>.Fail("Anbar kodu boş ola bilməz.");

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (warehouse == null)
                return Result<Warehouse>.Fail("Anbar tapılmadı.");

            var normalizedCode = code.Trim().ToUpper();

            var codeExists = await _context.Warehouses.AnyAsync(x =>
                x.Id != id &&
                x.Code.ToUpper() == normalizedCode &&
                x.IsActive);

            if (codeExists)
                return Result<Warehouse>.Fail("Bu anbar kodu artıq mövcuddur.");

            warehouse.Name = name.Trim();
            warehouse.Code = normalizedCode;
            warehouse.Address = address?.Trim();
            warehouse.Description = description?.Trim();
            warehouse.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<Warehouse>.Success(warehouse, "Anbar yeniləndi.");
        }

        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            var warehouse = await _context.Warehouses
                .Include(x => x.Shelves.Where(s => s.IsActive))
                    .ThenInclude(x => x.ShelfStocks.Where(ss => ss.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (warehouse == null)
                return Result<bool>.Fail("Anbar tapılmadı.");

            var hasActiveShelves = warehouse.Shelves.Any();

            if (hasActiveShelves)
                return Result<bool>.Fail("Bu anbarda aktiv rəflər var. Əvvəl rəfləri passiv edin.");

            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Anbar passiv edildi.");
        }

        public async Task<Result<Warehouse>> EnsureDefaultWarehouseAsync()
        {
            var existing = await _context.Warehouses
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
                return Result<Warehouse>.Success(existing, "Aktiv anbar mövcuddur.");

            var warehouse = new Warehouse
            {
                Name = "Əsas Anbar",
                Code = "MAIN",
                Address = "Əsas anbar",
                Description = "Sistem tərəfindən yaradılmış əsas anbar",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.Warehouses.AddAsync(warehouse);
            await _context.SaveChangesAsync();

            return Result<Warehouse>.Success(warehouse, "Əsas Anbar yaradıldı.");
        }
    }
}