using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Notification
    {
        public int Id { get; private set; }
        public NotificationType Type { get; private set; }
        public string Message { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public ICollection<UserNotification> UserNotifications { get; private set; }
            = new List<UserNotification>();
    }

}
