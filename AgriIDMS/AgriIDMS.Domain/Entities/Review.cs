using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int OrderDetailId { get; set; }
        public OrderDetail OrderDetail { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public int Rating { get; set; } // 1-5

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;
    }
}
