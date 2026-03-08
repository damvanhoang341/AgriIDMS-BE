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

        /// <summary>Số giờ tối thiểu box phải trong kho lạnh trước khi xuất. Chỉ áp dụng khi TitleWarehouse = Cold. Mặc định 48.</summary>
        public decimal? MinColdStorageHours { get; set; }
    }

    public class WarehouseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public TitleWarehouse TitleWarehouse { get; set; }
        public decimal? MinColdStorageHours { get; set; }
    }
}

