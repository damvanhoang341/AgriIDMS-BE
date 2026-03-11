using AgriIDMS.Domain.Entities;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order);

        Task<Order?> GetByIdWithDetailsAsync(int id);

        Task<Order?> GetByIdWithPaymentsAsync(int id);

        Task<Order?> GetByIdAsync(int id);
    }
}

