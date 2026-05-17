using Anbar.Services;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class NumberingSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public NumberingSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;
        }

        public void Bind()
        {
            InvoicePrefixText.Text = _model.InvoiceSetting.InvoicePrefix;
            PreviewText.Text = $"{_model.InvoiceSetting.InvoicePrefix}-2026-00001";
        }

        public void ApplyChanges()
        {
            var prefix = string.IsNullOrWhiteSpace(InvoicePrefixText.Text)
                ? "QAI"
                : InvoicePrefixText.Text.Trim().ToUpper();

            _model.InvoiceSetting.InvoicePrefix = prefix;
            _model.AppSetting.InvoicePrefix = prefix;
        }
    }
}