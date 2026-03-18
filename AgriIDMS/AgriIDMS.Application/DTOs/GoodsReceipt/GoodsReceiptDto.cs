using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.GoodsReceipt
{
    public class CreateGoodsReceiptRequest
    {

        [Required(ErrorMessage = "WarehouseId không được để trống")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Biển số xe không được để trống")]
        [MaxLength(50, ErrorMessage = "Biển số xe tối đa 50 ký tự")]
        public string VehicleNumber { get; set; } = null!;

        [Required(ErrorMessage = "Tên tài xế không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên tài xế tối đa 100 ký tự")]
        public string DriverName { get; set; } = null!;

        [MaxLength(100, ErrorMessage = "Tên công ty vận chuyển tối đa 100 ký tự")]
        public string? TransportCompany { get; set; }

        [Required(ErrorMessage = "PurchaseOrderId không được để trống")]
        public int PurchaseOrderId { get; set; }

        /// <summary>Các dòng chi tiết nhập theo PO ngay khi tạo phiếu (tùy chọn). Nếu truyền lên sẽ được validate giống AddGoodsReceiptDetail.</summary>
        public List<CreateGoodsReceiptDetailLineRequest> Details { get; set; } = new();
    }

    /// <summary>Dòng chi tiết đi kèm khi tạo phiếu nhập.</summary>
    public class CreateGoodsReceiptDetailLineRequest
    {
        [Required(ErrorMessage = "PurchaseOrderDetailId không được để trống")]
        public int PurchaseOrderDetailId { get; set; }

        [Required(ErrorMessage = "ProductVariantId không được để trống")]
        public int ProductVariantId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "ReceivedWeight phải lớn hơn 0")]
        public decimal ReceivedWeight { get; set; }
    }

    /// <summary>
    /// Warehouse only sends PO line + received weight. ExpectedWeight and UnitPrice come from PO (not exposed to warehouse).
    /// </summary>
    public class AddGoodsReceiptDetailRequest
    {
        [Required(ErrorMessage = "GoodsReceiptId không được để trống")]
        public int GoodsReceiptId { get; set; }

        [Required(ErrorMessage = "PurchaseOrderDetailId không được để trống")]
        public int PurchaseOrderDetailId { get; set; }

        [Required(ErrorMessage = "ProductVariantId không được để trống")]
        public int ProductVariantId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "ReceivedWeight phải lớn hơn 0")]
        public decimal ReceivedWeight { get; set; }
    }

    /// <summary>
    /// Cập nhật lại khối lượng nhận của một dòng chi tiết phiếu nhập trước khi QC/Approve.
    /// Không cho phép đổi PO line hoặc ProductVariant, chỉ sửa ReceivedWeight.
    /// </summary>
    public class UpdateGoodsReceiptDetailRequest
    {
        [Required(ErrorMessage = "DetailId không được để trống")]
        public int DetailId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "ReceivedWeight phải lớn hơn 0")]
        public decimal ReceivedWeight { get; set; }
    }


    public class QCInspectionRequest
    {
        [Required(ErrorMessage = "DetailId không được để trống")]
        public int DetailId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "UsableWeight phải lớn hơn hoặc bằng 0 (bằng 0 khi QC không đạt)")]
        public decimal UsableWeight { get; set; }
    }

    public class CreateLotRequest
    {
        [Required(ErrorMessage = "GoodsReceiptDetailId không được để trống")]
        public int GoodsReceiptDetailId { get; set; }

        [Required(ErrorMessage = "LotCode không được để trống")]
        [MaxLength(100, ErrorMessage = "LotCode tối đa 100 ký tự")]
        public string LotCode { get; set; } = null!;

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        /// <summary>Hạn sử dụng do supplier cung cấp (nếu có) → ưu tiên cao nhất.</summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Ngày thu hoạch (nếu có). Nếu có HarvestDate và ShelfLifeDays thì Expiry = HarvestDate + ShelfLifeDays.</summary>
        public DateTime? HarvestDate { get; set; }

        /// <summary>Số ngày bảo quản (shelf life) tính từ HarvestDate. Dùng cùng HarvestDate để tính Expiry.</summary>
        [Range(0, int.MaxValue, ErrorMessage = "ShelfLifeDays phải >= 0")]
        public int? ShelfLifeDays { get; set; }
    }

    public class CreateBoxesRequest
    {
        [Required(ErrorMessage = "LotId không được để trống")]
        public int LotId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "BoxSize phải lớn hơn 0")]
        public decimal BoxSize { get; set; }
    }

    // ===================== Response DTOs =====================

    public class GoodsReceiptSummaryDto
    {
        public int Id { get; set; }
        public string ReceiptCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        /// <summary>Lý do chờ Manager (vượt dung sai / dưới định mức). Chỉ có khi Status = PendingManagerApproval.</summary>
        public string? PendingReason { get; set; }
        public int? PurchaseOrderId { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public DateTime ReceivedDate { get; set; }
        public decimal TotalReceivedWeight { get; set; }
        public decimal TotalUsableWeight { get; set; }
    }

    public class GoodsReceiptDetailLineDto
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal ReceivedWeight { get; set; }
        public decimal? UsableWeight { get; set; }
        public decimal RejectWeight { get; set; }
        public string QCResult { get; set; } = null!;
    }

    public class GoodsReceiptResponseDto : GoodsReceiptSummaryDto
    {
        public List<GoodsReceiptDetailLineDto> Details { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết dòng phiếu nhập kèm giá nhập. Chỉ dùng cho API duyệt phiếu (Manager/Admin).
    /// </summary>
    public class GoodsReceiptDetailLineForApprovalDto : GoodsReceiptDetailLineDto
    {
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    /// <summary>
    /// Phiếu nhập đầy đủ kèm giá nhập để Manager/Admin xem xét khi Approve/Reject. Chỉ trả về từ GET .../for-approval.
    /// </summary>
    public class GoodsReceiptForApprovalDto : GoodsReceiptSummaryDto
    {
        public decimal TotalAmount { get; set; }
        public List<GoodsReceiptDetailLineForApprovalDto> Details { get; set; } = new();
    }
}