using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System.Collections.Generic;
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
    }
}

