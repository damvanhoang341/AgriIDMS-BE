using AgriIDMS.Application.DTOs.StockCheck;
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
    public class StockCheckService : IStockCheckService
    {
        private const decimal VarianceTolerance = 0.001m; // 0.001 kg coi là khớp
        private readonly IStockCheckRepository _stockCheckRepo;
        private readonly IStockCheckDetailRepository _detailRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly ISlotRepository _slotRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IInventoryRequestRepository _inventoryRequestRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StockCheckService> _logger;
        private readonly INotificationService _notificationService;

        public StockCheckService(
            IStockCheckRepository stockCheckRepo,
            IStockCheckDetailRepository detailRepo,
            IBoxRepository boxRepo,
            ISlotRepository slotRepo,
            IWarehouseRepository warehouseRepo,
            IInventoryRequestRepository inventoryRequestRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            IUnitOfWork unitOfWork,
            ILogger<StockCheckService> logger,
            INotificationService notificationService)
        {
            _stockCheckRepo = stockCheckRepo;
            _detailRepo = detailRepo;
            _boxRepo = boxRepo;
            _slotRepo = slotRepo;
            _warehouseRepo = warehouseRepo;
            _inventoryRequestRepo = inventoryRequestRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<int> CreateAsync(CreateStockCheckRequest request, string userId)
        {
            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
            if (warehouse == null)
                throw new NotFoundException("Kho không tồn tại");

            List<int> boxIds;
            if (request.CheckType == StockCheckType.Spot)
            {
                if (request.BoxIds == null || !request.BoxIds.Any())
                    throw new InvalidBusinessRuleException("Kiểm kê Spot cần danh sách BoxIds");
                boxIds = request.BoxIds.Distinct().ToList();
            }
            else if (request.CheckType == StockCheckType.Cycle)
            {
                if (!request.ZoneId.HasValue && !request.RackId.HasValue && !request.SlotId.HasValue)
                    throw new InvalidBusinessRuleException("Kiểm kê theo chu kỳ cần chọn phạm vi (Zone/Rack/Slot)");

                boxIds = await _stockCheckRepo.GetBoxIdsForCycleAsync(
                    request.WarehouseId,
                    request.ZoneId,
                    request.RackId,
                    request.SlotId);

                if (boxIds.Count == 0)
                    throw new InvalidBusinessRuleException("Phạm vi chu kỳ đã chọn không có box để kiểm kê");
            }
            else
            {
                boxIds = await _stockCheckRepo.GetBoxIdsInWarehouseAsync(request.WarehouseId);
                if (boxIds.Count == 0)
                    throw new InvalidBusinessRuleException("Kho không có box nào trong slot để kiểm kê");
            }

            var boxes = await _boxRepo.GetByIdsAsync(boxIds);
            if (boxes.Count != boxIds.Count)
            {
                var missing = boxIds.FirstOrDefault(id => !boxes.ContainsKey(id));
                throw new NotFoundException($"Box Id={missing} không tồn tại");
            }

            var snapshotAt = DateTime.UtcNow;
            var stockCheck = new StockCheck
            {
                WarehouseId = request.WarehouseId,
                CheckType = request.CheckType,
                Status = StockCheckStatus.Draft,
                SnapshotAt = snapshotAt,
                IsLockedSnapshot = false,
                CreatedAt = snapshotAt,
                CreatedBy = userId
            };
            stockCheck.Details = new List<StockCheckDetail>();

            await _stockCheckRepo.AddAsync(stockCheck);
            await _unitOfWork.SaveChangesAsync();

            var details = boxIds.Select(boxId =>
            {
                var box = boxes[boxId];
                return new StockCheckDetail
                {
                    StockCheckId = stockCheck.Id,
                    BoxId = box.Id,
                    SnapshotWeight = box.Weight
                };
            }).ToList();

            await _detailRepo.AddRangeAsync(details);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("StockCheck {Id} created, {Count} details", stockCheck.Id, details.Count);
            return stockCheck.Id;
        }

        public async Task StartCheckAsync(int stockCheckId)
        {
            var stockCheck = await _stockCheckRepo.GetByIdAsync(stockCheckId);
            if (stockCheck == null)
                throw new NotFoundException("Phiếu kiểm kê không tồn tại");
            if (stockCheck.Status != StockCheckStatus.Draft)
                throw new InvalidBusinessRuleException("Chỉ được bắt đầu kiểm kê khi phiếu ở trạng thái Draft");

            stockCheck.Status = StockCheckStatus.InProgress;
            stockCheck.IsLockedSnapshot = true;
            await _stockCheckRepo.UpdateAsync(stockCheck);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("StockCheck {Id} started (InProgress)", stockCheckId);
        }

        public async Task UpdateCountedWeightAsync(UpdateCountedWeightRequest request, string userId)
        {
            var detail = await _detailRepo.GetByIdAsync(request.StockCheckDetailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết kiểm kê không tồn tại");
            if (detail.StockCheck.Status != StockCheckStatus.InProgress)
                throw new InvalidBusinessRuleException("Chỉ được nhập số đếm khi phiếu đang InProgress");

            detail.CountedWeight = request.CountedWeight;
            detail.Note = request.Note;
            detail.CountedBy = userId;
            detail.CountedAt = DateTime.UtcNow;
            detail.DifferenceWeight = request.CountedWeight - detail.SnapshotWeight;
            detail.VarianceType = ComputeVarianceType(detail.DifferenceWeight.Value);
            detail.VarianceReason = NormalizeVarianceReason(detail.VarianceType.Value, request.VarianceReason);

            await _detailRepo.UpdateAsync(detail);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteCountAsync(int stockCheckId)
        {
            var stockCheck = await _stockCheckRepo.GetByIdWithDetailsAndBoxesAsync(stockCheckId);
            if (stockCheck == null)
                throw new NotFoundException("Phiếu kiểm kê không tồn tại");
            if (stockCheck.Status != StockCheckStatus.InProgress)
                throw new InvalidBusinessRuleException("Chỉ được chốt đếm khi phiếu đang InProgress");

            if (stockCheck.Details == null || !stockCheck.Details.Any())
                throw new InvalidBusinessRuleException("Phiếu kiểm kê chưa có chi tiết");
            var missing = stockCheck.Details.FirstOrDefault(d => !d.CountedWeight.HasValue);
            if (missing != null)
                throw new InvalidBusinessRuleException($"Còn dòng chưa nhập số đếm (BoxId={missing.BoxId})");

            stockCheck.Status = StockCheckStatus.Counted;
            await _stockCheckRepo.UpdateAsync(stockCheck);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("StockCheck {Id} completed count (Counted)", stockCheckId);
            await _notificationService.NotifyStockCheckPendingManagerAsync(stockCheckId);
        }

        public async Task ApproveAsync(int stockCheckId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var stockCheck = await _stockCheckRepo.GetByIdWithDetailsAndBoxesAsync(stockCheckId);
                if (stockCheck == null)
                    throw new NotFoundException("Phiếu kiểm kê không tồn tại");
                if (stockCheck.Status != StockCheckStatus.Counted)
                    throw new InvalidBusinessRuleException("Chỉ được duyệt khi phiếu ở trạng thái Counted");

                stockCheck.ApprovedBy = userId;
                stockCheck.ApprovedAt = DateTime.UtcNow;
                stockCheck.Status = StockCheckStatus.Approved;
                var affectedSlotIds = new HashSet<int>();

                foreach (var d in stockCheck.Details!)
                {
                    d.CurrentSystemWeight = d.Box?.Weight;
                    if (!d.CountedWeight.HasValue) continue;

                    var box = d.Box;
                    if (box == null) continue;

                    // Keep previous values so we can update Lot.RemainingQuantity by delta.
                    var oldStatus = box.Status;
                    var oldWeight = box.Weight;

                    if (d.VarianceType == VarianceType.Match)
                        continue;

                    var invRequest = new InventoryRequest
                    {
                        RequestType = InventoryRequestType.StockAdjustment,
                        ReferenceType = InventoryReferenceType.StockCheckDetail,
                        ReferenceId = d.Id,
                        Reason = $"Kiểm kê phiếu #{stockCheckId}, Box {box.BoxCode}: chênh lệch {d.DifferenceWeight}",
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        Status = InventoryRequestStatus.Approved,
                        ApprovedBy = userId,
                        ApprovedAt = DateTime.UtcNow
                    };
                    await _inventoryRequestRepo.AddAsync(invRequest);
                    await _unitOfWork.SaveChangesAsync();

                    await _inventoryTranRepo.CreateAsync(new InventoryTransaction
                    {
                        BoxId = box.Id,
                        TransactionType = InventoryTransactionType.Adjust,
                        Quantity = d.DifferenceWeight!.Value,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        InventoryRequestId = invRequest.Id
                    });

                    var previousWeight = box.Weight;
                    box.Weight = d.CountedWeight.Value;
                    var weightDiff = box.Weight - previousWeight;
                    var originalSlotId = box.SlotId;
                    if (originalSlotId.HasValue)
                        affectedSlotIds.Add(originalSlotId.Value);

                    if (originalSlotId.HasValue && weightDiff != 0m)
                    {
                        var slot = box.Slot ?? await _slotRepo.GetByIdAsync(originalSlotId.Value);
                        if (slot != null)
                        {
                            slot.CurrentCapacity = Math.Max(0, slot.CurrentCapacity + weightDiff);
                            await _slotRepo.UpdateAsync(slot);
                        }
                    }

                    // Nếu sau kiểm kê còn 0kg thì coi như box không còn nằm trong slot.
                    if (box.Weight <= 0m)
                    {
                        box.SlotId = null;
                        box.Slot = null;
                    }

                    if (d.VarianceType == VarianceType.Shortage && d.VarianceReason == VarianceReason.Damaged)
                        box.Status = BoxStatus.Damaged;

                    // Keep Lot.RemainingQuantity consistent with what "còn lại" dashboard counts
                    // (Stored/Reserved and weight > 0).
                    if (box.Lot != null)
                    {
                        var oldIncluded =
                            (oldStatus == BoxStatus.Stored || oldStatus == BoxStatus.Reserved) &&
                            oldWeight > 0m;
                        var newIncluded =
                            (box.Status == BoxStatus.Stored || box.Status == BoxStatus.Reserved) &&
                            box.Weight > 0m;

                        var oldQty = oldIncluded ? oldWeight : 0m;
                        var newQty = newIncluded ? box.Weight : 0m;
                        box.Lot.RemainingQuantity = Math.Max(0m, box.Lot.RemainingQuantity + (newQty - oldQty));
                    }
                    await _boxRepo.UpdateAsync(box);
                }

                await _unitOfWork.SaveChangesAsync();

                foreach (var slotId in affectedSlotIds)
                {
                    await _slotRepo.RecalculateCurrentCapacityAsync(slotId);
                }

                await _stockCheckRepo.UpdateAsync(stockCheck);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("StockCheck {Id} approved by {UserId}", stockCheckId, userId);

                await _notificationService.NotifyStockCheckApprovedAsync(stockCheckId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task RejectAsync(int stockCheckId, string userId)
        {
            var stockCheck = await _stockCheckRepo.GetByIdAsync(stockCheckId);
            if (stockCheck == null)
                throw new NotFoundException("Phiếu kiểm kê không tồn tại");
            if (stockCheck.Status != StockCheckStatus.Counted)
                throw new InvalidBusinessRuleException("Chỉ được từ chối khi phiếu ở trạng thái Counted");

            stockCheck.Status = StockCheckStatus.Rejected;
            await _stockCheckRepo.UpdateAsync(stockCheck);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("StockCheck {Id} rejected by {UserId}", stockCheckId, userId);
        }

        public async Task<StockCheckWarehouseDashboardDto> GetWarehouseDashboardAsync(int? warehouseId)
        {
            var statuses = new[]
            {
                StockCheckStatus.Draft,
                StockCheckStatus.InProgress,
                StockCheckStatus.Counted
            };

            var checks = await _stockCheckRepo.GetStockChecksWithDetailsAsync(
                warehouseId,
                statuses);

            StockCheckDashboardItemDto MapItem(StockCheck sc)
            {
                var details = sc.Details ?? Array.Empty<StockCheckDetail>();
                var totalLines = details.Count;
                var countedLines = details.Count(d => d.CountedWeight.HasValue);

                var shortageLines = details.Count(d => d.VarianceType == VarianceType.Shortage);
                var excessLines = details.Count(d => d.VarianceType == VarianceType.Excess);

                return new StockCheckDashboardItemDto
                {
                    StockCheckId = sc.Id,
                    Status = sc.Status.ToString(),
                    CheckType = sc.CheckType.ToString(),
                    SnapshotAt = sc.SnapshotAt,
                    CreatedAt = sc.CreatedAt,
                    TotalLines = totalLines,
                    CountedLines = countedLines,
                    ShortageLines = shortageLines,
                    ExcessLines = excessLines
                };
            }

            return new StockCheckWarehouseDashboardDto
            {
                DraftChecks = checks.Where(c => c.Status == StockCheckStatus.Draft).Select(MapItem).ToList(),
                InProgressChecks = checks.Where(c => c.Status == StockCheckStatus.InProgress).Select(MapItem).ToList(),
                CountedChecks = checks.Where(c => c.Status == StockCheckStatus.Counted).Select(MapItem).ToList()
            };
        }

        public async Task<StockCheckManagerDashboardDto> GetManagerDashboardAsync(int? warehouseId)
        {
            var statuses = new[]
            {
                StockCheckStatus.Counted,
                StockCheckStatus.Approved,
                StockCheckStatus.Rejected
            };

            var checks = await _stockCheckRepo.GetStockChecksWithDetailsAsync(
                warehouseId,
                statuses);

            StockCheckDashboardItemDto MapItem(StockCheck sc)
            {
                var details = sc.Details ?? Array.Empty<StockCheckDetail>();
                var totalLines = details.Count;
                var countedLines = details.Count(d => d.CountedWeight.HasValue);

                var shortageLines = details.Count(d => d.VarianceType == VarianceType.Shortage);
                var excessLines = details.Count(d => d.VarianceType == VarianceType.Excess);

                return new StockCheckDashboardItemDto
                {
                    StockCheckId = sc.Id,
                    Status = sc.Status.ToString(),
                    CheckType = sc.CheckType.ToString(),
                    SnapshotAt = sc.SnapshotAt,
                    CreatedAt = sc.CreatedAt,
                    TotalLines = totalLines,
                    CountedLines = countedLines,
                    ShortageLines = shortageLines,
                    ExcessLines = excessLines
                };
            }

            return new StockCheckManagerDashboardDto
            {
                PendingApprovalChecks = checks.Where(c => c.Status == StockCheckStatus.Counted).Select(MapItem).ToList(),
                ApprovedChecks = checks.Where(c => c.Status == StockCheckStatus.Approved).Select(MapItem).ToList(),
                RejectedChecks = checks.Where(c => c.Status == StockCheckStatus.Rejected).Select(MapItem).ToList()
            };
        }

        public async Task<StockCheckDetailsResponseDto> GetDetailsAsync(int stockCheckId)
        {
            var stockCheck = await _stockCheckRepo.GetByIdWithDetailsAndBoxesAsync(stockCheckId);
            if (stockCheck == null)
                throw new NotFoundException("Phiếu kiểm kê không tồn tại");

            var details = stockCheck.Details ?? new List<StockCheckDetail>();

            return new StockCheckDetailsResponseDto
            {
                StockCheckId = stockCheck.Id,
                WarehouseId = stockCheck.WarehouseId,
                WarehouseName = stockCheck.Warehouse?.Name ?? string.Empty,
                Status = stockCheck.Status.ToString(),
                CheckType = stockCheck.CheckType.ToString(),
                SnapshotAt = stockCheck.SnapshotAt,
                IsLockedSnapshot = stockCheck.IsLockedSnapshot,
                Details = details.Select(d => new StockCheckDetailDto
                {
                    StockCheckDetailId = d.Id,
                    BoxId = d.BoxId,
                    BoxCode = d.Box?.BoxCode ?? string.Empty,
                    LotCode = d.Box?.Lot?.LotCode ?? string.Empty,
                    SlotCode = d.Box?.Slot?.Code,

                    SnapshotWeight = d.SnapshotWeight,
                    CurrentSystemWeight = d.CurrentSystemWeight,
                    CountedWeight = d.CountedWeight,
                    DifferenceWeight = d.DifferenceWeight,

                    VarianceType = d.VarianceType?.ToString(),
                    VarianceReason = d.VarianceReason?.ToString(),

                    CountedBy = d.CountedBy,
                    CountedByName = d.CountedUser?.FullName ?? d.CountedUser?.UserName,
                    CountedAt = d.CountedAt,
                    Note = d.Note
                }).ToList()
            };
        }

        private static VarianceType ComputeVarianceType(decimal differenceWeight)
        {
            if (Math.Abs(differenceWeight) <= VarianceTolerance) return VarianceType.Match;
            return differenceWeight < 0 ? VarianceType.Shortage : VarianceType.Excess;
        }

        private static VarianceReason? NormalizeVarianceReason(VarianceType varianceType, VarianceReason? requestedReason)
        {
            if (varianceType == VarianceType.Match || varianceType == VarianceType.Excess)
                return null;

            if (!requestedReason.HasValue || requestedReason.Value == VarianceReason.None)
                throw new InvalidBusinessRuleException(
                    "Thiếu hàng (Shortage) bắt buộc chọn nguyên nhân (Damaged/Loss/MeasurementError/Other).");

            if (requestedReason is VarianceReason.Damaged or VarianceReason.Loss or VarianceReason.MeasurementError or VarianceReason.Other)
                return requestedReason.Value;

            throw new InvalidBusinessRuleException("Nguyên nhân chênh lệch không hợp lệ.");
        }
    }
}
