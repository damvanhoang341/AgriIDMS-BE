using AgriIDMS.Application.DTOs.Notification;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserNotificationRepository _userNotificationRepo;
        private readonly IUserRepository _userRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IExportReceiptRepository _exportRepo;
        private readonly IStockCheckRepository _stockCheckRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepo,
            IUserNotificationRepository userNotificationRepo,
            IUserRepository userRepo,
            IOrderRepository orderRepo,
            IExportReceiptRepository exportRepo,
            IStockCheckRepository stockCheckRepo,
            IUnitOfWork uow,
            ILogger<NotificationService> logger)
        {
            _notificationRepo = notificationRepo;
            _userNotificationRepo = userNotificationRepo;
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _exportRepo = exportRepo;
            _stockCheckRepo = stockCheckRepo;
            _uow = uow;
            _logger = logger;
        }

        public async Task NotifyOrderPaidAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var message = $"Đơn hàng #{orderId} đã thanh toán thành công.";
            await CreateNotificationIfNotExistsAsync(
                NotificationType.Order,
                message,
                referenceType: "Order",
                referenceId: orderId,
                recipientUserIds: new[] { order.UserId });
        }

        public async Task NotifyExportApprovedAsync(int exportReceiptId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportReceiptId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportReceiptId} không tồn tại");

            var order = receipt.Order;
            var message = $"Phiếu xuất {receipt.ExportCode} đã được duyệt. Đơn hàng #{receipt.OrderId} đang giao.";

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                order.UserId
            };

            if (!string.IsNullOrWhiteSpace(receipt.CreatedBy))
                recipients.Add(receipt.CreatedBy);

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Order,
                message,
                referenceType: "ExportReceipt",
                referenceId: receipt.Id,
                recipientUserIds: recipients);
        }

        public async Task NotifyStockCheckApprovedAsync(int stockCheckId)
        {
            var stockCheck = await _stockCheckRepo.GetByIdAsync(stockCheckId)
                ?? throw new NotFoundException($"Phiếu kiểm kê #{stockCheckId} không tồn tại");

            var message = $"Phiếu kiểm kê #{stockCheckId} đã được duyệt. Tồn kho đã được điều chỉnh.";

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(stockCheck.CreatedBy))
                recipients.Add(stockCheck.CreatedBy);

            // Notify managers/admins
            var managers = await _userRepo.GetUserIdsInRolesAsync("Admin", "Manager");
            foreach (var id in managers)
                recipients.Add(id);

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "StockCheck",
                referenceId: stockCheckId,
                recipientUserIds: recipients);
        }

        public async Task NotifyBackorderExpiredForSalesAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var message = $"Đơn hàng #{orderId} đã quá hạn backorder. Vui lòng liên hệ khách để chọn CancelShortage hoặc CancelOrder.";

            var recipients = await _userRepo.GetUserIdsInRolesAsync("Sale", "Admin", "Manager", "WarehouseStaff");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "BackorderExpired",
                referenceId: orderId,
                recipientUserIds: recipients);
        }

        public async Task<PagedNotificationResponse> GetMyNotificationsAsync(string userId, bool unreadOnly, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var skip = (page - 1) * pageSize;
            var (total, items) = await _userNotificationRepo.GetByUserIdAsync(userId, unreadOnly, skip, pageSize);

            return new PagedNotificationResponse
            {
                Total = total,
                Items = items.Select(un => new NotificationItemDto
                {
                    UserNotificationId = un.Id,
                    NotificationId = un.NotificationId,
                    Type = un.Notification.Type.ToString(),
                    Message = un.Notification.Message,
                    ReferenceType = un.Notification.ReferenceType,
                    ReferenceId = un.Notification.ReferenceId,
                    CreatedAt = un.Notification.CreatedAt,
                    IsRead = un.IsRead,
                    ReadAt = un.ReadAt
                }).ToList()
            };
        }

        public Task<int> GetMyUnreadCountAsync(string userId)
        {
            return _userNotificationRepo.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(string userId, int userNotificationId)
        {
            var un = await _userNotificationRepo.GetByIdAsync(userNotificationId)
                ?? throw new NotFoundException("Thông báo không tồn tại");

            if (!string.Equals(un.UserId, userId, StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Bạn không có quyền thao tác thông báo này");

            if (!un.IsRead)
            {
                un.MarkAsRead();
                await _uow.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _userNotificationRepo.MarkAllAsReadAsync(userId);
            await _uow.SaveChangesAsync();
        }

        private async Task CreateNotificationIfNotExistsAsync(
            NotificationType type,
            string message,
            string? referenceType,
            int? referenceId,
            IEnumerable<string> recipientUserIds)
        {
            var recipients = recipientUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count == 0)
            {
                _logger.LogInformation("Skip notification (no recipients). Type={Type}, Ref={RefType}:{RefId}", type, referenceType, referenceId);
                return;
            }

            var exists = await _notificationRepo.ExistsAsync(type, message, referenceType, referenceId);
            if (exists)
            {
                _logger.LogInformation("Notification already exists. Type={Type}, Ref={RefType}:{RefId}", type, referenceType, referenceId);
                return;
            }

            var notification = new Notification(type, message, referenceType, referenceId);
            await _notificationRepo.AddAsync(notification);
            await _uow.SaveChangesAsync();

            var userNotifications = recipients.Select(uid => new UserNotification(uid, notification.Id)).ToList();
            await _userNotificationRepo.AddRangeAsync(userNotifications);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} created for {Count} recipients", notification.Id, recipients.Count);
        }
    }
}

