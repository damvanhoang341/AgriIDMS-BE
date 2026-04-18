using AgriIDMS.Application.DTOs.DamageReport;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;

namespace AgriIDMS.Application.Services
{
    public class DamageReportService : IDamageReportService
    {
        private const decimal CapacityTolerance = 0.0001m;

        private readonly IDamageReportRepository _damageReportRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly ISlotRepository _slotRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly IInventoryRequestRepository _inventoryRequestRepo;
        private readonly IOrderAllocationRepository _allocationRepo;
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;

        public DamageReportService(
            IDamageReportRepository damageReportRepo,
            IBoxRepository boxRepo,
            ISlotRepository slotRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            IInventoryRequestRepository inventoryRequestRepo,
            IOrderAllocationRepository allocationRepo,
            IUnitOfWork uow,
            INotificationService notificationService)
        {
            _damageReportRepo = damageReportRepo;
            _boxRepo = boxRepo;
            _slotRepo = slotRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _inventoryRequestRepo = inventoryRequestRepo;
            _allocationRepo = allocationRepo;
            _uow = uow;
            _notificationService = notificationService;
        }

        public async Task<DamageReportResponseDto> CreateAsync(CreateDamageReportRequest request, string userId, string username)
        {
            if (request.TargetType != DamageTargetType.Box)
                throw new InvalidBusinessRuleException("Luồng xử lý hiện tại chỉ hỗ trợ báo hỏng theo thùng (Box).");
            if (request.TargetId <= 0)
                throw new InvalidBusinessRuleException("TargetId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(request.TargetCode))
                throw new InvalidBusinessRuleException("TargetCode không được để trống.");
            if (string.IsNullOrWhiteSpace(request.DamageReason))
                throw new InvalidBusinessRuleException("DamageReason không được để trống.");
            if (string.IsNullOrWhiteSpace(request.EvidenceImageUrl))
                throw new InvalidBusinessRuleException("Vui lòng đính kèm ảnh minh chứng.");

            var box = await _boxRepo.GetByIdWithLotAndReceiptAsync(request.TargetId)
                ?? throw new NotFoundException("Thùng không tồn tại.");

            if (box.Status == BoxStatus.Disposed || box.Status == BoxStatus.Exported)
                throw new InvalidBusinessRuleException("Thùng đã xử lý xong / đã xuất, không thể báo hỏng.");
            if (box.Status != BoxStatus.Stored)
                throw new InvalidBusinessRuleException("Chỉ báo hỏng khi thùng đang ở trạng thái Stored.");
            if (box.Weight <= 0)
                throw new InvalidBusinessRuleException("Thùng không còn khối lượng để báo hỏng.");
            if (box.Lot?.Status != LotStatus.Active)
                throw new InvalidBusinessRuleException("Lot của thùng không còn Active.");
            if (box.Lot?.ExpiryDate <= DateTime.UtcNow)
                throw new InvalidBusinessRuleException("Thùng đã hết hạn, không dùng phiếu báo hỏng này.");

            if (await _damageReportRepo.HasPendingForBoxAsync(box.Id))
                throw new InvalidBusinessRuleException("Thùng đang có phiếu hỏng chờ duyệt.");
            if (await _allocationRepo.HasReservedOrPickedAllocationForBoxAsync(box.Id))
                throw new InvalidBusinessRuleException("Thùng đang được giữ/xuất trên đơn (Reserved/Picked), không thể báo hỏng.");

            var warehouseId = box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId ?? 0;
            if (warehouseId <= 0)
                throw new InvalidBusinessRuleException("Không xác định được kho của thùng.");
            if (request.WarehouseId.HasValue && request.WarehouseId.Value > 0 && request.WarehouseId.Value != warehouseId)
                throw new InvalidBusinessRuleException("WarehouseId không khớp với kho của thùng.");

            var variantId = box.Lot?.GoodsReceiptDetail?.ProductVariantId;
            var productName = box.Lot?.GoodsReceiptDetail?.ProductVariant?.Name;

            var entity = new DamageReport
            {
                TargetType = DamageTargetType.Box,
                TargetId = box.Id,
                TargetCode = string.IsNullOrWhiteSpace(request.TargetCode) ? box.BoxCode : request.TargetCode.Trim(),
                ProductVariantId = variantId,
                ProductName = string.IsNullOrWhiteSpace(request.ProductName) ? productName : request.ProductName.Trim(),
                LotId = box.LotId,
                LotCode = string.IsNullOrWhiteSpace(request.LotCode) ? box.Lot?.LotCode : request.LotCode.Trim(),
                WarehouseId = warehouseId,
                WarehouseName = string.IsNullOrWhiteSpace(request.WarehouseName)
                    ? null
                    : request.WarehouseName.Trim(),
                DamageReason = request.DamageReason.Trim(),
                DamagePercent = ClampPercent(request.DamagePercent),
                SuggestedDiscountPercent = 0,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                EvidenceImageUrl = request.EvidenceImageUrl.Trim(),
                ReportedByUserId = userId,
                ReportedByUsername = username,
                ReportedAt = DateTime.UtcNow,
                Status = DamageReportStatus.Pending
            };

            await _damageReportRepo.AddAsync(entity);
            await _uow.SaveChangesAsync();
            await _notificationService.NotifyDamageReportPendingManagerAsync(entity.Id);

            return Map(entity);
        }

        public async Task<IReadOnlyList<DamageReportResponseDto>> GetListAsync(
            DamageReportStatus? status = null,
            int? warehouseId = null,
            string? reportedByUserId = null)
        {
            var list = await _damageReportRepo.GetListAsync(status, warehouseId, reportedByUserId);
            return list.Select(Map).ToList();
        }

        public async Task<DamageReportResponseDto> ApproveAsync(int id, ApproveDamageReportRequest request, string reviewerUserId, string reviewerUsername)
        {
            DamageReport? reportForMap = null;

            await _uow.ExecuteInRetryableTransactionAsync(async () =>
            {
                var report = await _damageReportRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Phiếu hỏng #{id} không tồn tại.");

                if (report.Status != DamageReportStatus.Pending)
                    throw new InvalidBusinessRuleException("Chỉ được duyệt phiếu đang chờ xử lý.");
                if (report.TargetType != DamageTargetType.Box)
                    throw new InvalidBusinessRuleException("Phiếu không phải loại Box, không xử lý được.");

                var box = await _boxRepo.GetByIdWithLotAndReceiptAsync(report.TargetId)
                    ?? throw new NotFoundException("Thùng không tồn tại.");

                if (box.Status == BoxStatus.Disposed)
                    throw new InvalidBusinessRuleException("Thùng đã disposed, không thể duyệt xử lý hỏng.");
                if (box.Status != BoxStatus.Stored)
                    throw new InvalidBusinessRuleException("Chỉ duyệt khi thùng đang Stored.");
                if (box.Weight <= 0)
                    throw new InvalidBusinessRuleException("Thùng không còn khối lượng.");
                if (await _allocationRepo.HasReservedOrPickedAllocationForBoxAsync(box.Id))
                    throw new InvalidBusinessRuleException("Thùng đang Reserved/Picked trên đơn, không thể duyệt loại hỏng.");

                var snapshotWeight = box.Weight;
                report.BoxWeightSnapshotKg = snapshotWeight;
                report.ProcessingOutcome = request.Outcome;

                var now = DateTime.UtcNow;
                var invRequest = new InventoryRequest
                {
                    RequestType = InventoryRequestType.DamageReport,
                    ReferenceType = InventoryReferenceType.DamageReport,
                    ReferenceId = report.Id,
                    Reason = $"Duyệt xử lý hỏng phiếu #{report.Id}, outcome={request.Outcome}",
                    CreatedBy = reviewerUserId,
                    CreatedAt = now,
                    Status = InventoryRequestStatus.Approved,
                    ApprovedBy = reviewerUserId,
                    ApprovedAt = now
                };
                await _inventoryRequestRepo.AddAsync(invRequest);
                await _uow.SaveChangesAsync();

                if (request.Outcome == DamageProcessingOutcome.CompleteDamaged)
                {
                    report.ApprovedDamagedWeightKg = snapshotWeight;
                    await DisposeBoxFullyAsync(box, report.Id, reviewerUserId, now, invRequest.Id);
                }
                else if (request.Outcome == DamageProcessingOutcome.PartialDamaged)
                {
                    if (!request.DamagedWeightKg.HasValue)
                        throw new InvalidBusinessRuleException("PartialDamaged cần DamagedWeightKg.");
                    var damaged = request.DamagedWeightKg.Value;
                    if (damaged <= 0 || damaged > snapshotWeight)
                        throw new InvalidBusinessRuleException("DamagedWeightKg phải > 0 và không vượt quá khối lượng hiện có của thùng.");

                    report.ApprovedDamagedWeightKg = damaged;
                    await ReduceBoxByDamagedWeightAsync(box, report.Id, damaged, reviewerUserId, now, invRequest.Id);
                }
                else
                    throw new InvalidBusinessRuleException("Outcome không hợp lệ.");

                report.Status = DamageReportStatus.Approved;
                report.ReviewedByUserId = reviewerUserId;
                report.ReviewedByUsername = reviewerUsername;
                report.ReviewedAt = now;
                report.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote) ? null : request.ReviewNote.Trim();
                report.AppliedDiscountPercent = null;
                report.UpdatedAt = now;

                reportForMap = report;
            });

            return Map(reportForMap!);
        }

