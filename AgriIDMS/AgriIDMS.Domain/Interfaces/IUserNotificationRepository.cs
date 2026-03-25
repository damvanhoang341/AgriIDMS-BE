using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IUserNotificationRepository
    {
        Task AddRangeAsync(IEnumerable<UserNotification> userNotifications);

        Task<(int total, List<UserNotification> items)> GetByUserIdAsync(
            string userId,
            bool unreadOnly,
            int skip,
            int take);

        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>Lấy bản ghi inbox theo khóa nghiệp vụ (UserId + NotificationId). Cột UserNotifications.Id không dùng làm khóa tra cứu.</summary>
        Task<UserNotification?> GetByUserAndNotificationAsync(string userId, int notificationId);

        Task MarkAllAsReadAsync(string userId);
    }
}

