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
        [Required(ErrorMessage = "ProductVariantId không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId không hợp lệ")]
        public int ProductVariantId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Khối lượng đặt (OrderedWeight) phải lớn hơn 0")]
        public decimal OrderedWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá (UnitPrice) phải >= 0")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Dung sai (TolerancePercent) phải từ 0 đến 100")]
        public decimal TolerancePercent { get; set; } = 2;

        /// <summary>Ngày thu hoạch nông sản. Bắt buộc cho traceability và tính hạn sử dụng.</summary>
        [Required(ErrorMessage = "HarvestDate không được để trống")]
        public DateTime HarvestDate { get; set; }
    }

    public class UpdatePurchaseOrderRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "SupplierId không hợp lệ")]
        public int? SupplierId { get; set; }

        public List<UpdatePurchaseOrderDetailRequest>? Details { get; set; }
    }

    public class UpdatePurchaseOrderDetailRequest
    {
        /// <summary>Id dòng PO (0 hoặc null = thêm mới).</summary>
        public int? Id { get; set; }

        [Required(ErrorMessage = "ProductVariantId không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId không hợp lệ")]
        public int ProductVariantId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Khối lượng đặt (OrderedWeight) phải lớn hơn 0")]
        public decimal OrderedWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá (UnitPrice) phải >= 0")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Dung sai (TolerancePercent) phải từ 0 đến 100")]
        public decimal TolerancePercent { get; set; } = 2;

        /// <summary>Ngày thu hoạch nông sản. Bắt buộc khi tạo/sửa dòng PO.</summary>
        [Required(ErrorMessage = "HarvestDate không được để trống")]
        public DateTime HarvestDate { get; set; }
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

        public decimal TolerancePercent { get; set; }

        /// <summary>Khối lượng đã nhận (cập nhật khi phiếu nhập được Approved).</summary>
        public decimal ReceivedWeight { get; set; }

        /// <summary>Còn lại = OrderedWeight - ReceivedWeight.</summary>
        public decimal RemainingWeight => OrderedWeight - ReceivedWeight;

        /// <summary>Ngày thu hoạch dùng để tính hạn sử dụng Lot.</summary>
        public DateTime HarvestDate { get; set; }
    }
}
