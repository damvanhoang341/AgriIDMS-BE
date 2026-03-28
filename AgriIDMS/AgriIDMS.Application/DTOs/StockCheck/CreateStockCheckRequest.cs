using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class CreateStockCheckRequest
    {
        [Required]
        public int WarehouseId { get; set; }

        [Required]
        public StockCheckType CheckType { get; set; }

        /// <summary>Bắt buộc khi CheckType = Spot: danh sách BoxId cần kiểm kê.</summary>
        public List<int>? BoxIds { get; set; }

        /// <summary>Tuỳ chọn khi CheckType = Cycle: lọc theo khu (Zone).</summary>
        public int? ZoneId { get; set; }

        /// <summary>Tuỳ chọn khi CheckType = Cycle: lọc theo dãy/kệ (Rack).</summary>
        public int? RackId { get; set; }

        /// <summary>Tuỳ chọn khi CheckType = Cycle: lọc theo ô (Slot).</summary>
        public int? SlotId { get; set; }
    }
}
