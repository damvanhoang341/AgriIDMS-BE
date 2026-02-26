using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.GoodsReceipt
{
    public class CreateGoodsReceiptRequest
    {
        [Required(ErrorMessage = "Nhà cung cấp không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Nhà cung cấp không hợp lệ.")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Kho không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Kho không hợp lệ.")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [StringLength(50, ErrorMessage = "Biển số xe tối đa 50 ký tự.")]
        public string VehicleNumber { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Tên tài xế tối đa 100 ký tự.")]
        public string? DriverName { get; set; }

        [StringLength(150, ErrorMessage = "Tên đơn vị vận chuyển tối đa 150 ký tự.")]
        public string? TransportCompany { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Trọng lượng tổng phải lớn hơn hoặc bằng 0.")]
        public decimal? GrossWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Trọng lượng bì phải lớn hơn hoặc bằng 0.")]
        public decimal? TareWeight { get; set; }

        [Required(ErrorMessage = "Ngày nhập kho không được để trống.")]
        public DateTime ReceivedDate { get; set; }

        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống.")]
        [MinLength(1, ErrorMessage = "Phiếu nhập phải có ít nhất một sản phẩm.")]
        public List<CreateGoodsReceiptDetailRequest> Details { get; set; } = new();
    }

    public class CreateGoodsReceiptDetailRequest
    {
        [Required(ErrorMessage = "Sản phẩm không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sản phẩm không hợp lệ.")]
        public int ProductVariantId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng dự kiến phải lớn hơn hoặc bằng 0.")]
        public decimal EstimatedQuantity { get; set; }

        [Required(ErrorMessage = "Số lượng thực tế không được để trống.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Số lượng thực tế phải lớn hơn 0.")]
        public decimal ActualQuantity { get; set; }

        [Required(ErrorMessage = "Đơn giá không được để trống.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0.")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Danh sách lô hàng không được để trống.")]
        [MinLength(1, ErrorMessage = "Mỗi sản phẩm phải có ít nhất một lô.")]
        public List<CreateLotRequest> Lots { get; set; } = new();
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