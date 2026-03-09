using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IOrderAllocationRepository
    {
        Task AddRangeAsync(IEnumerable<OrderAllocation> allocations);
    }
}

