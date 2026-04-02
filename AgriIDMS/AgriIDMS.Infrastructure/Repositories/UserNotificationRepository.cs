using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class UserNotificationRepository : IUserNotificationRepository
    {
        private readonly AppDbContext _context;

        public UserNotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<UserNotification> userNotifications)
        {
            await _context.UserNotifications.AddRangeAsync(userNotifications);
        }

        public async Task<(int total, List<UserNotification> items)> GetByUserIdAsync(
            string userId,
            bool unreadOnly,
            int skip,
            int take)
        {
            var query = _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId);

            if (unreadOnly)
                query = query.Where(un => !un.IsRead);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(un => un.Notification.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (total, items);
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _context.UserNotifications.CountAsync(un => un.UserId == userId && !un.IsRead);
        }

        public Task<UserNotification?> GetByUserAndNotificationAsync(string userId, int notificationId)
        {
            return _context.UserNotifications
                .Include(un => un.Notification)
                .FirstOrDefaultAsync(un =>
                    un.UserId == userId
                    && un.NotificationId == notificationId);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _context.UserNotifications
                .Where(un => un.UserId == userId && !un.IsRead)
                .ToListAsync();

            foreach (var un in unread)
                un.MarkAsRead();
        }
    }
}

