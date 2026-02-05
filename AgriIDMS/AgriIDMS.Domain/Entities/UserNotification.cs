using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class UserNotification
    {
        public int Id { get; private set; }

        public Guid UserId { get; private set; }
        public ApplicationUser User { get; private set; } = null!;

        public int NotificationId { get; private set; }
        public Notification Notification { get; private set; } = null!;

        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }

        private UserNotification() { }

        public UserNotification(Guid userId, int notificationId)
        {
            UserId = userId;
            NotificationId = notificationId;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

}
