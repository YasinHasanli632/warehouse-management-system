using Anbar.Services;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class ImportSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public ImportSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;
        }

        public void Bind()
        {
            EnableImportInvoiceCheck.IsChecked = _model.ImportSetting.EnableImportInvoice;
            AutoOpenImportFieldsForForeignSupplierCheck.IsChecked = _model.ImportSetting.AutoOpenImportFieldsForForeignSupplier;
            RequireDeclarationNumberCheck.IsChecked = _model.ImportSetting.RequireDeclarationNumber;
            RequireExchangeRateCheck.IsChecked = _model.ImportSetting.RequireExchangeRate;
            UseInvoiceDateExchangeRateCheck.IsChecked = _model.ImportSetting.UseInvoiceDateExchangeRate;

            IncludeCustomsDutyInCostCheck.IsChecked = _model.ImportSetting.IncludeCustomsDutyInCost;
            IncludeBrokerFeeInCostCheck.IsChecked = _model.ImportSetting.IncludeBrokerFeeInCost;
            IncludeInsuranceInCostCheck.IsChecked = _model.ImportSetting.IncludeInsuranceInCost;
            IncludeTransportInCostCheck.IsChecked = _model.ImportSetting.IncludeTransportInCost;

            ImportFieldsGrid.ItemsSource = _model.ImportFieldSettings;
        }

        public void ApplyChanges()
        {
            _model.ImportSetting.EnableImportInvoice = EnableImportInvoiceCheck.IsChecked == true;
            _model.ImportSetting.AutoOpenImportFieldsForForeignSupplier = AutoOpenImportFieldsForForeignSupplierCheck.IsChecked == true;
            _model.ImportSetting.RequireDeclarationNumber = RequireDeclarationNumberCheck.IsChecked == true;
            _model.ImportSetting.RequireExchangeRate = RequireExchangeRateCheck.IsChecked == true;
            _model.ImportSetting.UseInvoiceDateExchangeRate = UseInvoiceDateExchangeRateCheck.IsChecked == true;

            _model.ImportSetting.IncludeCustomsDutyInCost = IncludeCustomsDutyInCostCheck.IsChecked == true;
            _model.ImportSetting.IncludeBrokerFeeInCost = IncludeBrokerFeeInCostCheck.IsChecked == true;
            _model.ImportSetting.IncludeInsuranceInCost = IncludeInsuranceInCostCheck.IsChecked == true;
            _model.ImportSetting.IncludeTransportInCost = IncludeTransportInCostCheck.IsChecked == true;

            ImportFieldsGrid.CommitEdit();
            ImportFieldsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }
}