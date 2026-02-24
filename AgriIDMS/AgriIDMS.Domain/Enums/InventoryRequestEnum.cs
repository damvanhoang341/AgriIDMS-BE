using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum InventoryRequestType
    {
        Transfer = 0,          // MoveBox
        Export = 1,
        StockAdjustment = 2,   // từ kiểm kê
        Cancel = 3,
        ImportCorrection = 4
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
