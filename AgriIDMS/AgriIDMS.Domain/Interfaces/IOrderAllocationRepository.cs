using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IOrderAllocationRepository
    {
        Task AddRangeAsync(IEnumerable<OrderAllocation> allocations);
        Task<List<OrderAllocation>> GetByOrderIdAsync(int orderId, AllocationStatus? status = null);
        Task<List<OrderAllocation>> GetByOrderIdWithDetailsAsync(int orderId, AllocationStatus? status = null);
        Task<OrderAllocation?> GetByOrderIdAndBoxIdAsync(int orderId, int boxId);

        /// <summary>Đơn đã giữ thùng (Reserved/Picked) — không cho báo hỏng / duyệt loại thùng.</summary>
        Task<bool> HasReservedOrPickedAllocationForBoxAsync(int boxId);
        Task<List<OrderAllocation>> GetForRevenueEstimateReportAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? warehouseId,
            int? productId,
            int? productVariantId);
    }
}

