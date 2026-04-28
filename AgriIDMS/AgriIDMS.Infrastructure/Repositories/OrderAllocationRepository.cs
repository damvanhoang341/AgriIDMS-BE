using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class OrderAllocationRepository : IOrderAllocationRepository
    {
        private readonly AppDbContext _context;

        public OrderAllocationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<OrderAllocation> allocations)
        {
            await _context.OrderAllocations.AddRangeAsync(allocations);
        }

        public async Task<List<OrderAllocation>> GetByOrderIdAsync(int orderId, AllocationStatus? status = null)
        {
            var query = _context.OrderAllocations
                .Include(a => a.Box)
                    .ThenInclude(b => b.Slot)
                .Where(a => a.OrderId == orderId);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            return await query.ToListAsync();
        }

        public async Task<List<OrderAllocation>> GetByOrderIdWithDetailsAsync(int orderId, AllocationStatus? status = null)
        {
            var query = _context.OrderAllocations
                .Include(a => a.Box)
                    .ThenInclude(b => b.Lot)
                .Include(a => a.OrderDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Where(a => a.OrderId == orderId);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            return await query.ToListAsync();
        }

        public async Task<OrderAllocation?> GetByOrderIdAndBoxIdAsync(int orderId, int boxId)
        {
            return await _context.OrderAllocations
                .Include(a => a.Box)
                .Include(a => a.OrderDetail)
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.BoxId == boxId);
        }

        public Task<bool> HasReservedOrPickedAllocationForBoxAsync(int boxId)
        {
            return _context.OrderAllocations.AnyAsync(a =>
                a.BoxId == boxId &&
                a.Status != AllocationStatus.Cancelled &&
                (a.Status == AllocationStatus.Reserved || a.Status == AllocationStatus.Picked));
        }

        public async Task<List<OrderAllocation>> GetForRevenueEstimateReportAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? warehouseId,
            int? productId,
            int? productVariantId)
        {
            var q = _context.OrderAllocations
                .AsSplitQuery()
                .Include(a => a.Order)
                .Include(a => a.OrderDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(a => a.Box)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(grd => grd.GoodsReceipt)
                                .ThenInclude(gr => gr.Warehouse)
                .Include(a => a.Box)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(grd => grd.GoodsReceipt)
                                .ThenInclude(gr => gr.Supplier)
                .Where(a =>
                    a.Status != AllocationStatus.Cancelled &&
                    (a.Status == AllocationStatus.Reserved ||
                     a.Status == AllocationStatus.Picked ||
                     a.Status == AllocationStatus.SoftLocked));

            if (fromDate.HasValue)
                q = q.Where(a => a.ReservedAt >= fromDate.Value);
            if (toDate.HasValue)
                q = q.Where(a => a.ReservedAt <= toDate.Value);
            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                q = q.Where(a =>
                    a.Box != null &&
                    a.Box.Lot != null &&
                    a.Box.Lot.GoodsReceiptDetail != null &&
                    a.Box.Lot.GoodsReceiptDetail.GoodsReceipt != null &&
                    a.Box.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId.Value);
            }
            if (productId.HasValue && productId.Value > 0)
            {
                q = q.Where(a =>
                    a.OrderDetail != null &&
                    a.OrderDetail.ProductVariant != null &&
                    a.OrderDetail.ProductVariant.ProductId == productId.Value);
            }
            if (productVariantId.HasValue && productVariantId.Value > 0)
            {
                q = q.Where(a => a.OrderDetail != null && a.OrderDetail.ProductVariantId == productVariantId.Value);
            }

            return await q
                .OrderByDescending(a => a.ReservedAt)
                .ToListAsync();
        }
    }
}

