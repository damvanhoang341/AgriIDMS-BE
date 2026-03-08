using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IGoodsReceiptDetailRepository
    {
        Task AddGoodsReceiptDetaiAsync(GoodsReceiptDetail entity);
        Task<GoodsReceiptDetail?> GetByIdAsync(int id);
        /// <summary>Tổng ReceivedWeight của các dòng GR (phiếu Draft hoặc PendingManagerApproval) cho một dòng PO.</summary>
        Task<decimal> GetTotalReceivedWeightForPurchaseOrderDetailInDraftOrPendingAsync(int purchaseOrderDetailId);
    }
}
