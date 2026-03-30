using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum StockCheckType
    {
        Full = 1,
        Cycle = 2,
        Spot = 3
    }

    public enum StockCheckStatus
    {
        Draft = 1,
        InProgress = 2,
        Counted = 3,
        Approved = 4,
        Rejected = 5
    }

    public enum VarianceType
    {
        Match = 1,
        Shortage = 2,
        Excess = 3
    }

    /// <summary>
    /// Nguyên nhân chênh lệch khi kiểm kê.
    /// Chỉ bắt buộc với dòng thiếu (Shortage).
    /// </summary>
    public enum VarianceReason
    {
        None = 0,
        Damaged = 1,
        Loss = 2,
        MeasurementError = 3,
        Other = 4
    }
}
