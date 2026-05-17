using Anbar.Entities.Enum;
using Anbar.Services;
using System;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class GeneralSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public GeneralSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;

            DefaultCurrencyCombo.ItemsSource = Enum.GetValues(typeof(CurrencyType));
        }

        public void Bind()
        {
            AppNameText.Text = _model.WarehouseSetting.AppName;
            CompanyNameText.Text = _model.WarehouseSetting.CompanyName ?? "";
            CompanyVoenText.Text = _model.WarehouseSetting.CompanyVoen ?? "";
            CompanyPhoneText.Text = _model.WarehouseSetting.CompanyPhone ?? "";
            CompanyAddressText.Text = _model.WarehouseSetting.CompanyAddress ?? "";
            DefaultCurrencyCombo.SelectedItem = _model.WarehouseSetting.DefaultCurrency;

            EnableCriticalStockWarningCheck.IsChecked = _model.AppSetting.EnableCriticalStockWarning;
            AllowNegativeStockCheck.IsChecked = _model.AppSetting.AllowNegativeStock;
            AutoShelfAssignCheck.IsChecked = _model.AppSetting.AutoShelfAssign;
        }

        public void ApplyChanges()
        {
            var appName = AppNameText.Text.Trim();

            _model.AppSetting.AppName = appName;
            _model.AppSetting.CompanyName = ToNull(CompanyNameText.Text);
            _model.AppSetting.CompanyVoen = ToNull(CompanyVoenText.Text);
            _model.AppSetting.CompanyPhone = ToNull(CompanyPhoneText.Text);
            _model.AppSetting.CompanyAddress = ToNull(CompanyAddressText.Text);
            _model.AppSetting.DefaultCurrency = DefaultCurrencyCombo.SelectedItem is CurrencyType appCurrency
                ? appCurrency
                : CurrencyType.AZN;
            _model.AppSetting.EnableCriticalStockWarning = EnableCriticalStockWarningCheck.IsChecked == true;
            _model.AppSetting.AllowNegativeStock = AllowNegativeStockCheck.IsChecked == true;
            _model.AppSetting.AutoShelfAssign = AutoShelfAssignCheck.IsChecked == true;

            _model.WarehouseSetting.AppName = appName;
            _model.WarehouseSetting.CompanyName = ToNull(CompanyNameText.Text);
            _model.WarehouseSetting.CompanyVoen = ToNull(CompanyVoenText.Text);
            _model.WarehouseSetting.CompanyPhone = ToNull(CompanyPhoneText.Text);
            _model.WarehouseSetting.CompanyAddress = ToNull(CompanyAddressText.Text);
            _model.WarehouseSetting.DefaultCurrency = DefaultCurrencyCombo.SelectedItem is CurrencyType warehouseCurrency
                ? warehouseCurrency
                : CurrencyType.AZN;
        }

        private static string? ToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}