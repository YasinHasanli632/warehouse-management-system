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
    // Bu servis təchizatçı məlumatlarını idarə edir.
    // Təchizatçı giriş qaiməsində istifadə olunur.
    public class SupplierService
    {
        private readonly AppDbContext _context;

        public SupplierService(AppDbContext context)
        {
            _context = context;
        }

        // Bütün aktiv təchizatçıları gətirir.
        public async Task<Result<List<Supplier>>> GetAllAsync()
        {
            var suppliers = await _context.Suppliers
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<Supplier>>.Success(suppliers);
        }

        // ID-yə görə təchizatçı detalını gətirir.
        public async Task<Result<Supplier>> GetByIdAsync(int id)
        {
            var supplier = await _context.Suppliers
                .Include(x => x.Invoices.Where(i => i.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (supplier == null)
            {
                return Result<Supplier>.Fail("Təchizatçı tapılmadı.");
            }

            return Result<Supplier>.Success(supplier);
        }

        // Yeni təchizatçı yaradır.
        public async Task<Result<Supplier>> CreateAsync(
            string name,
            string? companyName = null,
            string? phone = null,
            string? email = null,
            string? address = null,
            string? voen = null,
            string? bankName = null,
            string? bankAccount = null,
            CurrencyType currency = CurrencyType.AZN,
            PaymentType paymentType = PaymentType.Cash,
            string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Supplier>.Fail("Təchizatçı adı boş ola bilməz.");
            }

            var normalizedName = name.Trim();

            var exists = await _context.Suppliers.AnyAsync(x =>
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower());

            if (exists)
            {
                return Result<Supplier>.Fail("Bu adda təchizatçı artıq mövcuddur.");
            }

            var supplier = new Supplier
            {
                Name = normalizedName,
                CompanyName = companyName,
                Phone = phone,
                Email = email,
                Address = address,
                Voen = voen,
                BankName = bankName,
                BankAccount = bankAccount,
                Currency = currency,
                PaymentType = paymentType,
                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.Suppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();

            return Result<Supplier>.Success(supplier, "Təchizatçı uğurla yaradıldı.");
        }

        // Təchizatçı məlumatlarını yeniləyir.
        public async Task<Result<Supplier>> UpdateAsync(
            int id,
            string name,
            string? companyName = null,
            string? phone = null,
            string? email = null,
            string? address = null,
            string? voen = null,
            string? bankName = null,
            string? bankAccount = null,
            CurrencyType currency = CurrencyType.AZN,
            PaymentType paymentType = PaymentType.Cash,
            string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Supplier>.Fail("Təchizatçı adı boş ola bilməz.");
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (supplier == null)
            {
                return Result<Supplier>.Fail("Təchizatçı tapılmadı.");
            }

            var normalizedName = name.Trim();

            var exists = await _context.Suppliers.AnyAsync(x =>
                x.Id != id &&
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower());

            if (exists)
            {
                return Result<Supplier>.Fail("Bu adda təchizatçı artıq mövcuddur.");
            }

            supplier.Name = normalizedName;
            supplier.CompanyName = companyName;
            supplier.Phone = phone;
            supplier.Email = email;
            supplier.Address = address;
            supplier.Voen = voen;
            supplier.BankName = bankName;
            supplier.BankAccount = bankAccount;
            supplier.Currency = currency;
            supplier.PaymentType = paymentType;
            supplier.Note = note;
            supplier.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<Supplier>.Success(supplier, "Təchizatçı uğurla yeniləndi.");
        }

        // Təchizatçını passiv edir.
        // Əgər aktiv qaiməsi varsa silmirik, sadəcə passiv edirik.
        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (supplier == null)
            {
                return Result<bool>.Fail("Təchizatçı tapılmadı.");
            }

            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Təchizatçı passiv edildi.");
        }
    }
}

