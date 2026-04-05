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
        private readonly IGoodsReceiptRepository _receiptRepo;
        private readonly ILotRepository _lotRepo;
        private readonly IExportReceiptRepository _exportRepo;
        private readonly IStockCheckRepository _stockCheckRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepo,
            IUserNotificationRepository userNotificationRepo,
            IUserRepository userRepo,
            IOrderRepository orderRepo,
            IGoodsReceiptRepository receiptRepo,
            ILotRepository lotRepo,
            IExportReceiptRepository exportRepo,
            IStockCheckRepository stockCheckRepo,
            IUnitOfWork uow,
            ILogger<NotificationService> logger)
        {
            _notificationRepo = notificationRepo;
            _userNotificationRepo = userNotificationRepo;
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _receiptRepo = receiptRepo;
            _lotRepo = lotRepo;
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

        public async Task NotifyOrderPaymentFailedAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var message = $"Thanh toán cho đơn hàng #{orderId} không thành công. Vui lòng thử lại.";
            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "Order",
                referenceId: orderId,
                recipientUserIds: new[] { order.UserId });
        }

        public async Task NotifyOrderPaymentCancelledAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var message = $"Thanh toán cho đơn hàng #{orderId} đã bị hủy.";
            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "Order",
                referenceId: orderId,
                recipientUserIds: new[] { order.UserId });
        }

        public async Task NotifyOrderAllocationShortageAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var totalShortage = order.Details?.Sum(d => d.ShortageQuantity) ?? 0;
            var totalFulfilled = order.Details?.Sum(d => d.FulfilledQuantity) ?? 0m;
            var actionText = totalFulfilled > 0
                ? "hủy phần thiếu"
                : "hủy đơn";

            var message = totalShortage > 0
                ? $"Đơn hàng #{orderId} đang thiếu {totalShortage} box sau khi kho xác nhận. Vui lòng chọn chờ backorder hoặc {actionText}."
                : $"Đơn hàng #{orderId} đang thiếu hàng sau khi kho xác nhận. Vui lòng chọn chờ backorder hoặc {actionText}.";

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                order.UserId
            };
            var salesUsers = await _userRepo.GetUserIdsInRolesAsync("SalesStaff");
            foreach (var id in salesUsers)
                recipients.Add(id);

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "OrderAllocationShortage",
                referenceId: orderId,
                recipientUserIds: recipients);
        }

        public async Task NotifyOnlineOrderPendingSaleConfirmAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Source != OrderSource.Online)
                return;

            var message =
                $"Đơn online #{orderId} vừa được đặt (chờ sale xác nhận). Vui lòng liên hệ khách để chốt đặt hàng hoặc hủy đơn nếu khách không mua.";

            var recipients = await _userRepo.GetUserIdsInRolesAsync("SalesStaff", "Manager", "Admin");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Order,
                message,
                referenceType: "OrderPendingSaleConfirm",
                referenceId: orderId,
                recipientUserIds: recipients);
        }

        public async Task NotifyOnlineOrderPayBeforeDeadlineOverdueAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Source != OrderSource.Online)
                return;

            var message =
                $"Đơn online #{orderId} (trả trước) đã quá hạn thanh toán 24h sau khi sale xác nhận. Vui lòng liên hệ khách: nếu đồng ý thanh toán thì tạo thanh toán/xác nhận thu; nếu không thì hủy đơn và nhả hàng.";

            var recipients = await _userRepo.GetUserIdsInRolesAsync("SalesStaff", "Manager", "Admin");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "OrderPayBeforeDeadlineOverdue",
                referenceId: orderId,
                recipientUserIds: recipients);
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

        public async Task NotifyStockCheckPendingManagerAsync(int stockCheckId)
        {
            var stockCheck = await _stockCheckRepo.GetByIdAsync(stockCheckId)
                ?? throw new NotFoundException($"Phiếu kiểm kê #{stockCheckId} không tồn tại");

            var message = $"Phiếu kiểm kê #{stockCheckId} đã chốt đếm và đang chờ Quản lý duyệt.";
            var recipients = await _userRepo.GetUserIdsInRolesAsync("Admin", "Manager");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "StockCheck",
                referenceId: stockCheckId,
                recipientUserIds: recipients);
        }

        public async Task NotifyGoodsReceiptPendingManagerAsync(int goodsReceiptId)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(goodsReceiptId)
                ?? throw new NotFoundException($"Phiếu nhập #{goodsReceiptId} không tồn tại");

            var statusText = receipt.Status switch
            {
                GoodsReceiptStatus.PendingManagerApprovalQc => "chờ Quản lý duyệt (định mức tối thiểu)",
                GoodsReceiptStatus.PendingManagerApproval => "chờ Quản lý duyệt",
                _ => $"mới tạo ở trạng thái {receipt.Status} và cần Quản lý theo dõi/duyệt",
            };
            var message = $"Phiếu nhập {receipt.ReceiptCode} đang {statusText}.";
            var recipients = await _userRepo.GetUserIdsInRolesAsync("Admin", "Manager");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "GoodsReceipt",
                referenceId: receipt.Id,
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

        public async Task NotifyNearExpiryLotAsync(int lotId)
        {
            var lot = await _lotRepo.GetByIdWithDetailAndReceiptAsync(lotId)
                ?? throw new NotFoundException($"Lot #{lotId} không tồn tại");

            var todayUtc = DateTime.UtcNow.Date;
            var expiryDateUtc = lot.ExpiryDate.Date;
            var dayDiff = (expiryDateUtc - todayUtc).Days;
            var isExpired = dayDiff < 0;
            var overdueDays = isExpired ? Math.Abs(dayDiff) : 0;
            var daysLeft = Math.Max(0, dayDiff);

            var variant = lot.GoodsReceiptDetail?.ProductVariant;
            var productName = variant?.Product?.Name ?? "N/A";
            var grade = variant?.Grade.ToString() ?? "N/A";
            var warehouseName = lot.GoodsReceiptDetail?.GoodsReceipt?.Warehouse?.Name ?? "N/A";
            var slotCodes = (lot.Boxes ?? Enumerable.Empty<Box>())
                .Where(b => b.Slot != null)
                .Select(b => b.Slot!.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .Take(5)
                .ToList();
            var slotDisplay = slotCodes.Count > 0
                ? string.Join(", ", slotCodes)
                : "Chưa xếp slot";
            var message = isExpired
                ? $"Lô {lot.LotCode} ({productName} - {grade}) đã quá hạn {overdueDays} ngày. Kho: {warehouseName}. Slot: {slotDisplay}. Cần xử lý gấp (chặn xuất online/ưu tiên xử lý)."
                : $"Lô {lot.LotCode} ({productName} - {grade}) sắp hết hạn sau {daysLeft} ngày. Kho: {warehouseName}. Slot: {slotDisplay}. Vui lòng ưu tiên xử lý/xuất kho.";

            var recipients = await _userRepo.GetUserIdsInRolesAsync("WarehouseStaff", "Sale", "Manager", "Admin");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "NearExpiryLot",
                referenceId: lot.Id,
                recipientUserIds: recipients);
        }

        public async Task NotifyDisposalRequestPendingAdminAsync(int disposalRequestId)
        {
            var message = $"Có yêu cầu tiêu hủy hàng hóa #{disposalRequestId} đang chờ Quản lí duyệt.";
            // Send to both Manager and Admin to avoid missing notifications if role mapping differs.
            var recipients = await _userRepo.GetUserIdsInRolesAsync("Manager", "Admin");

            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "DisposalRequest",
                referenceId: disposalRequestId,
                recipientUserIds: recipients);
        }

        public async Task NotifyDisposalRequestApprovedAsync(int disposalRequestId, string requestedByUserId)
        {
            var message = $"Yêu cầu tiêu hủy #{disposalRequestId} đã được Quản lí duyệt.";
            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "DisposalRequest",
                referenceId: disposalRequestId,
                recipientUserIds: new[] { requestedByUserId });
        }

        public async Task NotifyDisposalRequestRejectedAsync(int disposalRequestId, string requestedByUserId)
        {
            var message = $"Yêu cầu tiêu hủy #{disposalRequestId} đã bị Quản lí từ chối.";
            await CreateNotificationIfNotExistsAsync(
                NotificationType.Warning,
                message,
                referenceType: "DisposalRequest",
                referenceId: disposalRequestId,
                recipientUserIds: new[] { requestedByUserId });
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
                    // UserNotifications.Id không phải identity (DB thường = 0); FE gọi đánh dấu đọc bằng NotificationId.
                    UserNotificationId = un.NotificationId,
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

        public async Task MarkAsReadAsync(string userId, int notificationId)
        {
            var un = await _userNotificationRepo.GetByUserAndNotificationAsync(userId, notificationId)
                ?? throw new NotFoundException("Thông báo không tồn tại");

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

