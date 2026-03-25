using System;
using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class StockCheckDetailsResponseDto
    {
        public int StockCheckId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string CheckType { get; set; } = string.Empty;

        public DateTime SnapshotAt { get; set; }
        public bool IsLockedSnapshot { get; set; }

        public IList<StockCheckDetailDto> Details { get; set; } = new List<StockCheckDetailDto>();
    }
}

