using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Müştəri borc tarixçəsini və cari borcu idarə edir.
    public class CustomerBalanceService
    {
        private readonly AppDbContext _context;
        private readonly CurrencyService _currencyService;

        public CustomerBalanceService(AppDbContext context, CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<Result<bool>> ApplyInvoiceDebtAsync(int invoiceId, bool saveChanges = true)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            return await CreateDebtFromInvoiceAsync(invoice, saveChanges);
        }

        public async Task<Result<bool>> CreateDebtFromInvoiceAsync(Invoice invoice, bool saveChanges = true)
        {
            if (invoice == null)
                return Result<bool>.Fail("Qaimə boş ola bilməz.");

            if (!invoice.CustomerId.HasValue || invoice.CustomerId.Value <= 0)
                return Result<bool>.Success(true, "Qaimədə müştəri yoxdur, borc yaradılmadı.");

            if (invoice.Type != InvoiceType.StockOut && invoice.Type != InvoiceType.CustomerReturnIn)
                return Result<bool>.Success(true, "Bu qaimə müştəri balansına təsir etmir.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == invoice.CustomerId.Value && x.IsActive);

            if (customer == null)
                return Result<bool>.Fail("Müştəri tapılmadı.");

            var exists = await _context.CustomerBalanceTransactions
                .AnyAsync(x =>
                    x.IsActive &&
                    x.InvoiceId == invoice.Id &&
                    x.TransactionType == BalanceTransactionType.InvoiceDebt);

            if (exists)
                return Result<bool>.Success(true, "Bu qaimə üzrə müştəri borc tarixçəsi artıq yaradılıb.");

            var exchangeRate = invoice.ExchangeRate <= 0 ? 1 : invoice.ExchangeRate;

            var localDebt = invoice.DebtAmount;
            var originalDebt = invoice.OriginalDebtAmount > 0
                ? invoice.OriginalDebtAmount
                : invoice.Currency == CurrencyType.AZN
                    ? invoice.DebtAmount
                    : invoice.DebtAmount / exchangeRate;

            if (invoice.Type == InvoiceType.CustomerReturnIn)
            {
                localDebt *= -1;
                originalDebt *= -1;
            }

            var debtBefore = customer.DebtAmountLocal;
            var debtAfter = debtBefore + localDebt;

            if (debtAfter < 0)
                debtAfter = 0;

            customer.DebtAmountLocal = debtAfter;
            customer.DebtAmount = debtAfter;

            customer.DebtAmountOriginal += originalDebt;

            if (customer.DebtAmountOriginal < 0)
                customer.DebtAmountOriginal = 0;

            customer.UpdatedAt = DateTime.Now;

            var transaction = new CustomerBalanceTransaction
            {
                CustomerId = customer.Id,
                InvoiceId = invoice.Id,
                TransactionType = BalanceTransactionType.InvoiceDebt,
                Currency = invoice.Currency,
                ExchangeRate = exchangeRate,
                OriginalAmount = originalDebt,
                LocalAmount = localDebt,
                DebtBefore = debtBefore,
                DebtAfter = debtAfter,
                TransactionDate = DateTime.Now,
                Note = invoice.Type == InvoiceType.CustomerReturnIn
                    ? $"Müştəridən geri qaytarma üzrə borc azaldıldı. Qaimə №: {invoice.InvoiceNumber}"
                    : $"Çıxış qaiməsi üzrə müştəri borcu yazıldı. Qaimə №: {invoice.InvoiceNumber}",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.CustomerBalanceTransactions.AddAsync(transaction);

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Müştəri borcu yeniləndi.");
        }

        public async Task<Result<CustomerPayment>> ApplyPaymentAsync(
            int customerId,
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate,
            PaymentType paymentType,
            DateTime? paymentDate = null,
            string? referenceNumber = null,
            string? note = null)
        {
            return await AddPaymentAsync(
                customerId,
                originalAmount,
                currency,
                exchangeRate,
                paymentType,
                paymentDate,
                referenceNumber,
                note);
        }

        public async Task<Result<CustomerPayment>> AddPaymentAsync(
            int customerId,
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate,
            PaymentType paymentType,
            DateTime? paymentDate = null,
            string? referenceNumber = null,
            string? note = null)
        {
            if (customerId <= 0)
                return Result<CustomerPayment>.Fail("Müştəri düzgün seçilməyib.");

            if (originalAmount <= 0)
                return Result<CustomerPayment>.Fail("Ödəniş məbləği 0-dan böyük olmalıdır.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == customerId && x.IsActive);

            if (customer == null)
                return Result<CustomerPayment>.Fail("Müştəri tapılmadı.");

            var finalExchangeRate = currency == CurrencyType.AZN
                ? 1
                : exchangeRate <= 0 ? 1 : exchangeRate;

            var localResult = await _currencyService.ConvertToLocalAsync(
                originalAmount,
                currency,
                finalExchangeRate);

            if (!localResult.IsSuccess)
                return Result<CustomerPayment>.Fail(localResult.Message);

            var localAmount = localResult.Data;

            var debtBefore = customer.DebtAmountLocal;
            var debtAfter = Math.Max(debtBefore - localAmount, 0);

            var payment = new CustomerPayment
            {
                CustomerId = customer.Id,
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

            customer.DebtAmountLocal = debtAfter;
            customer.DebtAmount = debtAfter;

            customer.DebtAmountOriginal = Math.Max(customer.DebtAmountOriginal - originalAmount, 0);
            customer.UpdatedAt = DateTime.Now;

            var balanceTransaction = new CustomerBalanceTransaction
            {
                CustomerId = customer.Id,
                CustomerPayment = payment,
                TransactionType = BalanceTransactionType.Payment,
                Currency = currency,
                ExchangeRate = finalExchangeRate,
                OriginalAmount = originalAmount * -1,
                LocalAmount = localAmount * -1,
                DebtBefore = debtBefore,
                DebtAfter = debtAfter,
                TransactionDate = payment.PaymentDate,
                Note = string.IsNullOrWhiteSpace(note)
                    ? "Müştəridən ödəniş qəbul edildi."
                    : note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.CustomerPayments.AddAsync(payment);
            await _context.CustomerBalanceTransactions.AddAsync(balanceTransaction);
            await _context.SaveChangesAsync();

            return Result<CustomerPayment>.Success(payment, "Müştəri ödənişi qeydə alındı.");
        }

        public async Task<Result<bool>> RecalculateCustomerBalanceAsync(int customerId)
        {
            if (customerId <= 0)
                return Result<bool>.Fail("Müştəri düzgün seçilməyib.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == customerId && x.IsActive);

            if (customer == null)
                return Result<bool>.Fail("Müştəri tapılmadı.");

            var transactions = await _context.CustomerBalanceTransactions
                .Where(x => x.CustomerId == customerId && x.IsActive)
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

            customer.DebtAmountLocal = localDebt;
            customer.DebtAmount = localDebt;
            customer.DebtAmountOriginal = originalDebt;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Müştəri balansı yenidən hesablandı.");
        }
    }
}