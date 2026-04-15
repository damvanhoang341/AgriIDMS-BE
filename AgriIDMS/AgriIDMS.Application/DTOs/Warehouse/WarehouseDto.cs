using AgriIDMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Warehouse
{
    public class CreateWarehouseRequest
    {
        [Required(ErrorMessage = "Tên kho là bắt buộc")]
        [MaxLength(200, ErrorMessage = "Tên kho tối đa 200 ký tự")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ kho là bắt buộc")]
        [MaxLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự")]
        public string Location { get; set; } = null!;

        [Required(ErrorMessage = "Loại kho là bắt buộc")]
        public TitleWarehouse TitleWarehouse { get; set; }
        public decimal? LengthM { get; set; }
        public decimal? WidthM { get; set; }
        public decimal? FloorAreaM2 { get; set; }

        /// <summary>Số giờ tối thiểu box phải trong kho lạnh trước khi xuất. Chỉ áp dụng khi TitleWarehouse = Cold. Mặc định 48.</summary>
        public decimal? MinColdStorageHours { get; set; }
        /// <summary>Định mức tối thiểu (kg) cho mỗi phiếu nhập vào kho. Null = không bắt buộc.</summary>
        public decimal? MinReceiptWeight { get; set; }
    }

    public class WarehouseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public TitleWarehouse TitleWarehouse { get; set; }
        public decimal? LengthM { get; set; }
        public decimal? WidthM { get; set; }
        public decimal? FloorAreaM2 { get; set; }
        public decimal? MinColdStorageHours { get; set; }
        /// <summary>Định mức tối thiểu (kg) cho mỗi phiếu nhập vào kho. Null = không bắt buộc.</summary>
        public decimal? MinReceiptWeight { get; set; }
        /// <summary>Tổng thể tích hàng trong kho (m3), bao gồm cả hàng chưa xếp slot.</summary>
        public decimal TotalStockWeight { get; set; }
        /// <summary>Tổng sức chứa slot của kho (m3).</summary>
        public decimal TotalCapacity { get; set; }
        /// <summary>Thể tích hàng đang nằm trong các slot (m3).</summary>
        public decimal StoredInSlotsWeight { get; set; }
        /// <summary>Thể tích hàng thuộc kho nhưng chưa xếp slot (m3).</summary>
        public decimal UnassignedStockWeight { get; set; }
    }
}

