using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    // YENI:
    // Valyuta və məzənnə çevirmələrini idarə edir.
    // Əsas lokal sistem valyutası AZN-dir.
    public class CurrencyService
    {
        private readonly AppDbContext _context;

        public CurrencyService(AppDbContext context)
        {
            _context = context;
        }

        // YENI:
        // Xarici valyutanı AZN-ə çevirir.
        public decimal ConvertToLocal(decimal originalAmount, CurrencyType currency, decimal exchangeRate)
        {
            if (originalAmount == 0)
                return 0;

            if (originalAmount < 0)
                throw new InvalidOperationException("Məbləğ mənfi ola bilməz.");

            if (currency == CurrencyType.AZN)
                return Math.Round(originalAmount, 2);

            if (exchangeRate <= 0)
                throw new InvalidOperationException("Xarici valyuta üçün məzənnə 0-dan böyük olmalıdır.");

            return Math.Round(originalAmount * exchangeRate, 2);
        }

        // YENI:
        // AZN məbləği original valyutaya çevirir.
        public decimal ConvertFromLocal(decimal localAmount, CurrencyType targetCurrency, decimal exchangeRate)
        {
            if (localAmount == 0)
                return 0;

            if (localAmount < 0)
                throw new InvalidOperationException("Məbləğ mənfi ola bilməz.");

            if (targetCurrency == CurrencyType.AZN)
                return Math.Round(localAmount, 2);

            if (exchangeRate <= 0)
                throw new InvalidOperationException("Xarici valyuta üçün məzənnə 0-dan böyük olmalıdır.");

            return Math.Round(localAmount / exchangeRate, 2);
        }

        public async Task<Result<decimal>> ConvertToLocalAsync(
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate)
        {
            if (originalAmount < 0)
                return Result<decimal>.Fail("Məbləğ mənfi ola bilməz.");

            if (currency == CurrencyType.AZN)
                return Result<decimal>.Success(Math.Round(originalAmount, 2));

            if (exchangeRate <= 0)
                return Result<decimal>.Fail("Xarici valyuta üçün məzənnə 0-dan böyük olmalıdır.");

            var localAmount = Math.Round(originalAmount * exchangeRate, 2);
            return Result<decimal>.Success(localAmount);
        }

        public async Task<Result<decimal>> ConvertFromLocalAsync(
            decimal localAmount,
            CurrencyType targetCurrency,
            decimal exchangeRate)
        {
            if (localAmount < 0)
                return Result<decimal>.Fail("Məbləğ mənfi ola bilməz.");

            if (targetCurrency == CurrencyType.AZN)
                return Result<decimal>.Success(Math.Round(localAmount, 2));

            if (exchangeRate <= 0)
                return Result<decimal>.Fail("Xarici valyuta üçün məzənnə 0-dan böyük olmalıdır.");

            var originalAmount = Math.Round(localAmount / exchangeRate, 2);
            return Result<decimal>.Success(originalAmount);
        }

        // YENI:
        // Məzənnəni normal formaya salır.
        public Result<decimal> NormalizeRate(CurrencyType currency, decimal exchangeRate)
        {
            if (currency == CurrencyType.AZN)
                return Result<decimal>.Success(1);

            if (exchangeRate <= 0)
                return Result<decimal>.Fail("Xarici valyuta üçün məzənnə 0-dan böyük olmalıdır.");

            return Result<decimal>.Success(exchangeRate);
        }

        public async Task<Result<decimal>> NormalizeRateAsync(CurrencyType currency, decimal exchangeRate)
        {
            return await Task.FromResult(NormalizeRate(currency, exchangeRate));
        }

        // YENI:
        // Verilmiş tarixə görə ən son aktiv məzənnəni gətirir.
        public async Task<Result<decimal>> GetRateAsync(CurrencyType fromCurrency, DateTime date)
        {
            if (fromCurrency == CurrencyType.AZN)
                return Result<decimal>.Success(1);

            var onlyDate = date.Date;

            var rate = await _context.CurrencyRates
                .Where(x =>
                    x.IsActive &&
                    x.FromCurrency == fromCurrency &&
                    x.ToCurrency == CurrencyType.AZN &&
                    x.RateDate.Date <= onlyDate)
                .OrderByDescending(x => x.RateDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (rate == null)
                return Result<decimal>.Fail($"{fromCurrency} üçün məzənnə tapılmadı. Məzənnəni manual daxil edin.");

            if (rate.Rate <= 0)
                return Result<decimal>.Fail("Tapılan məzənnə düzgün deyil.");

            return Result<decimal>.Success(rate.Rate);
        }

        // YENI:
        // Roadmap compatibility.
        public async Task<Result<decimal>> GetRateByDateAsync(CurrencyType fromCurrency, DateTime date)
        {
            return await GetRateAsync(fromCurrency, date);
        }

        // YENI:
        // Əgər manual rate verilibsə onu istifadə edir, yoxdursa DB-dən tapır.
        public async Task<Result<decimal>> ResolveRateAsync(
            CurrencyType currency,
            decimal manualExchangeRate,
            DateTime date)
        {
            if (currency == CurrencyType.AZN)
                return Result<decimal>.Success(1);

            if (manualExchangeRate > 0)
                return Result<decimal>.Success(manualExchangeRate);

            return await GetRateAsync(currency, date);
        }

        // YENI:
        // Məzənnə yadda saxlayır və ya həmin gün üçün mövcud olanı update edir.
        public async Task<Result<CurrencyRate>> SaveRateAsync(
            CurrencyType fromCurrency,
            decimal rate,
            DateTime rateDate,
            CurrencyRateSource source = CurrencyRateSource.Manual,
            string? note = null)
        {
            if (fromCurrency == CurrencyType.AZN)
                return Result<CurrencyRate>.Fail("AZN üçün ayrıca məzənnə yaratmaq lazım deyil.");

            if (rate <= 0)
                return Result<CurrencyRate>.Fail("Məzənnə 0-dan böyük olmalıdır.");

            var existing = await _context.CurrencyRates
                .FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    x.FromCurrency == fromCurrency &&
                    x.ToCurrency == CurrencyType.AZN &&
                    x.RateDate.Date == rateDate.Date);

            if (existing != null)
            {
                existing.Rate = rate;
                existing.Source = source;
                existing.Note = note;
                existing.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<CurrencyRate>.Success(existing, "Məzənnə yeniləndi.");
            }

            var entity = new CurrencyRate
            {
                FromCurrency = fromCurrency,
                ToCurrency = CurrencyType.AZN,
                Rate = rate,
                RateDate = rateDate.Date,
                Source = source,
                Note = note,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.CurrencyRates.AddAsync(entity);
            await _context.SaveChangesAsync();

            return Result<CurrencyRate>.Success(entity, "Məzənnə əlavə olundu.");
        }

        // YENI:
        // Roadmap compatibility.
        public async Task<Result<CurrencyRate>> SaveCurrencyRateAsync(
            CurrencyType fromCurrency,
            decimal rate,
            DateTime rateDate,
            CurrencyRateSource source = CurrencyRateSource.Manual,
            string? note = null)
        {
            return await SaveRateAsync(fromCurrency, rate, rateDate, source, note);
        }

        // YENI:
        // Valyuta məlumatlarını bir yerdə normalize edir.
        public async Task<Result<CurrencyConversionResult>> ConvertAsync(
            decimal originalAmount,
            CurrencyType currency,
            decimal exchangeRate,
            DateTime date)
        {
            if (originalAmount < 0)
                return Result<CurrencyConversionResult>.Fail("Məbləğ mənfi ola bilməz.");

            var rateResult = await ResolveRateAsync(currency, exchangeRate, date);

            if (!rateResult.IsSuccess)
                return Result<CurrencyConversionResult>.Fail(rateResult.Message);

            var finalRate = rateResult.Data;

            var localResult = await ConvertToLocalAsync(originalAmount, currency, finalRate);

            if (!localResult.IsSuccess)
                return Result<CurrencyConversionResult>.Fail(localResult.Message);

            var result = new CurrencyConversionResult
            {
                OriginalAmount = originalAmount,
                Currency = currency,
                ExchangeRate = finalRate,
                LocalAmount = localResult.Data
            };

            return Result<CurrencyConversionResult>.Success(result);
        }
    }

    // YENI:
    // Entity deyil, sadəcə servis nəticəsi üçün helper modeldir.
    public class CurrencyConversionResult
    {
        public decimal OriginalAmount { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal LocalAmount { get; set; }
    }
}