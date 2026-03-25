using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class StockCheckManagerDashboardDto
    {
        public IList<StockCheckDashboardItemDto> PendingApprovalChecks { get; set; } = new List<StockCheckDashboardItemDto>();
        public IList<StockCheckDashboardItemDto> ApprovedChecks { get; set; } = new List<StockCheckDashboardItemDto>();
        public IList<StockCheckDashboardItemDto> RejectedChecks { get; set; } = new List<StockCheckDashboardItemDto>();
    }
}

