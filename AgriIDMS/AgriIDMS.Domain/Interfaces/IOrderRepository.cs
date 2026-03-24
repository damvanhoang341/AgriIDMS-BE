using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
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
        Task<IList<Order>> GetPendingSaleConfirmationOrdersAsync(string? customerUserId, int skip, int take);
        Task<IList<Order>> GetPendingAllocationOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);
        Task<IList<Order>> GetPendingWarehouseConfirmOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);
        Task<IList<Order>> GetPendingCustomerDecisionOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);
        Task<IList<Order>> GetBackorderWaitingOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);
    }
}

