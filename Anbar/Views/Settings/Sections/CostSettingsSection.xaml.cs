using Anbar.Entities.Enum;
using Anbar.Services;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class CostSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public CostSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;

            CostAllocationCombo.ItemsSource = Enum.GetValues(typeof(CostAllocationMethod));
        }

        public void Bind()
        {
            IncludeExpensesInCostCheck.IsChecked = _model.CostSetting.IncludeExpensesInStockCost;
            CostAllocationCombo.SelectedItem = _model.CostSetting.DefaultAllocationMethod;
            SuggestSalePriceCheck.IsChecked = _model.CostSetting.SuggestSalePrice;
            MinimumMarginText.Text = _model.CostSetting.MinimumMarginPercent.ToString(CultureInfo.InvariantCulture);
            AutoCalculateCostCheck.IsChecked = _model.CostSetting.AutoCalculateCostOnConfirm;
            RecalculateCostCheck.IsChecked = _model.CostSetting.RecalculateCostWhenExpenseChanges;
            ExcludeZeroExpensesCheck.IsChecked = _model.CostSetting.ExcludeZeroAmountExpenses;
            LockCostAfterConfirmCheck.IsChecked = _model.CostSetting.LockCostAfterConfirm;
        }

        public void ApplyChanges()
        {
            _model.CostSetting.IncludeExpensesInStockCost = IncludeExpensesInCostCheck.IsChecked == true;

            _model.CostSetting.DefaultAllocationMethod = CostAllocationCombo.SelectedItem is CostAllocationMethod method
                ? method
                : CostAllocationMethod.ByAmount;

            _model.CostSetting.SuggestSalePrice = SuggestSalePriceCheck.IsChecked == true;
            _model.CostSetting.MinimumMarginPercent = ParseDecimal(MinimumMarginText.Text, 0);
            _model.CostSetting.AutoCalculateCostOnConfirm = AutoCalculateCostCheck.IsChecked == true;
            _model.CostSetting.RecalculateCostWhenExpenseChanges = RecalculateCostCheck.IsChecked == true;
            _model.CostSetting.ExcludeZeroAmountExpenses = ExcludeZeroExpensesCheck.IsChecked == true;
            _model.CostSetting.LockCostAfterConfirm = LockCostAfterConfirmCheck.IsChecked == true;
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