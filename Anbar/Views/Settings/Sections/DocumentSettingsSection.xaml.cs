using Anbar.Services;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class DocumentSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public DocumentSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;
        }

        public void Bind()
        {
        }

        public void ApplyChanges()
        {
        }
    }
}