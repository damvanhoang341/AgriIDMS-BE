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
        [MaxLength(50, ErrorMessage = "Biển số xe tối đa 50 ký tự")]
        public string VehicleNumber { get; set; } = null!;

        [Required(ErrorMessage = "Tên tài xế không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên tài xế tối đa 100 ký tự")]
        public string DriverName { get; set; } = null!;

        [MaxLength(100, ErrorMessage = "Tên công ty vận chuyển tối đa 100 ký tự")]
        public string? TransportCompany { get; set; }

        [Range(0, 100, ErrorMessage = "TolerancePercent phải từ 0 đến 100")]
        public decimal TolerancePercent { get; set; }
    }

    public class AddGoodsReceiptDetailRequest
    {
        [Required(ErrorMessage = "GoodsReceiptId không được để trống")]
        public int GoodsReceiptId { get; set; }

        [Required(ErrorMessage = "PurchaseOrderDetailId không được để trống")]
        public int PurchaseOrderDetailId { get; set; }

        [Required(ErrorMessage = "ProductVariantId không được để trống")]
        public int ProductVariantId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "OrderedWeight phải lớn hơn 0")]
        public decimal OrderedWeight { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "UnitPrice phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }

    public class UpdateTruckWeightRequest
    {
        [Required(ErrorMessage = "GoodsReceiptId không được để trống")]
        public int GoodsReceiptId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "GrossWeight phải lớn hơn 0")]
        public decimal GrossWeight { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "TareWeight phải lớn hơn 0")]
        public decimal TareWeight { get; set; }
    }

    public class QCInspectionRequest
    {
        [Required(ErrorMessage = "DetailId không được để trống")]
        public int DetailId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "UsableWeight phải lớn hơn 0")]
        public decimal UsableWeight { get; set; }

        [Required(ErrorMessage = "QCResult không được để trống")]
        public string QCResult { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "QCNote tối đa 500 ký tự")]
        public string? QCNote { get; set; }
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

        public DateTime? ExpiryDate { get; set; }
    }

    public class CreateBoxesRequest
    {
        [Required(ErrorMessage = "LotId không được để trống")]
        public int LotId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "BoxSize phải lớn hơn 0")]
        public decimal BoxSize { get; set; }
    }
}