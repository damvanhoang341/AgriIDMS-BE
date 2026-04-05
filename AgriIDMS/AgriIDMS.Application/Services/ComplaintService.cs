using AgriIDMS.Application.DTOs.Complaint;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IOrderAllocationRepository _allocationRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ComplaintService> _logger;

        private static readonly OrderStatus[] AllowedOrderStatusesForComplaint =
        {
            OrderStatus.ApprovedExport,
            OrderStatus.Delivered,
            OrderStatus.Completed
        };

        public ComplaintService(
            IComplaintRepository complaintRepo,
            IOrderRepository orderRepo,
            IOrderAllocationRepository allocationRepo,
            IUnitOfWork unitOfWork,
            ILogger<ComplaintService> logger)
        {
            _complaintRepo = complaintRepo;
            _orderRepo = orderRepo;
            _allocationRepo = allocationRepo;
            _uow = unitOfWork;
            _logger = logger;
        }

        public async Task<ComplaintResponseDto> CreateAsync(CreateComplaintRequest request, string userId)
        {
            var order = await _orderRepo.GetByIdAsync(request.OrderId)
                ?? throw new NotFoundException($"Đơn hàng #{request.OrderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền khiếu nại trên đơn hàng này");

            if (!AllowedOrderStatusesForComplaint.Contains(order.Status))
                throw new InvalidBusinessRuleException(
                    $"Chỉ được khiếu nại khi đơn đang trong luồng giao (ApprovedExport) hoặc đã hoàn thành (Delivered/Completed). Hiện tại: {order.Status}");

            var allocation = await _allocationRepo.GetByOrderIdAndBoxIdAsync(request.OrderId, request.BoxId);
            if (allocation == null)
                throw new InvalidBusinessRuleException("Box không thuộc đơn hàng này hoặc không có phân bổ.");
            if (allocation.Status == AllocationStatus.Cancelled)
                throw new InvalidBusinessRuleException("Phân bổ box đã bị hủy, không thể khiếu nại.");

            var box = allocation.Box ?? throw new NotFoundException("Không tìm thấy thông tin box.");

            ValidateDamagedQuantity(request.Type, request.DamagedQuantity, allocation.ReservedQuantity, box.Weight);

            if (await _complaintRepo.HasPendingComplaintForOrderAndBoxAsync(request.OrderId, request.BoxId))
                throw new InvalidBusinessRuleException("Đã có khiếu nại đang chờ xử lý cho box này.");

            var complaint = new Complaint
            {
                OrderId = request.OrderId,
                BoxId = request.BoxId,
                Type = request.Type,
                Status = ComplaintStatus.Pending,
                DamagedQuantity = request.DamagedQuantity,
                Description = request.Description,
                CustomerEvidenceUrl = request.CustomerEvidenceUrl,
                IsVerified = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _complaintRepo.AddAsync(complaint);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Complaint {ComplaintId} created for Order {OrderId}, Box {BoxId}", complaint.Id, request.OrderId, request.BoxId);

            return MapToDto(complaint, box.BoxCode);
        }

        public async Task<IReadOnlyList<ComplaintResponseDto>> GetMineAsync(string userId)
        {
            var list = await _complaintRepo.ListByUserIdAsync(userId);
            return list.Select(c => MapToDto(c, c.Box?.BoxCode)).ToList();
        }

        public async Task<IReadOnlyList<ComplaintableBoxListItemDto>> GetOrderBoxesForComplaintAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Đơn hàng #{orderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền khiếu nại trên đơn hàng này");

            if (!AllowedOrderStatusesForComplaint.Contains(order.Status))
                throw new InvalidBusinessRuleException(
                    $"Chỉ được khiếu nại khi đơn đang trong luồng giao (ApprovedExport) hoặc đã hoàn thành (Delivered/Completed). Hiện tại: {order.Status}");

            // Lấy toàn bộ allocation thuộc đơn (reserved/picked...), chỉ loại trừ allocation bị cancel.
            var allocations = await _allocationRepo.GetByOrderIdWithDetailsAsync(orderId, status: null);
            var allocationsFiltered = allocations
                .Where(a => a.Status != AllocationStatus.Cancelled && a.Box != null)
                .ToList();

            var pendingBoxIds = await _complaintRepo.GetPendingComplaintBoxIdsForOrderAsync(orderId);

            return allocationsFiltered.Select(a => new ComplaintableBoxListItemDto
            {
                BoxId = a.BoxId,
                BoxCode = a.Box.BoxCode,
                ReservedQuantity = a.ReservedQuantity,
                ComplaintableQuantity = CalculateComplaintableQuantity(a.ReservedQuantity, a.Box.Weight),
                HasPendingComplaint = pendingBoxIds.Contains(a.BoxId)
            }).ToList();
        }

        public async Task<IReadOnlyList<EligibleOrderForComplaintListItemDto>> GetEligibleOrdersForCustomerAsync(string userId, int skip, int take)
        {
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);

            var orders = await _orderRepo.GetCustomerOrdersForComplaintAsync(userId, skip, take);

            return orders.Select(o => new EligibleOrderForComplaintListItemDto
            {
                OrderId = o.Id,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                BoxCount = (o.Allocations ?? Array.Empty<OrderAllocation>())
                    .Where(a => a.Status != AllocationStatus.Cancelled)
                    .Select(a => a.BoxId)
                    .Distinct()
                    .Count()
            }).ToList();
        }

        public async Task<ComplaintResponseDto> GetByIdAsync(int complaintId, string userId)
        {
            var c = await _complaintRepo.GetByIdWithDetailsAsync(complaintId)
                ?? throw new NotFoundException($"Khiếu nại #{complaintId} không tồn tại");

            if (c.Order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền xem khiếu nại này");

            return MapToDto(c, c.Box?.BoxCode);
        }

        public async Task<ComplaintResponseDto> GetByIdForStaffAsync(int complaintId)
        {
            var c = await _complaintRepo.GetByIdWithDetailsAsync(complaintId)
                ?? throw new NotFoundException($"Khiếu nại #{complaintId} không tồn tại");

            return MapToDto(c, c.Box?.BoxCode);
        }

        public async Task<IReadOnlyList<ComplaintResponseDto>> GetAllForStaffAsync(int skip, int take)
        {
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);
            var list = await _complaintRepo.ListAllAsync(skip, take);
            return list.Select(c => MapToDto(c, c.Box?.BoxCode)).ToList();
        }

        public async Task<ComplaintResponseDto> VerifyAsync(int complaintId, VerifyComplaintRequest request, string staffUserId)
        {
            var complaint = await _complaintRepo.GetByIdWithDetailsAsync(complaintId)
                ?? throw new NotFoundException($"Khiếu nại #{complaintId} không tồn tại");

            if (complaint.IsDeleted)
                throw new InvalidBusinessRuleException("Khiếu nại đã bị xóa.");

            if (complaint.Status != ComplaintStatus.Pending)
                throw new InvalidBusinessRuleException(
                    $"Chỉ xử lý khiếu nại đang chờ (Pending). Hiện tại: {complaint.Status}");

            complaint.Status = request.Approved ? ComplaintStatus.Verified : ComplaintStatus.Rejected;
            complaint.IsVerified = true;
            complaint.VerifiedBy = staffUserId;
            complaint.VerifiedAt = DateTime.UtcNow;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Complaint {ComplaintId} {Result} by {StaffUserId}",
                complaintId, request.Approved ? "verified" : "rejected", staffUserId);

            return MapToDto(complaint, complaint.Box?.BoxCode);
        }

        public async Task<ComplaintResponseDto> CancelPendingAsync(int complaintId, string userId)
        {
            var complaint = await _complaintRepo.GetByIdWithDetailsAsync(complaintId)
                ?? throw new NotFoundException($"Khiếu nại #{complaintId} không tồn tại");

            if (complaint.Order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền hủy khiếu nại này");

            if (complaint.Status != ComplaintStatus.Pending)
                throw new InvalidBusinessRuleException("Chỉ được hủy khiếu nại đang chờ xử lý (Pending).");

            complaint.Status = ComplaintStatus.Closed;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Complaint {ComplaintId} closed by customer {UserId}", complaintId, userId);

            return MapToDto(complaint, complaint.Box?.BoxCode);
        }

        private static void ValidateDamagedQuantity(
            ComplaintType type,
            decimal damagedQty,
            decimal reservedQty,
            decimal boxWeight)
        {
            var max = Math.Min(reservedQty, boxWeight);
            if (max <= 0)
                max = boxWeight;

            if (damagedQty <= 0)
                throw new InvalidBusinessRuleException("Số lượng khiếu nại phải lớn hơn 0.");

            if (damagedQty > max)
                throw new InvalidBusinessRuleException(
                    $"Số lượng khiếu nại ({damagedQty:N2} kg) không được vượt quá khối lượng box/phân bổ ({max:N2} kg).");

            switch (type)
            {
                case ComplaintType.Damaged:
                case ComplaintType.MissingQuantity:
                case ComplaintType.WrongItem:
                case ComplaintType.Other:
                    break;
                default:
                    throw new InvalidBusinessRuleException("Loại khiếu nại không hợp lệ.");
            }
        }

        private static decimal CalculateComplaintableQuantity(decimal reservedQty, decimal boxWeight)
        {
            var max = Math.Min(reservedQty, boxWeight);
            if (max <= 0)
                max = boxWeight;
            return max;
        }

        private static ComplaintResponseDto MapToDto(Complaint c, string? boxCode)
        {
            return new ComplaintResponseDto
            {
                Id = c.Id,
                OrderId = c.OrderId,
                BoxId = c.BoxId,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                DamagedQuantity = c.DamagedQuantity,
                Description = c.Description,
                CustomerEvidenceUrl = c.CustomerEvidenceUrl,
                IsVerified = c.IsVerified,
                VerifiedBy = c.VerifiedBy,
                VerifiedAt = c.VerifiedAt,
                CreatedAt = c.CreatedAt,
                BoxCode = boxCode
            };
        }

    }
}
