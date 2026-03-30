using AgriIDMS.Application.DTOs.Disposal;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class DisposalRequestService : IDisposalRequestService
    {
        private readonly IDisposalRequestRepository _repo;
        private readonly IBoxRepository _boxRepo;
        private readonly ISlotRepository _slotRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public DisposalRequestService(
            IDisposalRequestRepository repo,
            IBoxRepository boxRepo,
            ISlotRepository slotRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _repo = repo;
            _boxRepo = boxRepo;
            _slotRepo = slotRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<int> CreateRequestAsync(CreateDisposalRequestDto dto, string userId)
        {
            var (ids, _) = await ValidateAndLoadBoxesAsync(dto);

            var req = new DisposalRequest
            {
                WarehouseId = dto.WarehouseId,
                Status = DisposalRequestStatus.Pending,
                Reason = dto.Reason.Trim(),
                RequestedBy = userId,
                RequestedAt = DateTime.UtcNow,
                Items = ids.Select(id => new DisposalRequestItem { BoxId = id }).ToList()
            };

            await _repo.CreateAsync(req);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyDisposalRequestPendingAdminAsync(req.Id);
            return req.Id;
        }

        public async Task DirectDisposeAsync(CreateDisposalRequestDto dto, string reviewerUserId)
        {
            var (_, boxes) = await ValidateAndLoadBoxesAsync(dto);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ProcessDisposeBoxesAsync(boxes, reviewerUserId, null);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<DisposalRequestListItemDto>> GetRequestsAsync(DisposalRequestStatus? status, int? warehouseId)
        {
            var list = await _repo.GetListAsync(status, warehouseId);
            return list.Select(r => new DisposalRequestListItemDto
            {
                Id = r.Id,
                Status = r.Status.ToString(),
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                Reason = r.Reason,
                RequestedBy = r.RequestedBy,
                RequestedByName = r.RequestedUser?.FullName ?? r.RequestedUser?.UserName,
                RequestedAt = r.RequestedAt,
                ReviewedBy = r.ReviewedBy,
                ReviewedByName = r.ReviewedUser?.FullName ?? r.ReviewedUser?.UserName,
                ReviewedAt = r.ReviewedAt,
                ReviewNote = r.ReviewNote,
                BoxCount = r.Items?.Count ?? 0
            }).ToList();
        }

        public async Task<DisposalRequestDetailDto> GetRequestDetailAsync(int id)
        {
            var r = await _repo.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException("Yêu cầu tiêu hủy không tồn tại");

            return new DisposalRequestDetailDto
            {
                Id = r.Id,
                Status = r.Status.ToString(),
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                Reason = r.Reason,
                RequestedBy = r.RequestedBy,
                RequestedByName = r.RequestedUser?.FullName ?? r.RequestedUser?.UserName,
                RequestedAt = r.RequestedAt,
                ReviewedBy = r.ReviewedBy,
                ReviewedByName = r.ReviewedUser?.FullName ?? r.ReviewedUser?.UserName,
                ReviewedAt = r.ReviewedAt,
                ReviewNote = r.ReviewNote,
                BoxCount = r.Items?.Count ?? 0,
                Items = (r.Items ?? new List<DisposalRequestItem>()).Select(i => new DisposalRequestDetailItemDto
                {
                    BoxId = i.BoxId,
                    BoxCode = i.Box?.BoxCode ?? string.Empty,
                    Weight = i.Box?.Weight ?? 0,
                    LotCode = i.Box?.Lot?.LotCode,
                    ExpiryDate = i.Box?.Lot?.ExpiryDate,
                    SlotCode = i.Box?.Slot?.Code,
                    ProductName = i.Box?.Lot?.GoodsReceiptDetail?.ProductVariant?.Product?.Name,
                    ProductVariantName = i.Box?.Lot?.GoodsReceiptDetail?.ProductVariant?.Name
                }).ToList()
            };
        }

        public async Task ApproveAsync(int id, string adminUserId, string? reviewNote = null)
        {
            var req = await _repo.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException("Yêu cầu tiêu hủy không tồn tại");

            if (req.Status != DisposalRequestStatus.Pending)
                throw new InvalidBusinessRuleException("Yêu cầu tiêu hủy không còn ở trạng thái chờ duyệt");

            var boxIds = (req.Items ?? new List<DisposalRequestItem>())
                .Select(i => i.BoxId)
                .Distinct()
                .ToList();

            if (boxIds.Count == 0)
                throw new InvalidBusinessRuleException("Yêu cầu không có box để xử lý");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var boxes = await _boxRepo.GetByIdsWithLotAndReceiptAsync(boxIds);
                if (boxes.Count == 0)
                    throw new NotFoundException("Không tìm thấy box cần tiêu hủy");

                await ProcessDisposeBoxesAsync(boxes, adminUserId, req.Id);

                req.Status = DisposalRequestStatus.Approved;
                req.ReviewedBy = adminUserId;
                req.ReviewedAt = DateTime.UtcNow;
                req.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                await _notificationService.NotifyDisposalRequestApprovedAsync(req.Id, req.RequestedBy);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task RejectAsync(int id, string adminUserId, string? reviewNote = null)
        {
            var req = await _repo.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException("Yêu cầu tiêu hủy không tồn tại");

            if (req.Status != DisposalRequestStatus.Pending)
                throw new InvalidBusinessRuleException("Yêu cầu tiêu hủy không còn ở trạng thái chờ duyệt");

            req.Status = DisposalRequestStatus.Rejected;
            req.ReviewedBy = adminUserId;
            req.ReviewedAt = DateTime.UtcNow;
            req.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();

            await _unitOfWork.SaveChangesAsync();
            await _notificationService.NotifyDisposalRequestRejectedAsync(req.Id, req.RequestedBy);
        }

        private async Task<(List<int> ids, List<Box> boxes)> ValidateAndLoadBoxesAsync(CreateDisposalRequestDto dto)
        {
            if (dto.WarehouseId <= 0)
                throw new InvalidBusinessRuleException("WarehouseId không hợp lệ");
            if (dto.BoxIds == null || dto.BoxIds.Count == 0)
                throw new InvalidBusinessRuleException("Danh sách BoxId không được để trống");
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidBusinessRuleException("Lý do tiêu hủy không được để trống");

            var ids = dto.BoxIds.Where(i => i > 0).Distinct().ToList();
            if (ids.Count == 0)
                throw new InvalidBusinessRuleException("Danh sách BoxId không hợp lệ");

            var boxes = await _boxRepo.GetByIdsWithLotAndReceiptAsync(ids);
            if (boxes.Count != ids.Count)
                throw new NotFoundException("Một hoặc nhiều box không tồn tại");

            foreach (var b in boxes)
            {
                var warehouseId = b.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId ?? 0;
                if (warehouseId != dto.WarehouseId)
                    throw new InvalidBusinessRuleException($"Box {b.BoxCode} không thuộc kho đã chọn");
                if (b.Weight <= 0 || b.Status == BoxStatus.Exported || b.Status == BoxStatus.Expired || b.Status == BoxStatus.Disposed)
                    throw new InvalidBusinessRuleException($"Box {b.BoxCode} không hợp lệ để tiêu hủy");
            }

            return (ids, boxes);
        }

        private async Task ProcessDisposeBoxesAsync(List<Box> boxes, string reviewerUserId, int? requestId)
        {
            var now = DateTime.UtcNow;
            foreach (var box in boxes)
            {
                var isAlreadyRemoved = box.Status == BoxStatus.Exported || box.Weight <= 0 || box.Status == BoxStatus.Disposed;
                if (isAlreadyRemoved)
                    continue;

                var removedWeight = box.Weight;
                var fromSlotId = box.SlotId;

                if (box.SlotId.HasValue)
                {
                    var slot = await _slotRepo.GetByIdAsync(box.SlotId.Value);
                    if (slot != null)
                    {
                        slot.CurrentCapacity = Math.Max(0, slot.CurrentCapacity - removedWeight);
                        await _slotRepo.UpdateAsync(slot);
                    }
                }

                if (box.Lot != null)
                    box.Lot.RemainingQuantity = Math.Max(0, box.Lot.RemainingQuantity - removedWeight);

                box.SlotId = null;
                box.Status = BoxStatus.Disposed;
                box.Weight = 0;
                await _boxRepo.UpdateAsync(box);

                await _inventoryTranRepo.CreateAsync(new InventoryTransaction
                {
                    BoxId = box.Id,
                    TransactionType = InventoryTransactionType.Dispose,
                    FromSlotId = fromSlotId,
                    ToSlotId = null,
                    Quantity = removedWeight,
                    ReferenceType = ReferenceType.Adjustment,
                    ReferenceRequestId = requestId,
                    CreatedBy = string.IsNullOrWhiteSpace(reviewerUserId) ? "system" : reviewerUserId,
                    CreatedAt = now
                });
            }
        }
    }
}

