using Anbar.Services;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class InvoiceSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public InvoiceSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;
        }

        public void Bind()
        {
            InvoicePrefixText.Text = _model.InvoiceSetting.InvoicePrefix;

            LockConfirmedInvoiceCheck.IsChecked = _model.InvoiceSetting.LockConfirmedInvoice;
            RequireReturnReasonCheck.IsChecked = _model.InvoiceSetting.RequireReturnReason;
            RequireShelfSelectionCheck.IsChecked = _model.InvoiceSetting.RequireShelfSelection;
            RequireBatchReturnCheck.IsChecked = _model.InvoiceSetting.RequireBatchSelectionForReturn;
            CopyBarcodeCheck.IsChecked = _model.InvoiceSetting.CopyProductBarcodeToInvoiceItem;
        }

        public void ApplyChanges()
        {
            _model.InvoiceSetting.InvoicePrefix = string.IsNullOrWhiteSpace(InvoicePrefixText.Text)
                ? "QAI"
                : InvoicePrefixText.Text.Trim().ToUpper();

            _model.AppSetting.InvoicePrefix = _model.InvoiceSetting.InvoicePrefix;

            _model.InvoiceSetting.LockConfirmedInvoice = LockConfirmedInvoiceCheck.IsChecked == true;
            _model.InvoiceSetting.RequireReturnReason = RequireReturnReasonCheck.IsChecked == true;
            _model.InvoiceSetting.RequireShelfSelection = RequireShelfSelectionCheck.IsChecked == true;
            _model.InvoiceSetting.RequireBatchSelectionForReturn = RequireBatchReturnCheck.IsChecked == true;
            _model.InvoiceSetting.CopyProductBarcodeToInvoiceItem = CopyBarcodeCheck.IsChecked == true;
        }
    }
}