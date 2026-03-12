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

        Task<UserNotification?> GetByIdAsync(int id);

        Task MarkAllAsReadAsync(string userId);
    }
}

