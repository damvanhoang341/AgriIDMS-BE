using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class StockCheckWarehouseDashboardDto
    {
        public IList<StockCheckDashboardItemDto> DraftChecks { get; set; } = new List<StockCheckDashboardItemDto>();
        public IList<StockCheckDashboardItemDto> InProgressChecks { get; set; } = new List<StockCheckDashboardItemDto>();
        public IList<StockCheckDashboardItemDto> CountedChecks { get; set; } = new List<StockCheckDashboardItemDto>();
    }
}

