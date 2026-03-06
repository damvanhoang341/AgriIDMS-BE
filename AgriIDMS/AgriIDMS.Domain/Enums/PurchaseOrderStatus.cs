using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum PurchaseOrderStatus
    {
        Draft = 0,        // Đang tạo, chưa gửi
        Pending = 1,      // Đã gửi NCC, chờ xác nhận
        Approved = 2,     // Đã duyệt
        PartiallyReceived = 3, // Đã nhận 1 phần
        Completed = 4,    // Đã nhận đủ
        Cancelled = 5     // Đã hủy
    }
}
