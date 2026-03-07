using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    /// <summary>Luồng trạng thái phiếu nhập: Draft → Received → QCCompleted → Approved. Chỉ cập nhật tồn kho / PO.ReceivedWeight khi Approved.</summary>
    public enum GoodsReceiptStatus
    {
        Draft = 0,
        Received = 1,           // Đã nhập số liệu (cân xe / chi tiết)
        QCCompleted = 2,       // Đã QC toàn bộ dòng
        Approved = 3,
        Cancelled = 4,          // Hủy phiếu
        PendingManagerApproval = 5,  // Vượt dung sai, chờ Manager
        Rejected = 6,                 // Quản lý từ chối
    }

    public enum LotStatus
    {
        Active,
        Blocked,
        Expired
    }

    public enum BoxStatus
    {
        Stored,
        Reserved,
        Picking,
        Exported,
        Damaged,
        Expired
    }

    public enum QCResult
    {
        Pending = 0,
        Passed = 1,
        Failed = 2,
        Partial = 3
    }
}
