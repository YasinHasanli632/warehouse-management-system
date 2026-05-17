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
    // Bu servis müştəri məlumatlarını idarə edir.
    // Müştəri çıxış qaiməsində istifadə olunur.
    public class CustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
        }

        // Bütün aktiv müştəriləri gətirir.
        public async Task<Result<List<Customer>>> GetAllAsync()
        {
            var customers = await _context.Customers
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result<List<Customer>>.Success(customers);
        }

        // ID-yə görə müştəri detalını gətirir.
        public async Task<Result<Customer>> GetByIdAsync(int id)
        {
            var customer = await _context.Customers
                .Include(x => x.Invoices.Where(i => i.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (customer == null)
            {
                return Result<Customer>.Fail("Müştəri tapılmadı.");
            }

            return Result<Customer>.Success(customer);
        }

        // Yeni müştəri yaradır.
        public async Task<Result<Customer>> CreateAsync(
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
            decimal creditLimit = 0,
            string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Customer>.Fail("Müştəri adı boş ola bilməz.");
            }

            if (creditLimit < 0)
            {
                return Result<Customer>.Fail("Kredit limiti mənfi ola bilməz.");
            }

            var normalizedName = name.Trim();

            var exists = await _context.Customers.AnyAsync(x =>
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower());

            if (exists)
            {
                return Result<Customer>.Fail("Bu adda müştəri artıq mövcuddur.");
            }

            var customer = new Customer
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
                CreditLimit = creditLimit,
                DebtAmount = 0,
                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            return Result<Customer>.Success(customer, "Müştəri uğurla yaradıldı.");
        }

        // Müştəri məlumatlarını yeniləyir.
        public async Task<Result<Customer>> UpdateAsync(
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
            decimal creditLimit = 0,
            string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Customer>.Fail("Müştəri adı boş ola bilməz.");
            }

            if (creditLimit < 0)
            {
                return Result<Customer>.Fail("Kredit limiti mənfi ola bilməz.");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (customer == null)
            {
                return Result<Customer>.Fail("Müştəri tapılmadı.");
            }

            var normalizedName = name.Trim();

            var exists = await _context.Customers.AnyAsync(x =>
                x.Id != id &&
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower());

            if (exists)
            {
                return Result<Customer>.Fail("Bu adda müştəri artıq mövcuddur.");
            }

            customer.Name = normalizedName;
            customer.CompanyName = companyName;
            customer.Phone = phone;
            customer.Email = email;
            customer.Address = address;
            customer.Voen = voen;
            customer.BankName = bankName;
            customer.BankAccount = bankAccount;
            customer.Currency = currency;
            customer.PaymentType = paymentType;
            customer.CreditLimit = creditLimit;
            customer.Note = note;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<Customer>.Success(customer, "Müştəri uğurla yeniləndi.");
        }

        // Müştərini passiv edir.
        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (customer == null)
            {
                return Result<bool>.Fail("Müştəri tapılmadı.");
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Müştəri passiv edildi.");
        }

        // Müştərinin borcunu artırır və ya azaldır.
        // amount müsbət gələrsə borc artır, mənfi gələrsə borc azalır.
        public async Task<Result<bool>> UpdateDebtAsync(int customerId, decimal amount)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == customerId && x.IsActive);

            if (customer == null)
            {
                return Result<bool>.Fail("Müştəri tapılmadı.");
            }

            var newDebt = customer.DebtAmount + amount;

            if (newDebt < 0)
            {
                return Result<bool>.Fail("Borc məbləği 0-dan aşağı düşə bilməz.");
            }

            if (customer.CreditLimit > 0 && newDebt > customer.CreditLimit)
            {
                return Result<bool>.Fail("Müştərinin kredit limiti aşılır.");
            }

            customer.DebtAmount = newDebt;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Müştəri borcu yeniləndi.");
        }
    }
}
