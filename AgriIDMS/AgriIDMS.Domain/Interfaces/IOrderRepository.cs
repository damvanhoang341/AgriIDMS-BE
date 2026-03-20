using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IOrderRepository
    {
        Task<IList<Order>> GetByUserIdWithDetailsAndPaymentsAsync(string userId);

        Task AddAsync(Order order);

        Task<Order?> GetByIdWithDetailsAsync(int id);

        Task<Order?> GetByIdWithDetailsAndPaymentsAsync(int id);

        Task<Order?> GetByIdWithPaymentsAsync(int id);

        Task<Order?> GetByIdAsync(int id);

        Task<IList<Order>> GetOverdueBackordersAsync(DateTime nowUtc);
    }
}

