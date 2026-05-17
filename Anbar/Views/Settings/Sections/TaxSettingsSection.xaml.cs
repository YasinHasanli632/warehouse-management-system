using Anbar.Entities.Enum;
using Anbar.Services;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class TaxSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public TaxSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;

            TaxRegimeCombo.ItemsSource = Enum.GetValues(typeof(TaxRegime));
        }

        public void Bind()
        {
            TaxRegimeCombo.SelectedItem = _model.TaxSetting.TaxRegime;
            EnableVatCheck.IsChecked = _model.TaxSetting.EnableVAT;
            VatPercentText.Text = _model.TaxSetting.VATPercent.ToString(CultureInfo.InvariantCulture);
            PurchasePricesIncludeVatCheck.IsChecked = _model.TaxSetting.PurchasePricesIncludeVATByDefault;
            VatRecoverableCheck.IsChecked = _model.TaxSetting.VATRecoverableByDefault;

            EnableProfitTaxCheck.IsChecked = _model.TaxSetting.EnableProfitTax;
            ProfitTaxPercentText.Text = _model.TaxSetting.ProfitTaxPercent.ToString(CultureInfo.InvariantCulture);

            EnableSimplifiedTaxCheck.IsChecked = _model.TaxSetting.EnableSimplifiedTax;
            SimplifiedTaxPercentText.Text = _model.TaxSetting.SimplifiedTaxPercent.ToString(CultureInfo.InvariantCulture);

            IncludeImportVatInCostCheck.IsChecked = _model.TaxSetting.IncludeImportVATInCost;
            IncludeCustomsDutyInCostCheck.IsChecked = _model.TaxSetting.IncludeCustomsDutyInCost;
            IncludeExciseInCostCheck.IsChecked = _model.TaxSetting.IncludeExciseInCost;
        }

        public void ApplyChanges()
        {
            _model.TaxSetting.TaxRegime = TaxRegimeCombo.SelectedItem is TaxRegime regime
                ? regime
                : TaxRegime.NoTax;

            _model.TaxSetting.EnableVAT = EnableVatCheck.IsChecked == true;
            _model.TaxSetting.VATPercent = ParseDecimal(VatPercentText.Text, 18);
            _model.TaxSetting.PurchasePricesIncludeVATByDefault = PurchasePricesIncludeVatCheck.IsChecked == true;
            _model.TaxSetting.VATRecoverableByDefault = VatRecoverableCheck.IsChecked == true;

            _model.TaxSetting.EnableProfitTax = EnableProfitTaxCheck.IsChecked == true;
            _model.TaxSetting.ProfitTaxPercent = ParseDecimal(ProfitTaxPercentText.Text, 20);

            _model.TaxSetting.EnableSimplifiedTax = EnableSimplifiedTaxCheck.IsChecked == true;
            _model.TaxSetting.SimplifiedTaxPercent = ParseDecimal(SimplifiedTaxPercentText.Text, 2);

            _model.TaxSetting.IncludeImportVATInCost = IncludeImportVatInCostCheck.IsChecked == true;
            _model.TaxSetting.IncludeCustomsDutyInCost = IncludeCustomsDutyInCostCheck.IsChecked == true;
            _model.TaxSetting.IncludeExciseInCost = IncludeExciseInCostCheck.IsChecked == true;
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