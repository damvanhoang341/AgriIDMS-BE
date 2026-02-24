using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Lot
    {
        public int Id { get; set; }
        public string LotCode { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int GoodsReceiptDetailId { get; set; }
        public GoodsReceiptDetail GoodsReceiptDetail { get; set; } = null!;

        public decimal TotalQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }

        public LotStatus Status { get; set; } = LotStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Box> Boxes { get; set; }
            = new List<Box>();
    }
}
