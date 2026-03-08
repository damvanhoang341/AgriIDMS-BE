using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class CreateStockCheckRequest
    {
        [Required]
        public int WarehouseId { get; set; }

        [Required]
        public StockCheckType CheckType { get; set; }

        /// <summary>Bắt buộc khi CheckType = Spot: danh sách BoxId cần kiểm kê.</summary>
        public List<int>? BoxIds { get; set; }
    }
}