        public async Task<DamageReportResponseDto> RejectAsync(int id, RejectDamageReportRequest request, string reviewerUserId, string reviewerUsername)
        {
            var report = await _damageReportRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Phiếu hỏng #{id} không tồn tại.");

            if (report.Status != DamageReportStatus.Pending)
                throw new InvalidBusinessRuleException("Chỉ được từ chối phiếu đang chờ xử lý.");

            report.Status = DamageReportStatus.Rejected;
            report.ReviewedByUserId = reviewerUserId;
            report.ReviewedByUsername = reviewerUsername;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote)
                ? "Từ chối xử lý hỏng."
                : request.ReviewNote.Trim();
            report.ProcessingOutcome = null;
            report.ApprovedDamagedWeightKg = null;
            report.BoxWeightSnapshotKg = null;
            report.AppliedDiscountPercent = null;
            report.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            return Map(report);
        }

        private async Task DisposeBoxFullyAsync(Box box, int damageReportId, string reviewerUserId, DateTime now, int inventoryRequestId)
        {
            var removedWeight = box.Weight;
            if (removedWeight <= 0) return;

            var removedVolume = CapacityVolume(box);
            var fromSlotId = box.SlotId;

            if (box.SlotId.HasValue)
            {
                var slot = await _slotRepo.GetByIdAsync(box.SlotId.Value);
                if (slot != null)
                {
                    slot.CurrentCapacity = Math.Max(0, slot.CurrentCapacity - removedVolume);
                    await _slotRepo.UpdateAsync(slot);
                }
            }

            if (box.Lot != null)
                box.Lot.RemainingQuantity = Math.Max(0, box.Lot.RemainingQuantity - removedWeight);

            box.SlotId = null;
            box.Status = BoxStatus.Disposed;
            box.Weight = 0;
            box.VolumeM3 = 0;
            await _boxRepo.UpdateAsync(box);

            await _inventoryTranRepo.CreateAsync(new InventoryTransaction
            {
                BoxId = box.Id,
                TransactionType = InventoryTransactionType.Dispose,
                FromSlotId = fromSlotId,
                ToSlotId = null,
                Quantity = removedWeight,
                ReferenceType = ReferenceType.Adjustment,
                ReferenceRequestId = damageReportId,
                CreatedBy = string.IsNullOrWhiteSpace(reviewerUserId) ? "system" : reviewerUserId,
                CreatedAt = now,
                InventoryRequestId = inventoryRequestId
            });
        }

