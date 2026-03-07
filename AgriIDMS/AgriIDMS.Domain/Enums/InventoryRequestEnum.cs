using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum InventoryRequestType
    {
        TransferBetweenWarehouses = 0,   // chuyển box giữa 2 kho
        StockAdjustment = 1,             // điều chỉnh tồn kho
        CancelExport = 2,                // hủy phiếu xuất
        CancelImport = 3,                // hủy phiếu nhập
        ImportCorrection = 4,            // chỉnh sửa dữ liệu nhập kho
        DamageReport = 5                 // báo hỏng hàng
    }

    public enum InventoryRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Completed = 3
    }

    public enum InventoryReferenceType
    {
        Box = 0,
        Lot = 1,
        Order = 2,
        ExportReceipt = 3,
        StockCheck = 4,
        StockCheckDetail = 5
    }
}
