using Anbar.Services;
using System.Windows.Controls;

namespace Anbar.Views.Settings.Sections
{
    public partial class StockSettingsSection : UserControl, ISettingsSection
    {
        private readonly SettingsUiModel _model;

        public StockSettingsSection(SettingsUiModel model)
        {
            InitializeComponent();
            _model = model;
        }

        public void Bind()
        {
            EnableFifoCheck.IsChecked = _model.StockSetting.EnableFIFO;
            PreventNegativeStockCheck.IsChecked = _model.StockSetting.PreventNegativeStock;
            CheckShelfCapacityCheck.IsChecked = _model.StockSetting.CheckShelfCapacity;
            BlockPassiveProductCheck.IsChecked = _model.StockSetting.BlockPassiveProductInInvoice;
            AutoCreateBatchCheck.IsChecked = _model.StockSetting.AutoCreateBatchOnStockIn;
        }

        public void ApplyChanges()
        {
            _model.StockSetting.EnableFIFO = EnableFifoCheck.IsChecked == true;
            _model.StockSetting.PreventNegativeStock = PreventNegativeStockCheck.IsChecked == true;
            _model.StockSetting.CheckShelfCapacity = CheckShelfCapacityCheck.IsChecked == true;
            _model.StockSetting.BlockPassiveProductInInvoice = BlockPassiveProductCheck.IsChecked == true;
            _model.StockSetting.AutoCreateBatchOnStockIn = AutoCreateBatchCheck.IsChecked == true;

            // YENI:
            // Legacy AppSetting ilə uyğunluq.
            // PreventNegativeStock true-dursa AllowNegativeStock false olmalıdır.
            _model.AppSetting.AllowNegativeStock = _model.StockSetting.PreventNegativeStock != true;
        }
    }
}