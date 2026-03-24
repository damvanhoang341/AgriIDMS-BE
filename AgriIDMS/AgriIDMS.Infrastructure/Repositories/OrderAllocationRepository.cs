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
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.BoxId == boxId);
        }
    }
}

