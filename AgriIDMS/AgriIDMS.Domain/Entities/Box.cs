using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Box
    {
        public int Id { get; set; }
        public string BoxCode { get; set; } = null!;

        public int LotId { get; set; }
        public Lot Lot { get; set; } = null!;

        public decimal Weight { get; set; }
        /// <summary>Loại bao bì (thùng xốp, carton, bao lưới, sọt...). Khác với <see cref="IsPartial"/>.</summary>
        public BoxType BoxType { get; set; } = BoxType.Unknown;
        /// <summary>Box đầy hay lẻ (khối lượng theo quy cách).</summary>
        public bool IsPartial { get; set; }

        public int? SlotId { get; set; }
        public Slot? Slot { get; set; }
        /// <summary>Thời điểm box được đặt vào slot thuộc kho lạnh (lần đầu). Dùng để kiểm tra đủ thời gian lưu lạnh trước khi xuất.</summary>
        public DateTime? PlacedInColdAt { get; set; }
        public string? QRCode { get; set; }
        public BoxStatus Status { get; set; } = BoxStatus.Stored;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
        public ICollection<OrderAllocation> Allocations { get; set; } = new List<OrderAllocation>();
    }
}
