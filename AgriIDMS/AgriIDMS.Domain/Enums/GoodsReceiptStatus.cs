using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum GoodsReceiptStatus
    {
        Draft = 0,
        WaitingQC = 1,
        QcFailed = 2,
        Approved = 3,
        Cancelled = 4
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
