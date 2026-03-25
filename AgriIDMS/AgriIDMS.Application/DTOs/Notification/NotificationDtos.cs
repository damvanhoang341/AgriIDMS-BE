using System;
using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.Notification
{
    public class NotificationItemDto
    {
        /// <summary>Giá trị trùng <see cref="NotificationId"/> — dùng làm id trong route đánh dấu đã đọc (UserNotifications.Id không dùng).</summary>
        public int UserNotificationId { get; set; }
        public int NotificationId { get; set; }
        public string Type { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class PagedNotificationResponse
    {
        public int Total { get; set; }
        public List<NotificationItemDto> Items { get; set; } = new();
    }
}

