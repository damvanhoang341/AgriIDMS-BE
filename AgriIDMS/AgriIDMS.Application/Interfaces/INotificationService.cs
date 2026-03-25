using AgriIDMS.Application.DTOs.Notification;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface INotificationService
    {
        // Event producers
        Task NotifyOrderPaidAsync(int orderId);
        Task NotifyOrderPaymentFailedAsync(int orderId);
        Task NotifyOrderPaymentCancelledAsync(int orderId);
        Task NotifyOrderAllocationShortageAsync(int orderId);
        Task NotifyExportApprovedAsync(int exportReceiptId);
        Task NotifyStockCheckApprovedAsync(int stockCheckId);
        Task NotifyBackorderExpiredForSalesAsync(int orderId);
        Task NotifyNearExpiryLotAsync(int lotId);

        // User inbox
        Task<PagedNotificationResponse> GetMyNotificationsAsync(string userId, bool unreadOnly, int page, int pageSize);
        Task<int> GetMyUnreadCountAsync(string userId);
        /// <param name="notificationId">Id bản ghi Notifications (cùng giá trị với trường notificationId trong list inbox).</param>
        Task MarkAsReadAsync(string userId, int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}

