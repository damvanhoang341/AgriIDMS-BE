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

        Task<IList<Order>> GetPendingSaleConfirmationOrdersAsync(string? customerUserId, int skip, int take);
        Task<IList<Order>> GetPendingAllocationOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);
        Task<IList<Order>> GetPendingWarehouseConfirmOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);
        Task<IList<Order>> GetConfirmedAllocationOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take);

        /// <summary>Đơn sẵn sàng xuất kho theo PaymentTiming; còn allocation Reserved.</summary>
        Task<IList<Order>> GetPaidPendingExportOrdersAsync(int? orderId, OrderSource? source, int skip, int take, string? sort);

        /// <summary>Danh sách đơn của customer có thể khiếu nại (Shipping/Completed).</summary>
        Task<IList<Order>> GetCustomerOrdersForComplaintAsync(string userId, int skip, int take);
        Task<IList<Order>> GetDeliveredOrdersEligibleForCompletionAsync(DateTime deliveredBeforeUtc);
        Task<bool> HasPendingComplaintAsync(int orderId);
    }
}
