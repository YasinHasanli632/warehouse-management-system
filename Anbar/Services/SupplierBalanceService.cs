using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Təchizatçı borc tarixçəsini və cari borcu idarə edir.
    // InvoiceService artıq təchizatçı borcunu özü artırmamalıdır.
    // InvoiceService sadəcə ApplyInvoiceDebtAsync(invoiceId) çağırmalıdır.
    public class SupplierBalanceService
    {
        private readonly AppDbContext _context;
        private readonly CurrencyService _currencyService;

        public SupplierBalanceService(AppDbContext context, CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        // YENI:
        // InvoiceService üçün əsas metod.
        public async Task<Result<bool>> ApplyInvoiceDebtAsync(int invoiceId, bool saveChanges = true)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            return await CreateDebtFromInvoiceAsync(invoice, saveChanges);
        }

        // KOHNE COMPATIBILITY:
        // Köhnə çağırışlar qırılmasın deyə saxlanılır.
        public async Task<Result<bool>> CreateDebtFromInvoiceAsync(Invoice invoice, bool saveChanges = true)
        {
            if (invoice == null)
                return Result<bool>.Fail("Qaimə boş ola bilməz.");

            if (!invoice.SupplierId.HasValue || invoice.SupplierId.Value <= 0)
                return Result<bool>.Success(true, "Qaimədə təchizatçı yoxdur, borc yaradılmadı.");

            if (invoice.Type != InvoiceType.StockIn && invoice.Type != InvoiceType.SupplierReturnOut)
                return Result<bool>.Success(true, "Bu qaimə təchizatçı balansına təsir etmir.");

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == invoice.SupplierId.Value && x.IsActive);

            if (supplier == null)
                return Result<bool>.Fail("Təchizatçı tapılmadı.");

            var existing = await _context.SupplierBalanceTransactions
                .AnyAsync(x =>
                    x.IsActive &&
                    x.InvoiceId == invoice.Id &&
                    x.TransactionType == BalanceTransactionType.InvoiceDebt);

            if (existing)
                return Result<bool>.Success(true, "Bu qaimə üzrə borc tarixçəsi artıq yaradılıb.");

            var exchangeRate = invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;

            var localDebt = invoice.DebtAmount;
            var originalDebt = invoice.OriginalDebtAmount > 0
                ? invoice.OriginalDebtAmount
                : invoice.Currency == CurrencyType.AZN
                    ? invoice.DebtAmount
                    : invoice.DebtAmount / exchangeRate;

            // YENI:
            // Təchizatçıya geri qaytarma borcu azaldır.
            if (invoice.Type == InvoiceType.SupplierReturnOut)
            {
                localDebt *= -1;
                originalDebt *= -1;
            }

            var debtBefore = supplier.DebtAmountLocal;
            var debtAfter = debtBefore + localDebt;

            if (debtAfter < 0)
                debtAfter = 0;

            supplier.DebtAmountLocal = debtAfter;
            supplier.DebtAmount = debtAfter;

            supplier.DebtAmountOriginal += originalDebt;

            if (supplier.DebtAmountOriginal < 0)
                supplier.DebtAmountOriginal = 0;

            supplier.DebtAmountOriginalCurrency = supplier.DebtAmountOriginal;
            supplier.UpdatedAt = DateTime.Now;

            var transaction = new SupplierBalanceTransaction
            {
                SupplierId = supplier.Id,
                InvoiceId = invoice.Id,
                TransactionType = BalanceTransactionType.InvoiceDebt,
                Currency = invoice.Currency,
                ExchangeRate = exchangeRate,
                OriginalAmount = originalDebt,
                LocalAmount = localDebt,
                DebtBefore = debtBefore,
                DebtAfter = debtAfter,
                TransactionDate = DateTime.Now,
                Note = invoice.Type == InvoiceType.SupplierReturnOut
                    ? $"Təchizatçıya geri qaytarma üzrə borc azaldıldı. Qaimə №: {invoice.InvoiceNumber}"
                    : $"Giriş qaiməsi üzrə borc yazıldı. Qaimə №: {invoice.InvoiceNumber}",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.SupplierBalanceTransactions.AddAsync(transaction);

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Təchizatçı borcu yeniləndi.");
        }

        // YENI:
        // Roadmap adlandırmasına uyğun payment metodu.
        public async Task<Result<SupplierPayment>> ApplyPaymentAsync(
            int supplierId,
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate,
            PaymentType paymentType,
            DateTime? paymentDate = null,
            string? referenceNumber = null,
            string? note = null)
        {
            return await AddPaymentAsync(
                supplierId,
                originalAmount,
                currency,
                exchangeRate,
                paymentType,
                paymentDate,
                referenceNumber,
                note);
        }

        public async Task<Result<SupplierPayment>> AddPaymentAsync(
            int supplierId,
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate,
            PaymentType paymentType,
            DateTime? paymentDate = null,
            string? referenceNumber = null,
            string? note = null)
        {
            if (supplierId <= 0)
                return Result<SupplierPayment>.Fail("Təchizatçı düzgün seçilməyib.");

            if (originalAmount <= 0)
                return Result<SupplierPayment>.Fail("Ödəniş məbləği 0-dan böyük olmalıdır.");

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == supplierId && x.IsActive);

            if (supplier == null)
                return Result<SupplierPayment>.Fail("Təchizatçı tapılmadı.");

            var finalExchangeRate = currency == CurrencyType.AZN
                ? 1
                : exchangeRate <= 0 ? 1 : exchangeRate;

            var localResult = await _currencyService.ConvertToLocalAsync(
                originalAmount,
                currency,
                finalExchangeRate);

            if (!localResult.IsSuccess)
                return Result<SupplierPayment>.Fail(localResult.Message);

            var localAmount = localResult.Data;

            var debtBefore = supplier.DebtAmountLocal;
            var debtAfter = Math.Max(debtBefore - localAmount, 0);

            var payment = new SupplierPayment
            {
                SupplierId = supplier.Id,
                Amount = localAmount,
                Currency = currency,
                ExchangeRate = finalExchangeRate,
                OriginalAmount = originalAmount,
                LocalAmount = localAmount,
                PaymentDate = paymentDate ?? DateTime.Now,
                PaymentType = paymentType,
                DebtBeforePayment = debtBefore,
                DebtAfterPayment = debtAfter,
                ReferenceNumber = referenceNumber,
                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            supplier.DebtAmountLocal = debtAfter;
            supplier.DebtAmount = debtAfter;

            supplier.DebtAmountOriginal = Math.Max(
                supplier.DebtAmountOriginal - originalAmount,
                0);

            supplier.DebtAmountOriginalCurrency = supplier.DebtAmountOriginal;
            supplier.UpdatedAt = DateTime.Now;

            var balance = new SupplierBalanceTransaction
            {
                SupplierId = supplier.Id,
                SupplierPayment = payment,
                TransactionType = BalanceTransactionType.Payment,
                Currency = currency,
                ExchangeRate = finalExchangeRate,
                OriginalAmount = originalAmount * -1,
                LocalAmount = localAmount * -1,
                DebtBefore = debtBefore,
                DebtAfter = debtAfter,
                TransactionDate = payment.PaymentDate,
                Note = string.IsNullOrWhiteSpace(note)
                    ? "Təchizatçıya ödəniş edildi."
                    : note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.SupplierPayments.AddAsync(payment);
            await _context.SupplierBalanceTransactions.AddAsync(balance);
            await _context.SaveChangesAsync();

            return Result<SupplierPayment>.Success(payment, "Təchizatçı ödənişi qeydə alındı.");
        }

        // YENI:
        // Təchizatçı balansını transaction tarixçəsinə görə yenidən hesablayır.
        public async Task<Result<bool>> RecalculateSupplierBalanceAsync(int supplierId)
        {
            if (supplierId <= 0)
                return Result<bool>.Fail("Təchizatçı düzgün seçilməyib.");

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == supplierId && x.IsActive);

            if (supplier == null)
                return Result<bool>.Fail("Təchizatçı tapılmadı.");

            var transactions = await _context.SupplierBalanceTransactions
                .Where(x => x.SupplierId == supplierId && x.IsActive)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var localDebt = 0m;
            var originalDebt = 0m;

            foreach (var transaction in transactions)
            {
                transaction.DebtBefore = localDebt;

                localDebt += transaction.LocalAmount;
                originalDebt += transaction.OriginalAmount;

                if (localDebt < 0)
                    localDebt = 0;

                if (originalDebt < 0)
                    originalDebt = 0;

                transaction.DebtAfter = localDebt;
                transaction.UpdatedAt = DateTime.Now;
            }

            supplier.DebtAmountLocal = localDebt;
            supplier.DebtAmount = localDebt;
            supplier.DebtAmountOriginal = originalDebt;
            supplier.DebtAmountOriginalCurrency = originalDebt;
            supplier.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Təchizatçı balansı yenidən hesablandı.");
        }
    }
}