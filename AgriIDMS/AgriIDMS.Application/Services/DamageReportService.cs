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
        private readonly IDamageReportRepository _damageReportRepo;
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;

        public DamageReportService(
            IDamageReportRepository damageReportRepo,
            IUnitOfWork uow,
            INotificationService notificationService)
        {
            _damageReportRepo = damageReportRepo;
            _uow = uow;
            _notificationService = notificationService;
        }

        public async Task<DamageReportResponseDto> CreateAsync(CreateDamageReportRequest request, string userId, string username)
        {
            if (request.TargetId <= 0)
                throw new InvalidBusinessRuleException("TargetId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(request.TargetCode))
                throw new InvalidBusinessRuleException("TargetCode không được để trống.");
            if (string.IsNullOrWhiteSpace(request.DamageReason))
                throw new InvalidBusinessRuleException("DamageReason không được để trống.");
            if (string.IsNullOrWhiteSpace(request.EvidenceImageUrl))
                throw new InvalidBusinessRuleException("Vui lòng đính kèm ảnh minh chứng.");

            var entity = new DamageReport
            {
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                TargetCode = request.TargetCode.Trim(),
                ProductVariantId = request.ProductVariantId,
                ProductName = string.IsNullOrWhiteSpace(request.ProductName) ? null : request.ProductName.Trim(),
                LotId = request.LotId,
                LotCode = string.IsNullOrWhiteSpace(request.LotCode) ? null : request.LotCode.Trim(),
                WarehouseId = request.WarehouseId,
                WarehouseName = string.IsNullOrWhiteSpace(request.WarehouseName) ? null : request.WarehouseName.Trim(),
                DamageReason = request.DamageReason.Trim(),
                DamagePercent = NormalizePercent(request.DamagePercent),
                SuggestedDiscountPercent = NormalizePercent(request.SuggestedDiscountPercent),
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

        public async Task<IReadOnlyList<DamageReportResponseDto>> GetListAsync(DamageReportStatus? status = null)
        {
            var list = await _damageReportRepo.GetListAsync(status);
            return list.Select(Map).ToList();
        }

        public async Task<DamageReportResponseDto> ApproveAsync(int id, ApproveDamageReportRequest request, string reviewerUserId, string reviewerUsername)
        {
            var report = await _damageReportRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Phiếu hỏng #{id} không tồn tại.");

            if (report.Status != DamageReportStatus.Pending)
                throw new InvalidBusinessRuleException("Chỉ được duyệt phiếu đang chờ xử lý.");

            report.Status = DamageReportStatus.Approved;
            report.ReviewedByUserId = reviewerUserId;
            report.ReviewedByUsername = reviewerUsername;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote) ? null : request.ReviewNote.Trim();
            report.AppliedDiscountPercent = NormalizePercent(request.DiscountPercent);
            report.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            return Map(report);
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
                ? "Không đạt điều kiện áp giảm giá."
                : request.ReviewNote.Trim();
            report.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            return Map(report);
        }

        private static decimal NormalizePercent(decimal value)
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
                AppliedDiscountPercent = x.AppliedDiscountPercent
            };
        }
    }
}

