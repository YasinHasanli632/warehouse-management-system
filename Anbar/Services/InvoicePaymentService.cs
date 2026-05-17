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
    // YENI:
    // Qaimə üzrə ödənişləri idarə edir.
    // Bir qaimədə bir neçə ödəniş ola bilər: nağd, kart, bank, kredit.
    public class InvoicePaymentService
    {
        private readonly AppDbContext _context;
        private readonly CurrencyService _currencyService;

        public InvoicePaymentService(AppDbContext context, CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<Result<InvoicePayment>> AddPaymentAsync(
            int invoiceId,
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate,
            PaymentType paymentType,
            DateTime? paymentDate = null,
            string? referenceNumber = null,
            string? note = null,
            bool saveChanges = true)
        {
            if (invoiceId <= 0)
                return Result<InvoicePayment>.Fail("Qaimə düzgün seçilməyib.");

            if (originalAmount < 0)
                return Result<InvoicePayment>.Fail("Ödəniş məbləği mənfi ola bilməz.");

            var invoice = await _context.Invoices
                .Include(x => x.Payments.Where(p => p.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<InvoicePayment>.Fail("Qaimə tapılmadı.");

            if (invoice.Status == InvoiceStatus.Confirmed)
                return Result<InvoicePayment>.Fail("Təsdiqlənmiş qaiməyə yeni ödəniş əlavə etmək üçün ayrıca ödəniş əməliyyatı istifadə olunmalıdır.");

            var localResult = await _currencyService.ConvertToLocalAsync(originalAmount, currency, exchangeRate);
            if (!localResult.IsSuccess)
                return Result<InvoicePayment>.Fail(localResult.Message);

            var payment = new InvoicePayment
            {
                InvoiceId = invoice.Id,
                PaymentType = paymentType,
                Currency = currency,
                ExchangeRate = currency == CurrencyType.AZN ? 1 : exchangeRate,
                OriginalAmount = originalAmount,
                LocalAmount = localResult.Data,
                PaymentDate = paymentDate ?? DateTime.Now,
                ReferenceNumber = referenceNumber,
                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.InvoicePayments.AddAsync(payment);

            await RecalculateInvoicePaymentSummaryAsync(invoice.Id, saveChanges: false);

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<InvoicePayment>.Success(payment, "Qaimə ödənişi əlavə olundu.");
        }

        public async Task<Result<bool>> RecalculateInvoicePaymentSummaryAsync(int invoiceId, bool saveChanges = true)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Payments.Where(p => p.IsActive))
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            var localPaid = invoice.Payments.Where(x => x.IsActive).Sum(x => x.LocalAmount);
            var originalPaid = invoice.Payments.Where(x => x.IsActive && x.Currency == invoice.Currency).Sum(x => x.OriginalAmount);

            // YENI:
            // Köhnə PaidAmount sahəsi AZN qarşılığı kimi saxlanır.
            invoice.PaidAmount = localPaid;
            invoice.OriginalPaidAmount = originalPaid;

            var totalLocal = invoice.LocalTotalAmount > 0 ? invoice.LocalTotalAmount : invoice.TotalAmount;
            var totalOriginal = invoice.OriginalTotalAmount > 0 ? invoice.OriginalTotalAmount : invoice.OriginalItemsTotalAmount;

            invoice.DebtAmount = Math.Max(totalLocal - localPaid, 0);
            invoice.OriginalDebtAmount = Math.Max(totalOriginal - originalPaid, 0);

            if (localPaid <= 0)
                invoice.PaymentStatus = PaymentStatus.Unpaid;
            else if (localPaid < totalLocal)
                invoice.PaymentStatus = PaymentStatus.PartialPaid;
            else if (localPaid == totalLocal)
                invoice.PaymentStatus = PaymentStatus.Paid;
            else
                invoice.PaymentStatus = PaymentStatus.OverPaid;

            invoice.UpdatedAt = DateTime.Now;

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə ödəniş yekunu hesablandı.");
        }
    }

}
