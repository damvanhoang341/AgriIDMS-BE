using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.PurchaseOrder
{
    public class CreatePurchaseOrderRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreatePurchaseOrderDetailRequest> Details { get; set; }
    }

    public class CreatePurchaseOrderDetailRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ProductVariantId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal OrderedWeight { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }

    public class PurchaseOrderResponse
    {
        public int Id { get; set; }

        public string OrderCode { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; }

        public string Status { get; set; }

        public DateTime OrderDate { get; set; }

        public List<PurchaseOrderDetailResponse> Details { get; set; }
    }

    public class PurchaseOrderDetailResponse
    {
        /// <summary>Id dòng đơn mua (dùng làm PurchaseOrderDetailId khi thêm chi tiết phiếu nhập).</summary>
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public string ProductName { get; set; }

        public decimal OrderedWeight { get; set; }

        public decimal UnitPrice { get; set; }
    }
}
