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
        /// <summary>Đơn online mới (chờ sale xác nhận / liên hệ khách đặt hay hủy).</summary>
        Task NotifyOnlineOrderPendingSaleConfirmAsync(int orderId);

        /// <summary>Đơn online PayBefore đã quá hạn thanh toán (theo ExpiredAt allocation sau sale confirm). Sale liên hệ khách hoặc hủy đơn.</summary>
        Task NotifyOnlineOrderPayBeforeDeadlineOverdueAsync(int orderId);
        Task NotifyExportApprovedAsync(int exportReceiptId);
        Task NotifyStockCheckApprovedAsync(int stockCheckId);
        Task NotifyStockCheckPendingManagerAsync(int stockCheckId);
        Task NotifyGoodsReceiptPendingManagerAsync(int goodsReceiptId);
        Task NotifyBackorderExpiredForSalesAsync(int orderId);
        Task NotifyNearExpiryLotAsync(int lotId);
        Task NotifyDisposalRequestPendingAdminAsync(int disposalRequestId);
        Task NotifyDisposalRequestApprovedAsync(int disposalRequestId, string requestedByUserId);
        Task NotifyDisposalRequestRejectedAsync(int disposalRequestId, string requestedByUserId);

        // User inbox
        Task<PagedNotificationResponse> GetMyNotificationsAsync(string userId, bool unreadOnly, int page, int pageSize);
        Task<int> GetMyUnreadCountAsync(string userId);
        /// <param name="notificationId">Id bản ghi Notifications (cùng giá trị với trường notificationId trong list inbox).</param>
        Task MarkAsReadAsync(string userId, int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}

