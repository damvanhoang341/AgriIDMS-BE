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
        Task NotifyExportApprovedAsync(int exportReceiptId);
        Task NotifyStockCheckApprovedAsync(int stockCheckId);
        Task NotifyBackorderExpiredForSalesAsync(int orderId);
        Task NotifyNearExpiryLotAsync(int lotId);

        // User inbox
        Task<PagedNotificationResponse> GetMyNotificationsAsync(string userId, bool unreadOnly, int page, int pageSize);
        Task<int> GetMyUnreadCountAsync(string userId);
        Task MarkAsReadAsync(string userId, int userNotificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}

