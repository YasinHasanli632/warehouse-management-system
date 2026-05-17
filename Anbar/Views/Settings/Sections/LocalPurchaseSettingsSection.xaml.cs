using Anbar.Entities.Enum;
using Anbar.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class LocalPurchaseSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public LocalPurchaseSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;

            DefaultExpenseAllocationMethodCombo.ItemsSource = Enum.GetValues(typeof(CostAllocationMethod));
        }

        public void Bind()
        {
            EnableLocalPurchaseInvoiceCheck.IsChecked = GetBool("EnableLocalPurchaseInvoice", true);
            EnableAdditionalExpensesCheck.IsChecked = GetBool("EnableAdditionalExpenses", true);
            AutoCalculateCostOnConfirmCheck.IsChecked = GetBool("AutoCalculateCostOnConfirm", true);
            RecalculateBatchCostWhenExpenseChangesCheck.IsChecked = GetBool("RecalculateBatchCostWhenExpenseChanges", true);
            IgnoreZeroAmountExpensesCheck.IsChecked = GetBool("IgnoreZeroAmountExpenses", true);

            EnableVatCalculationCheck.IsChecked = GetBool("EnableVATCalculation", false);
            VatPercentText.Text = GetValue("VATPercent", "18");
            PurchaseVatIncludedInPriceCheck.IsChecked = GetBool("PurchaseVATIncludedInPrice", false);
            PurchaseVatIncludedInCostCheck.IsChecked = GetBool("PurchaseVATIncludedInCost", false);
            ShowVatSeparatelyInReportsCheck.IsChecked = GetBool("ShowVATSeparatelyInReports", true);
            HidePassiveExpensesCheck.IsChecked = GetBool("HidePassiveExpenses", true);

            DefaultExpenseAllocationMethodCombo.SelectedItem = GetAllocationMethod();

            ExpenseTypesGrid.ItemsSource = _model.ExpenseTypes
                .Where(x => x.IsActive && x.UseForStockIn && !x.UseForImport)
                .ToList();
        }

        public void ApplyChanges()
        {
            SetValue("EnableLocalPurchaseInvoice", BoolValue(EnableLocalPurchaseInvoiceCheck));
            SetValue("EnableAdditionalExpenses", BoolValue(EnableAdditionalExpensesCheck));
            SetValue("AutoCalculateCostOnConfirm", BoolValue(AutoCalculateCostOnConfirmCheck));
            SetValue("RecalculateBatchCostWhenExpenseChanges", BoolValue(RecalculateBatchCostWhenExpenseChangesCheck));
            SetValue("IgnoreZeroAmountExpenses", BoolValue(IgnoreZeroAmountExpensesCheck));

            SetValue("EnableVATCalculation", BoolValue(EnableVatCalculationCheck));
            SetValue("VATPercent", ParseDecimal(VatPercentText.Text, 18).ToString(CultureInfo.InvariantCulture));
            SetValue("PurchaseVATIncludedInPrice", BoolValue(PurchaseVatIncludedInPriceCheck));
            SetValue("PurchaseVATIncludedInCost", BoolValue(PurchaseVatIncludedInCostCheck));
            SetValue("ShowVATSeparatelyInReports", BoolValue(ShowVatSeparatelyInReportsCheck));
            SetValue("HidePassiveExpenses", BoolValue(HidePassiveExpensesCheck));

            SetValue("DefaultExpenseAllocationMethod",
                DefaultExpenseAllocationMethodCombo.SelectedItem?.ToString() ?? CostAllocationMethod.ByQuantity.ToString());
        }

        private string GetValue(string key, string defaultValue)
        {
            var item = _model.LocalPurchaseValues
                .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(item?.Value) ? defaultValue : item.Value;
        }

        private bool GetBool(string key, bool defaultValue)
        {
            var value = GetValue(key, defaultValue.ToString());

            if (bool.TryParse(value, out var parsed))
                return parsed;

            if (value == "1")
                return true;

            if (value == "0")
                return false;

            return defaultValue;
        }

        private CostAllocationMethod GetAllocationMethod()
        {
            var value = GetValue("DefaultExpenseAllocationMethod", CostAllocationMethod.ByQuantity.ToString());

            return Enum.TryParse<CostAllocationMethod>(value, true, out var parsed)
                ? parsed
                : CostAllocationMethod.ByQuantity;
        }

        private void SetValue(string key, string value)
        {
            var item = _model.LocalPurchaseValues
                .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (item != null)
            {
                item.Value = value;
                return;
            }

            _model.LocalPurchaseValues.Add(new Anbar.Entities.LocalPurchaseSettingValue
            {
                Key = key,
                Value = value,
                ValueType = "String",
                DisplayName = key,
                SortOrder = _model.LocalPurchaseValues.Any()
                    ? _model.LocalPurchaseValues.Max(x => x.SortOrder) + 1
                    : 1,
                IsSystem = false,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
        }

        private static string BoolValue(CheckBox checkBox)
        {
            return checkBox.IsChecked == true ? "true" : "false";
        }

        private static decimal ParseDecimal(string? value, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (decimal.TryParse(value, out var parsed))
                return parsed;

            var normalized = value.Replace(",", ".");

            return decimal.TryParse(
                normalized,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out parsed)
                ? parsed
                : defaultValue;
        }
    }
}