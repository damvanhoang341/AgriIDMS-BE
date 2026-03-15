using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public TitleWarehouse TitleWarehouse { get; set; }
        /// <summary>Số giờ tối thiểu box phải nằm trong kho lạnh trước khi được xuất. Chỉ áp dụng khi TitleWarehouse = Cold. Mặc định 48h.</summary>
        public decimal? MinColdStorageHours { get; set; }
        /// <summary>Định mức tối thiểu (kg) cho mỗi phiếu nhập vào kho này. Null = không bắt buộc.</summary>
        public decimal? MinReceiptWeight { get; set; }
        public ICollection<Zone> Zones { get; set; } = new List<Zone>();
        public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
    }
}
