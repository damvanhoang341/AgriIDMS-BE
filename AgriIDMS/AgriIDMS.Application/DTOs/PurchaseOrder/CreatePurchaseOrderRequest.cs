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

    public class CreateMultiSupplierPurchaseOrderRequest
    {
        [Required]
        [MinLength(1)]
        public List<CreateSupplierPlanRequest> SupplierPlans { get; set; } = new();
    }

    public class CreateSupplierPlanRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateSupplierPlanDetailRequest> Details { get; set; } = new();
    }

    public class CreateSupplierPlanDetailRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal OrderedWeight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPriceAtOrder { get; set; }

        [Required]
        public DateTime PriceDate { get; set; }

        [Range(0, 100)]
        public decimal TolerancePercent { get; set; } = 0;
    }

    public class CreatePurchaseOrderDetailRequest
    {
        [Required(ErrorMessage = "ProductId không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ")]
        public int ProductId { get; set; }

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

        [Required(ErrorMessage = "ProductId không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ")]
        public int ProductId { get; set; }

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

    public class PurchaseOrderGetAllResponse
    {
        public int Id { get; set; }

        public string OrderCode { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; }

        public string Status { get; set; }
        public string ProcurementMode { get; set; }

        public DateTime OrderDate { get; set; }
        public string? NameCreater { get; set; }

    }
    public class PurchaseOrderResponse
    {
        public int Id { get; set; }

        public string OrderCode { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; }

        public string Status { get; set; }
        public string ProcurementMode { get; set; }

        public DateTime OrderDate { get; set; }
        public string NameCreater { get; set; }

        public List<PurchaseOrderDetailResponse> Details { get; set; }
    }

    public class PurchaseOrderDetailResponse
    {
        /// <summary>Id dòng đơn mua (dùng làm PurchaseOrderDetailId khi thêm chi tiết phiếu nhập).</summary>
        public int Id { get; set; }

        public int ProductId { get; set; }

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
        public string? NameApprover { get; set; }
    }

    public class PurchaseOrderStructuredStatusDto
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class PurchaseOrderStructuredProcurementDto
    {
        public string Mode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class PurchaseOrderStructuredCreatedByDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PurchaseOrderStructuredSummaryDto
    {
        public int TotalSuppliers { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalOrderedWeight { get; set; }
        public decimal TotalEstimatedAmount { get; set; }
    }

    public class PurchaseOrderStructuredSupplierDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class PurchaseOrderStructuredSupplierPlanSummaryDto
    {
        public decimal TotalOrderedWeight { get; set; }
        public decimal TotalEstimatedAmount { get; set; }
    }

    public class PurchaseOrderStructuredLineDto
    {
        public int LineId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedWeight { get; set; }
        public decimal UnitPriceAtOrder { get; set; }
        public DateTime PriceDate { get; set; }
        public decimal LineAmount { get; set; }
    }

    public class PurchaseOrderStructuredSupplierPlanDto
    {
        public int SupplierPlanId { get; set; }
        public PurchaseOrderStructuredSupplierDto Supplier { get; set; } = new();
        public DateTime OrderDate { get; set; }
        public string? Notes { get; set; }
        public PurchaseOrderStructuredSupplierPlanSummaryDto Summary { get; set; } = new();
        public List<PurchaseOrderStructuredLineDto> Details { get; set; } = new();
    }

    public class PurchaseOrderStructuredResponse
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public PurchaseOrderStructuredStatusDto Status { get; set; } = new();
        public PurchaseOrderStructuredProcurementDto Procurement { get; set; } = new();
        public DateTime OrderDate { get; set; }
        public PurchaseOrderStructuredCreatedByDto CreatedBy { get; set; } = new();
        public PurchaseOrderStructuredSummaryDto Summary { get; set; } = new();
        public List<PurchaseOrderStructuredSupplierPlanDto> SupplierPlans { get; set; } = new();
    }
}
