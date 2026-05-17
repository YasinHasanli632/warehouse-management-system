using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Maya dəyəri və xərc paylaşma ayarlarını saxlayır.
    public class CostSetting : BaseEntity
    {
        public bool IncludeExpensesInStockCost { get; set; } = true;

        public CostAllocationMethod DefaultAllocationMethod { get; set; } = CostAllocationMethod.ByAmount;

        public bool SuggestSalePrice { get; set; } = true;

        public decimal MinimumMarginPercent { get; set; } = 0;

        // YENI:
        // Qaimə təsdiqlənəndə maya dəyəri avtomatik hesablansın.
        public bool AutoCalculateCostOnConfirm { get; set; } = true;

        // YENI:
        // Xərc dəyişəndə maya dəyəri yenidən hesablansın.
        public bool RecalculateCostWhenExpenseChanges { get; set; } = true;

        // YENI:
        // 0 dəyərli xərclər maya hesabına daxil edilməsin.
        public bool ExcludeZeroAmountExpenses { get; set; } = true;

        // YENI:
        // Təsdiqlənmiş qaimənin maya dəyəri kilidlənsin.
        public bool LockCostAfterConfirm { get; set; } = true;
    }
}
