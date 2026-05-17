using Anbar.Services;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class PaymentSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public PaymentSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;
        }

        public void Bind()
        {
            // Hələ backend-də ayrıca PaymentSetting entity yoxdur.
            // Placeholder olaraq UI görünür.
        }

        public void ApplyChanges()
        {
            // Hələ save ediləcək ayrıca payment setting yoxdur.
            // Yeni entity əlavə etmədən saxlamırıq.
        }
    }
}