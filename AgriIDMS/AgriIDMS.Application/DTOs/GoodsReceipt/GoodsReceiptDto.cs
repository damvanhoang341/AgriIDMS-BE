using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.GoodsReceipt
{
    using System.ComponentModel.DataAnnotations;

    public class CreateGoodsReceiptRequest
    {
        [Required(ErrorMessage = "SupplierId không được để trống")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "WarehouseId không được để trống")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Biển số xe không được để trống")]
        [StringLength(50, ErrorMessage = "Biển số xe tối đa 50 ký tự")]
        public string VehicleNumber { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Tên tài xế tối đa 100 ký tự")]
        public string? DriverName { get; set; }

        [StringLength(150, ErrorMessage = "Tên công ty vận chuyển tối đa 150 ký tự")]
        public string? TransportCompany { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "GrossWeight phải >= 0")]
        public decimal? GrossWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "TareWeight phải >= 0")]
        public decimal? TareWeight { get; set; }

        [Range(0, 100, ErrorMessage = "TolerancePercent phải từ 0 đến 100")]
        public decimal TolerancePercent { get; set; }

        [Required(ErrorMessage = "Danh sách sản phẩm không được rỗng")]
        [MinLength(1, ErrorMessage = "Phiếu nhập phải có ít nhất một sản phẩm")]
        public List<CreateGoodsReceiptDetailRequest> Details { get; set; } = new();
    }

    public class CreateGoodsReceiptDetailRequest
    {
        [Required(ErrorMessage = "PurchaseOrderDetailId không được để trống")]
        public int PurchaseOrderDetailId { get; set; }

        [Required(ErrorMessage = "ProductVariantId không được để trống")]
        public int ProductVariantId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "OrderedWeight phải > 0")]
        public decimal OrderedWeight { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "UnitPrice phải > 0")]
        public decimal UnitPrice { get; set; }
    }

    public class CreateLotRequest
    {
        [Required(ErrorMessage = "Mã lô không được để trống.")]
        [StringLength(100, ErrorMessage = "Mã lô tối đa 100 ký tự.")]
        public string LotCode { get; set; } = null!;

        [Required(ErrorMessage = "Số lượng lô không được để trống.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Số lượng lô phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Hạn sử dụng không được để trống.")]
        public DateTime ExpiryDate { get; set; }
    }
}