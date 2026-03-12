using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        public async Task<bool> ExistsAsync(NotificationType type, string message, string? referenceType, int? referenceId)
        {
            return await _context.Notifications.AnyAsync(n =>
                n.Type == type
                && n.Message == message
                && n.ReferenceType == referenceType
                && n.ReferenceId == referenceId);
        }
    }
}

