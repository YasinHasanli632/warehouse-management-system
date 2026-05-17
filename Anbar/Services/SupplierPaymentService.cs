using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // Bu servis təchizatçıya edilən ödənişləri idarə edir.
    // Məntiq:
    // 1. Supplier.DebtAmount cari borcu saxlayır.
    // 2. SupplierPayment ödəniş tarixçəsini saxlayır.
    // 3. Ödəniş ediləndə supplier borcu azalır.
    public class SupplierPaymentService
    {
        private readonly AppDbContext _context;

        public SupplierPaymentService(AppDbContext context)
        {
            _context = context;
        }

        // Bütün aktiv təchizatçı ödənişlərini gətirir.
        public async Task<Result<List<SupplierPayment>>> GetAllAsync()
        {
            var payments = await _context.SupplierPayments
                .Include(x => x.Supplier)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return Result<List<SupplierPayment>>.Success(payments);
        }

        // ID-yə görə ödəniş detalını gətirir.
        public async Task<Result<SupplierPayment>> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                return Result<SupplierPayment>.Fail("Ödəniş düzgün seçilməyib.");
            }

            var payment = await _context.SupplierPayments
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (payment == null)
            {
                return Result<SupplierPayment>.Fail("Təchizatçı ödənişi tapılmadı.");
            }

            return Result<SupplierPayment>.Success(payment);
        }

        // Bir təchizatçıya aid bütün aktiv ödənişləri gətirir.
        public async Task<Result<List<SupplierPayment>>> GetBySupplierIdAsync(int supplierId)
        {
            if (supplierId <= 0)
            {
                return Result<List<SupplierPayment>>.Fail("Təchizatçı düzgün seçilməyib.");
            }

            var supplierExists = await _context.Suppliers
                .AnyAsync(x => x.Id == supplierId && x.IsActive);

            if (!supplierExists)
            {
                return Result<List<SupplierPayment>>.Fail("Təchizatçı tapılmadı.");
            }

            var payments = await _context.SupplierPayments
                .Include(x => x.Supplier)
                .Where(x => x.SupplierId == supplierId && x.IsActive)
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return Result<List<SupplierPayment>>.Success(payments);
        }

        // Təchizatçıya ödəniş edir.
        // Bu metod həm payment tarixçəsi yaradır, həm də Supplier.DebtAmount dəyərini azaldır.
        public async Task<Result<SupplierPayment>> CreatePaymentAsync(
            int supplierId,
            decimal amount,
            PaymentType paymentType = PaymentType.Cash,
            DateTime? paymentDate = null,
            string? note = null)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (supplierId <= 0)
                {
                    return Result<SupplierPayment>.Fail("Təchizatçı düzgün seçilməyib.");
                }

                if (amount <= 0)
                {
                    return Result<SupplierPayment>.Fail("Ödəniş məbləği 0-dan böyük olmalıdır.");
                }

                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(x => x.Id == supplierId && x.IsActive);

                if (supplier == null)
                {
                    return Result<SupplierPayment>.Fail("Təchizatçı tapılmadı.");
                }

                if (supplier.DebtAmount <= 0)
                {
                    return Result<SupplierPayment>.Fail("Bu təchizatçıya borc yoxdur.");
                }

                if (amount > supplier.DebtAmount)
                {
                    return Result<SupplierPayment>.Fail("Ödəniş məbləği təchizatçı borcundan çox ola bilməz.");
                }

                var now = DateTime.Now;
                var debtBeforePayment = supplier.DebtAmount;
                var debtAfterPayment = supplier.DebtAmount - amount;

                var payment = new SupplierPayment
                {
                    SupplierId = supplier.Id,
                    Amount = amount,
                    PaymentDate = paymentDate ?? now,
                    PaymentType = paymentType,
                    DebtBeforePayment = debtBeforePayment,
                    DebtAfterPayment = debtAfterPayment,
                    Note = note,
                    CreatedAt = now,
                    IsActive = true
                };

                supplier.DebtAmount = debtAfterPayment;
                supplier.UpdatedAt = now;

                await _context.SupplierPayments.AddAsync(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Result<SupplierPayment>.Success(payment, "Təchizatçı ödənişi uğurla qeydə alındı.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<SupplierPayment>.Fail($"Təchizatçı ödənişi qeydə alınmadı: {ex.Message}");
            }
        }

        // Ödənişi passiv edir və supplier borcunu geri artırır.
        // Real sistemdə payment silinmir, tarixçə üçün passiv edilir.
        public async Task<Result<bool>> DeactivateAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (id <= 0)
                {
                    return Result<bool>.Fail("Ödəniş düzgün seçilməyib.");
                }

                var payment = await _context.SupplierPayments
                    .Include(x => x.Supplier)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (payment == null)
                {
                    return Result<bool>.Fail("Təchizatçı ödənişi tapılmadı.");
                }

                if (payment.Supplier == null || !payment.Supplier.IsActive)
                {
                    return Result<bool>.Fail("Təchizatçı tapılmadı.");
                }

                var now = DateTime.Now;

                payment.IsActive = false;
                payment.UpdatedAt = now;

                payment.Supplier.DebtAmount += payment.Amount;
                payment.Supplier.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<bool>.Success(true, "Təchizatçı ödənişi ləğv edildi və borc geri qaytarıldı.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Fail($"Təchizatçı ödənişi ləğv edilmədi: {ex.Message}");
            }
        }

        // Təchizatçı üzrə ümumi ödəniş məbləğini gətirir.
        public async Task<Result<decimal>> GetTotalPaidBySupplierAsync(int supplierId)
        {
            if (supplierId <= 0)
            {
                return Result<decimal>.Fail("Təchizatçı düzgün seçilməyib.");
            }

            var supplierExists = await _context.Suppliers
                .AnyAsync(x => x.Id == supplierId && x.IsActive);

            if (!supplierExists)
            {
                return Result<decimal>.Fail("Təchizatçı tapılmadı.");
            }

            var totalPaid = await _context.SupplierPayments
                .Where(x => x.SupplierId == supplierId && x.IsActive)
                .SumAsync(x => x.Amount);

            return Result<decimal>.Success(totalPaid);
        }
    }
}