        private async Task ReduceBoxByDamagedWeightAsync(Box box, int damageReportId, decimal damagedWeightKg, string reviewerUserId, DateTime now, int inventoryRequestId)
        {
            var oldWeight = box.Weight;
            if (oldWeight <= 0) return;

            var damagedVolume = VolumeForWeightPortion(box, damagedWeightKg, oldWeight);
            var fromSlotId = box.SlotId;

            if (box.SlotId.HasValue && damagedVolume > CapacityTolerance)
            {
                var slot = await _slotRepo.GetByIdAsync(box.SlotId.Value);
                if (slot != null)
                {
                    slot.CurrentCapacity = Math.Max(0, slot.CurrentCapacity - damagedVolume);
                    await _slotRepo.UpdateAsync(slot);
                }
            }

            if (box.Lot != null)
                box.Lot.RemainingQuantity = Math.Max(0, box.Lot.RemainingQuantity - damagedWeightKg);

            var newWeight = oldWeight - damagedWeightKg;
            var newVolumeM3 = RemainingVolumeAfterPartialDamage(box, newWeight, oldWeight);
            box.Weight = newWeight;
            box.VolumeM3 = newWeight <= 0 ? 0m : newVolumeM3;

            if (newWeight <= 0)
            {
                box.SlotId = null;
                box.Status = BoxStatus.Disposed;
                box.Weight = 0;
                box.VolumeM3 = 0;
            }
            else
                box.Status = BoxStatus.Stored;

            await _boxRepo.UpdateAsync(box);

            await _inventoryTranRepo.CreateAsync(new InventoryTransaction
            {
                BoxId = box.Id,
                TransactionType = InventoryTransactionType.Adjust,
                FromSlotId = fromSlotId,
                ToSlotId = fromSlotId,
                Quantity = -damagedWeightKg,
                ReferenceType = ReferenceType.Adjustment,
                ReferenceRequestId = damageReportId,
                CreatedBy = string.IsNullOrWhiteSpace(reviewerUserId) ? "system" : reviewerUserId,
                CreatedAt = now,
                InventoryRequestId = inventoryRequestId
            });
        }

