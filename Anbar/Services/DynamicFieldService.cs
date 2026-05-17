using Anbar.Data;
using Anbar.Entities;
using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Anbar.Services
{
    // YENI:
    // Qaimə üçün dinamik sahələri idarə edən servis.
    // Məsələn:
    // - Gömrük bəyannamə №
    // - İdxal tarixi
    // - Mənşə ölkəsi
    // - Gömrük postu
    // - Xarici qaimə №
    // - Daşıma sənədi №
    //
    // Bu servis stock/cost/tax hesablamır.
    // Sadəcə InvoiceDynamicFieldValue dəyərlərini saxlayır.
    public class DynamicFieldService
    {
        private readonly AppDbContext _context;

        public DynamicFieldService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ImportFieldSetting>>> GetImportFieldSettingsAsync(bool includePassive = false)
        {
            try
            {
                var query = _context.ImportFieldSettings.AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive && x.IsVisible);

                var data = await query
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.DisplayName)
                    .ToListAsync();

                return Result<List<ImportFieldSetting>>.Success(data, "Dinamik sahə ayarları yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ImportFieldSetting>>.Fail($"Dinamik sahə ayarları yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<ImportFieldSetting>>> GetVisibleInvoiceFieldsAsync()
        {
            try
            {
                var data = await _context.ImportFieldSettings
                    .Where(x => x.IsActive && x.IsVisible && x.ShowOnInvoice)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.DisplayName)
                    .ToListAsync();

                return Result<List<ImportFieldSetting>>.Success(data, "Qaimədə görünən dinamik sahələr yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<ImportFieldSetting>>.Fail($"Qaimə dinamik sahələri yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<List<InvoiceDynamicFieldValue>>> GetInvoiceFieldValuesAsync(
            int invoiceId,
            bool includePassive = false)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<List<InvoiceDynamicFieldValue>>.Fail("Qaimə düzgün seçilməyib.");

                var query = _context.InvoiceDynamicFieldValues
                    .Where(x => x.InvoiceId == invoiceId)
                    .AsQueryable();

                if (!includePassive)
                    query = query.Where(x => x.IsActive);

                var data = await query
                    .OrderBy(x => x.FieldName)
                    .ToListAsync();

                return Result<List<InvoiceDynamicFieldValue>>.Success(data, "Qaimə dinamik sahələri yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceDynamicFieldValue>>.Fail($"Qaimə dinamik sahələri yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceDynamicFieldValue>> GetInvoiceFieldValueAsync(
            int invoiceId,
            string fieldKey)
        {
            try
            {
                fieldKey = NormalizeKey(fieldKey);

                if (invoiceId <= 0)
                    return Result<InvoiceDynamicFieldValue>.Fail("Qaimə düzgün seçilməyib.");

                if (string.IsNullOrWhiteSpace(fieldKey))
                    return Result<InvoiceDynamicFieldValue>.Fail("Sahə açarı boş ola bilməz.");

                var value = await _context.InvoiceDynamicFieldValues
                    .FirstOrDefaultAsync(x =>
                        x.InvoiceId == invoiceId &&
                        x.FieldKey == fieldKey &&
                        x.IsActive);

                if (value == null)
                    return Result<InvoiceDynamicFieldValue>.Fail("Dinamik sahə dəyəri tapılmadı.");

                return Result<InvoiceDynamicFieldValue>.Success(value, "Dinamik sahə dəyəri yükləndi.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceDynamicFieldValue>.Fail($"Dinamik sahə dəyəri yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result<InvoiceDynamicFieldValue>> SetInvoiceFieldValueAsync(
            int invoiceId,
            string fieldKey,
            string? value,
            string? fieldName = null,
            FieldDataType? fieldType = null,
            string? note = null,
            bool saveChanges = true)
        {
            try
            {
                fieldKey = NormalizeKey(fieldKey);

                if (invoiceId <= 0)
                    return Result<InvoiceDynamicFieldValue>.Fail("Qaimə düzgün seçilməyib.");

                if (string.IsNullOrWhiteSpace(fieldKey))
                    return Result<InvoiceDynamicFieldValue>.Fail("Sahə açarı boş ola bilməz.");

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<InvoiceDynamicFieldValue>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<InvoiceDynamicFieldValue>.Fail("Yalnız draft qaimədə dinamik sahə dəyişmək olar.");

                if (invoice.IsLocked)
                    return Result<InvoiceDynamicFieldValue>.Fail("Qaimə kilidlidir. Dinamik sahə dəyişmək olmaz.");

                var setting = await _context.ImportFieldSettings
                    .FirstOrDefaultAsync(x => x.FieldKey == fieldKey && x.IsActive);

                var finalFieldName = !string.IsNullOrWhiteSpace(fieldName)
                    ? fieldName.Trim()
                    : setting?.DisplayName ?? fieldKey;

                var finalFieldType = fieldType ?? setting?.FieldType ?? FieldDataType.Text;

                var normalizedValueResult = NormalizeValue(value, finalFieldType);
                if (!normalizedValueResult.IsSuccess)
                    return Result<InvoiceDynamicFieldValue>.Fail(normalizedValueResult.Message);

                if (setting != null && setting.IsRequired && string.IsNullOrWhiteSpace(normalizedValueResult.Data))
                    return Result<InvoiceDynamicFieldValue>.Fail($"{setting.DisplayName} sahəsi məcburidir.");

                var dynamicValue = await _context.InvoiceDynamicFieldValues
                    .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.FieldKey == fieldKey);

                if (dynamicValue == null)
                {
                    dynamicValue = new InvoiceDynamicFieldValue
                    {
                        InvoiceId = invoiceId,
                        FieldKey = fieldKey,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.InvoiceDynamicFieldValues.AddAsync(dynamicValue);
                }

                dynamicValue.FieldName = finalFieldName;
                dynamicValue.FieldType = finalFieldType;
                dynamicValue.Value = normalizedValueResult.Data;
                dynamicValue.Note = NormalizeNullableText(note);
                dynamicValue.IsActive = true;
                dynamicValue.UpdatedAt = DateTime.Now;

                invoice.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<InvoiceDynamicFieldValue>.Success(dynamicValue, "Dinamik sahə yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceDynamicFieldValue>.Fail($"Dinamik sahə yadda saxlanılmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SetInvoiceFieldValuesAsync(
            int invoiceId,
            Dictionary<string, string?> values,
            bool saveChanges = true)
        {
            try
            {
                if (values == null || values.Count == 0)
                    return Result<bool>.Fail("Yadda saxlanılacaq dinamik sahə yoxdur.");

                foreach (var item in values)
                {
                    var result = await SetInvoiceFieldValueAsync(
                        invoiceId: invoiceId,
                        fieldKey: item.Key,
                        value: item.Value,
                        saveChanges: false);

                    if (!result.IsSuccess)
                        return Result<bool>.Fail(result.Message);
                }

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Dinamik sahələr yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Dinamik sahələr yadda saxlanılmadı: {ex.Message}");
            }
        }

        public async Task<Result<List<InvoiceDynamicFieldValue>>> EnsureDefaultFieldsForInvoiceAsync(
            int invoiceId,
            bool saveChanges = true)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<List<InvoiceDynamicFieldValue>>.Fail("Qaimə düzgün seçilməyib.");

                var invoice = await _context.Invoices
                    .Include(x => x.DynamicFieldValues)
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<List<InvoiceDynamicFieldValue>>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<List<InvoiceDynamicFieldValue>>.Fail("Yalnız draft qaimə üçün dinamik sahə yaradıla bilər.");

                if (invoice.IsLocked)
                    return Result<List<InvoiceDynamicFieldValue>>.Fail("Qaimə kilidlidir. Dinamik sahə yaradıla bilməz.");

                var settings = await _context.ImportFieldSettings
                    .Where(x => x.IsActive && x.IsVisible && x.ShowOnInvoice)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                var createdOrUpdated = new List<InvoiceDynamicFieldValue>();

                foreach (var setting in settings)
                {
                    var key = NormalizeKey(setting.FieldKey);

                    var existing = invoice.DynamicFieldValues
                        .FirstOrDefault(x => x.FieldKey == key);

                    if (existing == null)
                    {
                        existing = new InvoiceDynamicFieldValue
                        {
                            InvoiceId = invoiceId,
                            FieldKey = key,
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        };

                        await _context.InvoiceDynamicFieldValues.AddAsync(existing);
                    }

                    existing.FieldName = setting.DisplayName;
                    existing.FieldType = setting.FieldType;
                    existing.Value = existing.Value ?? setting.DefaultValue;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.Now;

                    createdOrUpdated.Add(existing);
                }

                invoice.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<List<InvoiceDynamicFieldValue>>.Success(createdOrUpdated, "Qaimə üçün default dinamik sahələr hazırlandı.");
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceDynamicFieldValue>>.Fail($"Default dinamik sahələr hazırlanmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ValidateRequiredFieldsAsync(int invoiceId)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<bool>.Fail("Qaimə düzgün seçilməyib.");

                var settings = await _context.ImportFieldSettings
                    .Where(x => x.IsActive && x.IsVisible && x.ShowOnInvoice && x.IsRequired)
                    .ToListAsync();

                if (!settings.Any())
                    return Result<bool>.Success(true, "Məcburi dinamik sahə yoxdur.");

                var values = await _context.InvoiceDynamicFieldValues
                    .Where(x => x.InvoiceId == invoiceId && x.IsActive)
                    .ToListAsync();

                foreach (var setting in settings)
                {
                    var key = NormalizeKey(setting.FieldKey);

                    var value = values.FirstOrDefault(x => x.FieldKey == key);

                    if (value == null || string.IsNullOrWhiteSpace(value.Value))
                        return Result<bool>.Fail($"{setting.DisplayName} sahəsi məcburidir.");
                }

                return Result<bool>.Success(true, "Məcburi dinamik sahələr düzgündür.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Dinamik sahələr yoxlanılmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateInvoiceFieldValueAsync(
            int invoiceId,
            string fieldKey,
            bool saveChanges = true)
        {
            try
            {
                fieldKey = NormalizeKey(fieldKey);

                if (invoiceId <= 0)
                    return Result<bool>.Fail("Qaimə düzgün seçilməyib.");

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<bool>.Fail("Yalnız draft qaimədə dinamik sahə silmək olar.");

                if (invoice.IsLocked)
                    return Result<bool>.Fail("Qaimə kilidlidir. Dinamik sahə silmək olmaz.");

                var value = await _context.InvoiceDynamicFieldValues
                    .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.FieldKey == fieldKey && x.IsActive);

                if (value == null)
                    return Result<bool>.Fail("Dinamik sahə dəyəri tapılmadı.");

                value.IsActive = false;
                value.UpdatedAt = DateTime.Now;

                invoice.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Dinamik sahə passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Dinamik sahə passiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ClearInvoiceFieldValuesAsync(
            int invoiceId,
            bool saveChanges = true)
        {
            try
            {
                if (invoiceId <= 0)
                    return Result<bool>.Fail("Qaimə düzgün seçilməyib.");

                var invoice = await _context.Invoices
                    .Include(x => x.DynamicFieldValues.Where(f => f.IsActive))
                    .FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsActive);

                if (invoice == null)
                    return Result<bool>.Fail("Qaimə tapılmadı.");

                if (invoice.Status != InvoiceStatus.Draft)
                    return Result<bool>.Fail("Yalnız draft qaimədə dinamik sahələr təmizlənə bilər.");

                if (invoice.IsLocked)
                    return Result<bool>.Fail("Qaimə kilidlidir. Dinamik sahələr təmizlənə bilməz.");

                foreach (var field in invoice.DynamicFieldValues.Where(x => x.IsActive))
                {
                    field.IsActive = false;
                    field.UpdatedAt = DateTime.Now;
                }

                invoice.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Qaimənin dinamik sahələri təmizləndi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Qaimənin dinamik sahələri təmizlənmədi: {ex.Message}");
            }
        }

        public async Task<Result<ImportFieldSetting>> CreateOrUpdateFieldSettingAsync(
            string fieldKey,
            string displayName,
            FieldDataType fieldType = FieldDataType.Text,
            bool isVisible = true,
            bool isRequired = false,
            bool showOnInvoice = true,
            string? optionsJson = null,
            string? defaultValue = null,
            string? placeholder = null,
            int sortOrder = 0,
            bool saveChanges = true)
        {
            try
            {
                fieldKey = NormalizeKey(fieldKey);

                if (string.IsNullOrWhiteSpace(fieldKey))
                    return Result<ImportFieldSetting>.Fail("Sahə açarı boş ola bilməz.");

                if (string.IsNullOrWhiteSpace(displayName))
                    return Result<ImportFieldSetting>.Fail("Sahə adı boş ola bilməz.");

                var setting = await _context.ImportFieldSettings
                    .FirstOrDefaultAsync(x => x.FieldKey == fieldKey);

                if (setting == null)
                {
                    setting = new ImportFieldSetting
                    {
                        FieldKey = fieldKey,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _context.ImportFieldSettings.AddAsync(setting);
                }

                setting.DisplayName = displayName.Trim();
                setting.FieldType = fieldType;
                setting.IsVisible = isVisible;
                setting.IsRequired = isRequired;
                setting.ShowOnInvoice = showOnInvoice;
                setting.OptionsJson = NormalizeNullableText(optionsJson);
                setting.DefaultValue = NormalizeNullableText(defaultValue);
                setting.Placeholder = NormalizeNullableText(placeholder);
                setting.SortOrder = sortOrder;
                setting.IsActive = true;
                setting.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<ImportFieldSetting>.Success(setting, "Dinamik sahə ayarı yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                return Result<ImportFieldSetting>.Fail($"Dinamik sahə ayarı yadda saxlanılmadı: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateFieldSettingAsync(string fieldKey, bool saveChanges = true)
        {
            try
            {
                fieldKey = NormalizeKey(fieldKey);

                var setting = await _context.ImportFieldSettings
                    .FirstOrDefaultAsync(x => x.FieldKey == fieldKey && x.IsActive);

                if (setting == null)
                    return Result<bool>.Fail("Dinamik sahə ayarı tapılmadı.");

                setting.IsActive = false;
                setting.IsVisible = false;
                setting.UpdatedAt = DateTime.Now;

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Dinamik sahə ayarı passiv edildi.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Dinamik sahə ayarı passiv edilmədi: {ex.Message}");
            }
        }

        public async Task<Result<bool>> EnsureDefaultImportFieldSettingsAsync(bool saveChanges = true)
        {
            try
            {
                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "DeclarationNumber",
                    displayName: "Gömrük bəyannamə №",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    placeholder: "Məs: AB123456",
                    sortOrder: 1,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "ImportDate",
                    displayName: "İdxal tarixi",
                    fieldType: FieldDataType.Date,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    sortOrder: 2,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "OriginCountry",
                    displayName: "Mənşə ölkəsi",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    placeholder: "Məs: Türkiyə",
                    sortOrder: 3,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "CustomsPoint",
                    displayName: "Gömrük postu",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    sortOrder: 4,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "Currency",
                    displayName: "Valyuta",
                    fieldType: FieldDataType.Dropdown,
                    isVisible: true,
                    isRequired: true,
                    showOnInvoice: true,
                    optionsJson: "AZN,USD,EUR,TRY,RUB",
                    defaultValue: "AZN",
                    sortOrder: 5,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "ExchangeRate",
                    displayName: "Məzənnə",
                    fieldType: FieldDataType.Number,
                    isVisible: true,
                    isRequired: true,
                    showOnInvoice: true,
                    defaultValue: "1",
                    sortOrder: 6,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "ForeignInvoiceNumber",
                    displayName: "Xarici qaimə №",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    sortOrder: 7,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "TransportDocumentNumber",
                    displayName: "Daşıma sənədi №",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    sortOrder: 8,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "VehicleNumber",
                    displayName: "Maşın nömrəsi",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    placeholder: "Məs: 99-AA-999",
                    sortOrder: 9,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "DriverName",
                    displayName: "Sürücü adı",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    sortOrder: 10,
                    saveChanges: false);

                await CreateOrUpdateFieldSettingAsync(
                    fieldKey: "BrokerName",
                    displayName: "Broker adı",
                    fieldType: FieldDataType.Text,
                    isVisible: true,
                    isRequired: false,
                    showOnInvoice: true,
                    sortOrder: 11,
                    saveChanges: false);

                if (saveChanges)
                    await _context.SaveChangesAsync();

                return Result<bool>.Success(true, "Default dinamik sahə ayarları hazırlandı.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Default dinamik sahə ayarları hazırlanmadı: {ex.Message}");
            }
        }

        private static Result<string?> NormalizeValue(string? value, FieldDataType fieldType)
        {
            value = NormalizeNullableText(value);

            if (string.IsNullOrWhiteSpace(value))
                return Result<string?>.Success(null);

            switch (fieldType)
            {
                case FieldDataType.Number:
                    if (!decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                        return Result<string?>.Fail("Rəqəm tipli sahəyə düzgün ədəd daxil edilməlidir.");

                    return Result<string?>.Success(number.ToString("0.####", CultureInfo.InvariantCulture));

                case FieldDataType.Date:
                    if (!DateTime.TryParse(value, out var date))
                        return Result<string?>.Fail("Tarix tipli sahəyə düzgün tarix daxil edilməlidir.");

                    return Result<string?>.Success(date.ToString("yyyy-MM-dd"));

                case FieldDataType.Checkbox:
                    var lower = value.ToLower();

                    if (lower == "true" || lower == "1" || lower == "bəli" || lower == "beli" || lower == "yes")
                        return Result<string?>.Success("true");

                    if (lower == "false" || lower == "0" || lower == "xeyr" || lower == "no")
                        return Result<string?>.Success("false");

                    return Result<string?>.Fail("Checkbox sahəsi üçün dəyər true/false olmalıdır.");

                default:
                    return Result<string?>.Success(value);
            }
        }

        private static string NormalizeKey(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string? NormalizeNullableText(string? value)
        {
            value = value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}