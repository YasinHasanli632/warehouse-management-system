using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Services
{
    public class ImportInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly CurrencyService _currencyService;

        public ImportInvoiceService(AppDbContext context)
        {
            _context = context;
            _currencyService = new CurrencyService(context);
        }

        public ImportInvoiceService(AppDbContext context, CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<Result<InvoiceImportInfo>> SaveImportInfoAsync(
            int invoiceId,
            string? declarationNo,
            string? originCountry,
            string? customsPoint,
            DateTime importDate,
            CurrencyType currency,
            decimal exchangeRate,
            decimal customsValue = 0,
            decimal customsDutyAmount = 0,
            decimal importVatAmount = 0,
            bool saveChanges = true)
        {
            return await CreateOrUpdateImportInfoAsync(
                invoiceId,
                declarationNo,
                originCountry,
                customsPoint,
                importDate,
                currency,
                exchangeRate,
                null,
                null,
                null,
                saveChanges);
        }

        public async Task<Result<InvoiceImportInfo>> CreateOrUpdateImportInfoAsync(
            int invoiceId,
            string? declarationNumber,
            string? originCountry,
            string? customsPoint,
            DateTime importDate,
            CurrencyType currency,
            decimal exchangeRate,
            string? foreignInvoiceNumber = null,
            string? transportDocumentNumber = null,
            string? note = null,
            bool saveChanges = true)
        {
            if (invoiceId <= 0)
                return Result<InvoiceImportInfo>.Fail("Qaimə düzgün seçilməyib.");

            var rateResult = await _currencyService.NormalizeRateAsync(currency, exchangeRate);

            if (!rateResult.IsSuccess)
                return Result<InvoiceImportInfo>.Fail(rateResult.Message);

            var finalRate = rateResult.Data;

            var invoice = await _context.Invoices
                .Include(x => x.ImportInfo)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<InvoiceImportInfo>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<InvoiceImportInfo>.Fail("İdxal məlumatları yalnız draft qaimədə dəyişdirilə bilər.");

            if (invoice.Type != InvoiceType.StockIn)
                return Result<InvoiceImportInfo>.Fail("İdxal məlumatı yalnız giriş qaiməsinə əlavə oluna bilər.");

            invoice.IsImport = true;
            invoice.Currency = currency;
            invoice.ExchangeRate = finalRate;
            invoice.UpdatedAt = DateTime.Now;

            if (invoice.ImportInfo == null)
            {
                var info = new InvoiceImportInfo
                {
                    InvoiceId = invoice.Id,
                    DeclarationNumber = NormalizeText(declarationNumber),
                    OriginCountry = NormalizeText(originCountry),
                    CustomsPoint = NormalizeText(customsPoint),
                    ImportDate = importDate == default ? DateTime.Now : importDate,
                    Currency = currency,
                    ExchangeRate = finalRate,
                    CurrencyRateSource = CurrencyRateSource.Manual,
                    ForeignInvoiceNumber = NormalizeText(foreignInvoiceNumber),
                    TransportDocumentNumber = NormalizeText(transportDocumentNumber),
                    Note = NormalizeText(note),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _context.InvoiceImportInfos.AddAsync(info);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<InvoiceImportInfo>.Success(info, "İdxal məlumatları əlavə olundu.");
            }

            invoice.ImportInfo.DeclarationNumber = NormalizeText(declarationNumber);
            invoice.ImportInfo.OriginCountry = NormalizeText(originCountry);
            invoice.ImportInfo.CustomsPoint = NormalizeText(customsPoint);
            invoice.ImportInfo.ImportDate = importDate == default ? DateTime.Now : importDate;
            invoice.ImportInfo.Currency = currency;
            invoice.ImportInfo.ExchangeRate = finalRate;
            invoice.ImportInfo.CurrencyRateSource = CurrencyRateSource.Manual;
            invoice.ImportInfo.ForeignInvoiceNumber = NormalizeText(foreignInvoiceNumber);
            invoice.ImportInfo.TransportDocumentNumber = NormalizeText(transportDocumentNumber);
            invoice.ImportInfo.Note = NormalizeText(note);
            invoice.ImportInfo.IsActive = true;
            invoice.ImportInfo.UpdatedAt = DateTime.Now;

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<InvoiceImportInfo>.Success(invoice.ImportInfo, "İdxal məlumatları yeniləndi.");
        }

        public async Task<Result<bool>> ValidateImportInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.ImportInfo)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (!invoice.IsImport)
                return Result<bool>.Success(true, "Qaimə idxal qaiməsi deyil.");

            if (invoice.Type != InvoiceType.StockIn)
                return Result<bool>.Fail("İdxal qaiməsi yalnız giriş qaiməsi ola bilər.");

            if (invoice.ImportInfo == null || !invoice.ImportInfo.IsActive)
                return Result<bool>.Fail("İdxal məlumatları daxil edilməyib.");

            if (invoice.Currency != CurrencyType.AZN && invoice.ExchangeRate <= 0)
                return Result<bool>.Fail("İdxal qaiməsində məzənnə 0-dan böyük olmalıdır.");

            if (invoice.ImportInfo.Currency != invoice.Currency)
                return Result<bool>.Fail("Qaimə valyutası ilə idxal məlumatı valyutası uyğun deyil.");

            if (invoice.ImportInfo.ExchangeRate <= 0)
                return Result<bool>.Fail("İdxal məlumatında məzənnə düzgün deyil.");

            return Result<bool>.Success(true, "İdxal qaiməsi validasiyadan keçdi.");
        }

        public async Task<Result<bool>> RemoveImportFlagAsync(int invoiceId, bool saveChanges = true)
        {
            var invoice = await _context.Invoices
                .Include(x => x.ImportInfo)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<bool>.Fail("İdxal statusu yalnız draft qaimədə dəyişdirilə bilər.");

            invoice.IsImport = false;
            invoice.UpdatedAt = DateTime.Now;

            if (invoice.ImportInfo != null)
            {
                invoice.ImportInfo.IsActive = false;
                invoice.ImportInfo.UpdatedAt = DateTime.Now;
            }

            if (saveChanges)
                await _context.SaveChangesAsync();

            return Result<bool>.Success(true, "Qaimə idxal qaiməsindən çıxarıldı.");
        }

        public async Task<Result<InvoiceImportInfo>> GetImportInfoAsync(int invoiceId)
        {
            var info = await _context.InvoiceImportInfos
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.IsActive);

            if (info == null)
                return Result<InvoiceImportInfo>.Fail("İdxal məlumatı tapılmadı.");

            return Result<InvoiceImportInfo>.Success(info);
        }

        public async Task<Result<List<string>>> GetVisibleImportFieldsAsync()
        {
            var fields = await _context.ImportFieldSettings
                .Where(x => x.IsActive && x.IsVisible)
                .OrderBy(x => x.SortOrder)
                .Select(x => x.FieldKey)
                .ToListAsync();

            if (!fields.Any())
            {
                fields = new List<string>
                {
                    "DeclarationNumber",
                    "ImportDate",
                    "OriginCountry",
                    "CustomsPoint",
                    "Currency",
                    "ExchangeRate",
                    "ForeignInvoiceNumber",
                    "TransportDocumentNumber"
                };
            }

            return Result<List<string>>.Success(fields);
        }

        public async Task<Result<bool>> AutoOpenImportFieldsForForeignSupplierAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

            if (invoice == null)
                return Result<bool>.Fail("Qaimə tapılmadı.");

            if (invoice.Type != InvoiceType.StockIn)
                return Result<bool>.Success(true, "Yalnız giriş qaiməsi üçün idxal field-ləri açılır.");

            if (invoice.Currency != CurrencyType.AZN || invoice.IsImport)
            {
                invoice.IsImport = true;
                invoice.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "İdxal field-ləri aktiv edildi.");
            }

            return Result<bool>.Success(true, "İdxal field-lərinin avtomatik açılmasına ehtiyac yoxdur.");
        }

        private static string? NormalizeText(string? value)
        {
            var text = value?.Trim();

            return string.IsNullOrWhiteSpace(text)
                ? null
                : text;
        }
    }
}