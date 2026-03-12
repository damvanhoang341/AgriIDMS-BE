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
        public string? ReferenceType { get; private set; }
        public int? ReferenceId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public ICollection<UserNotification> UserNotifications { get; private set; }
            = new List<UserNotification>();

        private Notification() { }

        public Notification(
            NotificationType type,
            string message,
            string? referenceType = null,
            int? referenceId = null)
        {
            Type = type;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ReferenceType = referenceType;
            ReferenceId = referenceId;
            CreatedAt = DateTime.UtcNow;
        }
    }

}
