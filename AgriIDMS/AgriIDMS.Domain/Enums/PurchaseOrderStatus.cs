using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum PurchaseOrderStatus
    {
        //Draft = 0,        // Đang tạo, chưa gửi
        Pending = 0,      // Đã gửi NCC, chờ xác nhận
        Approved = 1,     // Đã duyệt
        //PartiallyReceived = 3, // Đã nhận 1 phần
        Completed = 2,    // Đã nhận đủ
        Cancelled = 3     // Đã hủy
    }
}