        /// <summary>Tính thể tích còn lại từ trạng thái thùng trước khi đổi <see cref="Box.Weight"/>.</summary>
        private static decimal RemainingVolumeAfterPartialDamage(Box box, decimal newWeightKg, decimal oldWeightKg)
        {
            if (newWeightKg <= CapacityTolerance) return 0m;
            if (oldWeightKg <= CapacityTolerance) return 0m;

            if (box.VolumeM3 > CapacityTolerance)
                return box.VolumeM3 * (newWeightKg / oldWeightKg);

            var density = box.Lot?.GoodsReceiptDetail?.ProductVariant?.DensityKgPerM3 ?? 0m;
            if (density > CapacityTolerance)
                return newWeightKg / density;

            return 0m;
        }

        private static decimal VolumeForWeightPortion(Box box, decimal portionWeightKg, decimal totalWeightKg)
        {
            if (totalWeightKg <= CapacityTolerance) return 0m;
            var fullVol = CapacityVolume(box);
            if (fullVol > CapacityTolerance)
                return fullVol * (portionWeightKg / totalWeightKg);

            var density = box.Lot?.GoodsReceiptDetail?.ProductVariant?.DensityKgPerM3 ?? 0m;
            if (density > CapacityTolerance)
                return portionWeightKg / density;

            return 0m;
        }

        private static decimal CapacityVolume(Box box)
        {
            if (box.VolumeM3 > CapacityTolerance) return box.VolumeM3;
            var density = box.Lot?.GoodsReceiptDetail?.ProductVariant?.DensityKgPerM3 ?? 0m;
            if (density > CapacityTolerance && box.Weight > CapacityTolerance)
                return box.Weight / density;

            return 0m;
        }

        private static decimal ClampPercent(decimal value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static DamageReportResponseDto Map(DamageReport x)
        {
            return new DamageReportResponseDto
            {
                Id = x.Id,
                TargetType = x.TargetType.ToString(),
                TargetId = x.TargetId,
                TargetCode = x.TargetCode,
                ProductVariantId = x.ProductVariantId,
                ProductName = x.ProductName,
                LotId = x.LotId,
                LotCode = x.LotCode,
                WarehouseId = x.WarehouseId,
                WarehouseName = x.WarehouseName,
                DamageReason = x.DamageReason,
                DamagePercent = x.DamagePercent,
                SuggestedDiscountPercent = x.SuggestedDiscountPercent,
                Note = x.Note,
                EvidenceImageUrl = x.EvidenceImageUrl,
                ReportedByUserId = x.ReportedByUserId,
                ReportedByUsername = x.ReportedByUsername,
                ReportedAt = x.ReportedAt,
                Status = x.Status.ToString(),
                ReviewedByUserId = x.ReviewedByUserId,
                ReviewedByUsername = x.ReviewedByUsername,
                ReviewedAt = x.ReviewedAt,
                ReviewNote = x.ReviewNote,
                AppliedDiscountPercent = x.AppliedDiscountPercent,
                ProcessingOutcome = x.ProcessingOutcome?.ToString(),
                ApprovedDamagedWeightKg = x.ApprovedDamagedWeightKg,
                BoxWeightSnapshotKg = x.BoxWeightSnapshotKg
            };
        }
    }
}
