using AgriIDMS.Application.DTOs.Lot;
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
    public class LotService : ILotService
    {
        private readonly ILotRepository _lotRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INearExpiryDiscountRuleRepository _nearExpiryRuleRepo;
        private readonly IProductVariantDiscountOverrideRepository _variantOverrideRepo;

        public LotService(
            ILotRepository lotRepository,
            IUnitOfWork unitOfWork,
            INearExpiryDiscountRuleRepository nearExpiryRuleRepo,
            IProductVariantDiscountOverrideRepository variantOverrideRepo)
        {
            _lotRepository = lotRepository;
            _unitOfWork = unitOfWork;
            _nearExpiryRuleRepo = nearExpiryRuleRepo;
            _variantOverrideRepo = variantOverrideRepo;
        }

        public async Task<List<LotListItemDto>> GetAllLotsAsync()
        {
            var lots = await _lotRepository.GetAllWithContextAsync();
            return lots.Select(l =>
            {
                var detail = l.GoodsReceiptDetail;
                var productVariant = l.ProductVariant;
                var remainingFromBoxes = l.Boxes
                    .Where(b => (b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved) && b.Weight > 0m)
                    .Sum(b => b.Weight);
                return new LotListItemDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    QrImageUrl = l.QrImageUrl,
                    TotalQuantity = l.TotalQuantity,
                    RemainingQuantity = remainingFromBoxes,
                    ReceivedDate = l.ReceivedDate,
                    ExpiryDate = l.ExpiryDate,
                    Status = l.Status.ToString(),
                    GoodsReceiptId = detail?.GoodsReceiptId ?? 0,
                    ProductName = productVariant?.Product?.Name ?? string.Empty,
                    ProductVariantId = productVariant?.Id ?? 0,
                    ProductVariantName = productVariant?.Name ?? string.Empty,
                    WarehouseName = detail?.GoodsReceipt?.Warehouse?.Name ?? string.Empty
                };
            }).ToList();
        }

        public async Task<LotDetailDto> GetLotDetailAsync(int lotId)
        {
            var lot = await _lotRepository.GetByIdWithContextAndBoxesAsync(lotId);
            if (lot == null)
                throw new NotFoundException("Lot không tồn tại");

            var detail = lot.GoodsReceiptDetail;
            var productVariant = lot.ProductVariant;

            return new LotDetailDto
            {
                LotId = lot.Id,
                LotCode = lot.LotCode,
                QrImageUrl = lot.QrImageUrl,
                TotalQuantity = lot.TotalQuantity,
                // Use real box stock to avoid stale Lot.RemainingQuantity after stock-check.
                RemainingQuantity = lot.Boxes
                    .Where(b => (b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved) && b.Weight > 0m)
                    .Sum(b => b.Weight),
                ReceivedDate = lot.ReceivedDate,
                ExpiryDate = lot.ExpiryDate,
                Status = lot.Status.ToString(),
                GoodsReceiptId = detail?.GoodsReceiptId ?? 0,
                ProductName = productVariant?.Product?.Name ?? string.Empty,
                ProductVariantName = productVariant?.Name ?? string.Empty,
                WarehouseName = detail?.GoodsReceipt?.Warehouse?.Name ?? string.Empty,
                Boxes = lot.Boxes
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new LotBoxItemDto
                    {
                        // Fallback cho box cũ chưa backfill VolumeM3.
                        VolumeM3 = b.VolumeM3 > 0
                            ? b.VolumeM3
                            : ((productVariant?.DensityKgPerM3 ?? 0m) > 0
                                ? b.Weight / productVariant!.DensityKgPerM3
                                : 0m),
                        BoxId = b.Id,
                        BoxCode = b.BoxCode,
                        Weight = b.Weight,
                        Status = b.Status.ToString(),
                        SlotId = b.SlotId,
                        SlotCode = b.Slot?.Code,
                        QrCode = b.QRCode,
                        QrImageUrl = b.QrImageUrl,
                        CreatedAt = b.CreatedAt
                    })
                    .ToList()
            };
        }

        public async Task<List<LotListItemDto>> GetLotsByGoodsReceiptIdAsync(int goodsReceiptId)
        {
            var lots = await _lotRepository.GetByGoodsReceiptIdAsync(goodsReceiptId);

            if (lots == null || !lots.Any())
                return new List<LotListItemDto>();

            return lots.Select(l =>
            {
                var detail = l.GoodsReceiptDetail;
                var productVariant = l.ProductVariant;
                var remainingFromBoxes = l.Boxes
                    .Where(b => (b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved) && b.Weight > 0m)
                    .Sum(b => b.Weight);
                return new LotListItemDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    QrImageUrl = l.QrImageUrl,
                    TotalQuantity = l.TotalQuantity,
                    RemainingQuantity = remainingFromBoxes,
                    ReceivedDate = l.ReceivedDate,
                    ExpiryDate = l.ExpiryDate,
                    Status = l.Status.ToString(),
                    GoodsReceiptId = detail?.GoodsReceiptId ?? 0,
                    ProductName = productVariant?.Product?.Name ?? string.Empty,
                    ProductVariantId = productVariant?.Id ?? 0,
                    ProductVariantName = productVariant?.Name ?? string.Empty,
                    WarehouseName = detail?.GoodsReceipt?.Warehouse?.Name ?? string.Empty
                };
            }).ToList();
        }

        public async Task<List<LotListItemDto>> GetLotsByProductVariantIdAsync(int productVariantId)
        {
            if (productVariantId <= 0)
                throw new InvalidBusinessRuleException("Mã biến thể sản phẩm phải lớn hơn 0.");

            var lots = await _lotRepository.GetByProductVariantIdAsync(productVariantId);
            if (lots == null || !lots.Any())
                return new List<LotListItemDto>();

            return lots.Select(l =>
            {
                var detail = l.GoodsReceiptDetail;
                var productVariant = l.ProductVariant;
                var remainingFromBoxes = l.Boxes
                    .Where(b => (b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved) && b.Weight > 0m)
                    .Sum(b => b.Weight);
                return new LotListItemDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    QrImageUrl = l.QrImageUrl,
                    TotalQuantity = l.TotalQuantity,
                    RemainingQuantity = remainingFromBoxes,
                    ReceivedDate = l.ReceivedDate,
                    ExpiryDate = l.ExpiryDate,
                    Status = l.Status.ToString(),
                    GoodsReceiptId = detail?.GoodsReceiptId ?? 0,
                    ProductName = productVariant?.Product?.Name ?? string.Empty,
                    ProductVariantId = productVariant?.Id ?? 0,
                    ProductVariantName = productVariant?.Name ?? string.Empty,
                    WarehouseName = detail?.GoodsReceipt?.Warehouse?.Name ?? string.Empty
                };
            }).ToList();
        }

        public async Task UpdateQrImageUrlAsync(int lotId, string qrImageUrl)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);
            if (lot == null)
                throw new NotFoundException("Lot không tồn tại");

            lot.QrImageUrl = qrImageUrl.Trim();
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<object?> GetByLotCodeAsync(string lotCode)
        {
            if (string.IsNullOrWhiteSpace(lotCode))
                return null;

            var lot = await _lotRepository.GetByLotCodeAsync(lotCode.Trim());
            if (lot == null)
                return null;

            var productVariant = lot.ProductVariant;
            var product = productVariant?.Product;
            var goodsReceipt = lot.GoodsReceiptDetail?.GoodsReceipt;

            return new
            {
                id = lot.Id,
                lotCode = lot.LotCode,
                qrImageUrl = lot.QrImageUrl,
                expiryDate = lot.ExpiryDate,
                receivedDate = lot.ReceivedDate,
                totalQuantity = lot.TotalQuantity,
                remainingQuantity = lot.RemainingQuantity,
                status = lot.Status.ToString(),
                productVariantId = productVariant?.Id,
                productVariantName = productVariant?.Name,
                productName = product?.Name,
                warehouseId = goodsReceipt?.WarehouseId
            };
        }

        public async Task<IEnumerable<NearExpiryLotDto>> GetNearExpiryLotsAsync()
        {
            var dashboard = await GetNearExpiryDashboardAsync(3);
            return dashboard.Lots;
        }

        public async Task<NearExpiryDashboardDto> GetNearExpiryDashboardAsync(int days, int? warehouseId = null)
        {
            if (days <= 0)
                throw new InvalidBusinessRuleException("Số ngày lọc phải lớn hơn 0");

            var todayUtc = DateTime.UtcNow.Date;
            var lots = await _lotRepository.GetNearExpiryLotsAsync(days, warehouseId);
            var rules = await _nearExpiryRuleRepo.GetActiveRulesAsync(DateTime.UtcNow);
            if (lots == null || !lots.Any())
            {
                return new NearExpiryDashboardDto
                {
                    DaysThreshold = days,
                    TotalLots = 0,
                    TotalBoxes = 0,
                    Lots = new List<NearExpiryLotDto>()
                };
            }

            var mappedLots = lots.Select(l =>
            {
                var nearExpiryBoxes = l.Boxes
                    .Where(b => b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved)
                    .Select(b => new NearExpiryBoxDto
                    {
                        BoxId = b.Id,
                        BoxCode = b.BoxCode,
                        Weight = b.Weight,
                        IsPartial = b.IsPartial,
                        Status = b.Status.ToString(),
                        SlotId = b.SlotId,
                        SlotCode = b.Slot?.Code
                    })
                    .ToList();

                var daysLeft = (l.ExpiryDate.Date - todayUtc).Days;
                var suggestedDiscountPercent = GetSuggestedDiscountPercent(daysLeft, rules);

                var variant = l.ProductVariant;
                return new NearExpiryLotDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    ProductVariantId = variant.Id,
                    ProductName = variant.Product?.Name ?? string.Empty,
                    ProductVariantName = variant.Name ?? string.Empty,
                    Grade = variant.Grade.ToString(),
                    // RemainingQuantity should reflect boxes that are still "usable":
                    // Stored/Reserved with weight > 0. (Don't trust Lot.RemainingQuantity after stock-check.)
                    RemainingQuantity = nearExpiryBoxes.Sum(b => b.Weight),
                    ExpiryDate = l.ExpiryDate,
                    DaysLeft = daysLeft,
                    NearExpiryBoxCount = nearExpiryBoxes.Count(),
                    Boxes = nearExpiryBoxes,
                    WarehouseId = l.GoodsReceiptDetail.GoodsReceipt.WarehouseId,
                    WarehouseName = l.GoodsReceiptDetail.GoodsReceipt.Warehouse?.Name ?? string.Empty,
                    Status = l.ExpiryDate.Date < todayUtc ? "Expired" : "NearExpiry",
                    SuggestedDiscountPercent = suggestedDiscountPercent
                };
            }).ToList();

            return new NearExpiryDashboardDto
            {
                DaysThreshold = days,
                TotalLots = mappedLots.Count(),
                TotalBoxes = mappedLots.Sum(x => x.NearExpiryBoxCount),
                Lots = mappedLots
            };
        }

        public async Task<List<NearExpiryDiscountRuleDto>> GetNearExpiryDiscountRulesAsync()
        {
            var rules = await _nearExpiryRuleRepo.GetAllRulesAsync();
            return rules
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .Select(r => new NearExpiryDiscountRuleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    MinDaysLeft = r.MinDaysLeft,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority > 0 ? r.Priority : 1,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();
        }

        public async Task UpdateNearExpiryDiscountRulesAsync(string userId, List<UpsertNearExpiryDiscountRuleDto> rules)
        {
            if (rules == null)
                throw new InvalidBusinessRuleException("Danh sách quy tắc giảm giá không hợp lệ.");

            // NearExpiryDiscountRule.StartAtUtc / EndAtUtc: vẫn có trên entity/DB nhưng không còn dùng cho màn cấu hình;
            // luôn ghi null khi replace. (Có thể migration drop cột sau nếu muốn dọn schema.)
            var normalized = rules
                .Select(r => new UpsertNearExpiryDiscountRuleDto
                {
                    Name = string.IsNullOrWhiteSpace(r.Name) ? null : r.Name.Trim(),
                    MinDaysLeft = r.MinDaysLeft,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority,
                    IsActive = r.IsActive
                })
                .ToList();

            foreach (var r in normalized)
            {
                if (r.MinDaysLeft.HasValue && r.MinDaysLeft.Value < 0)
                    throw new InvalidBusinessRuleException("Số ngày tối thiểu phải lớn hơn hoặc bằng 0.");
                if (r.MaxDaysLeft <= 0)
                    throw new InvalidBusinessRuleException("Số ngày tối đa phải lớn hơn 0.");
                if (r.MinDaysLeft.HasValue && r.MinDaysLeft.Value > r.MaxDaysLeft)
                    throw new InvalidBusinessRuleException("Số ngày tối thiểu phải nhỏ hơn hoặc bằng số ngày tối đa.");
                if (r.DiscountPercent < 0 || r.DiscountPercent > 100)
                    throw new InvalidBusinessRuleException("Phần trăm giảm giá phải nằm trong khoảng từ 0 đến 100.");
                if (r.Priority <= 0)
                    throw new InvalidBusinessRuleException("Độ ưu tiên phải lớn hơn 0.");
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .Select(r => new NearExpiryDiscountRule
                {
                    Name = string.IsNullOrWhiteSpace(r.Name)
                        ? $"Near-expiry <= {r.MaxDaysLeft} day(s)"
                        : r.Name,
                    MaxDaysLeft = r.MaxDaysLeft,
                    MinDaysLeft = r.MinDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority,
                    IsActive = r.IsActive,
                    StartAtUtc = null,
                    EndAtUtc = null,
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _nearExpiryRuleRepo.ReplaceAllRulesAsync(entities);
        }

        public async Task<List<ProductVariantDiscountOverrideDto>> GetProductVariantDiscountOverridesAsync()
        {
            var rules = await _variantOverrideRepo.GetAllAsync();
            var result = new List<ProductVariantDiscountOverrideDto>();
            foreach (var rule in rules)
            {
                var (lotId, reason) = ParseEmbeddedLot(rule.Reason);

                result.Add(new ProductVariantDiscountOverrideDto
                {
                    Id = rule.Id,
                    ProductVariantId = rule.ProductVariantId,
                    LotId = lotId,
                    Priority = rule.Priority,
                    OverrideNearExpiryDiscountPercent = rule.OverrideNearExpiryDiscountPercent,
                    Reason = reason,
                    IsActive = rule.IsActive,
                    StartAtUtc = rule.StartAtUtc,
                    EndAtUtc = rule.EndAtUtc,
                    CreatedAt = rule.CreatedAt,
                    UpdatedAt = rule.UpdatedAt
                });
            }

            return result
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.ProductVariantId)
                .ThenBy(x => x.LotId ?? int.MaxValue)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public async Task UpdateProductVariantDiscountOverridesAsync(
            string userId,
            List<UpsertProductVariantDiscountOverrideDto> overrides)
        {
            if (overrides == null)
                throw new InvalidBusinessRuleException("Danh sách cấu hình ghi đè không hợp lệ.");

            var normalized = overrides.Select(x => new UpsertProductVariantDiscountOverrideDto
            {
                ProductVariantId = x.ProductVariantId,
                LotId = x.LotId,
                Priority = x.Priority,
                OverrideNearExpiryDiscountPercent = x.OverrideNearExpiryDiscountPercent,
                Reason = string.IsNullOrWhiteSpace(x.Reason) ? null : x.Reason.Trim(),
                IsActive = x.IsActive,
                StartAtUtc = x.StartAtUtc,
                EndAtUtc = x.EndAtUtc
            }).ToList();

            var priorityGroups = normalized
                .GroupBy(x => x.Priority)
                .Where(g => g.Key > 0 && g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (priorityGroups.Count > 0)
                throw new InvalidBusinessRuleException("Độ ưu tiên không được trùng nhau giữa các dòng cấu hình.");

            foreach (var item in normalized)
            {
                if (item.ProductVariantId <= 0)
                    throw new InvalidBusinessRuleException("Mã biến thể sản phẩm phải lớn hơn 0.");
                if (item.Priority <= 0)
                    throw new InvalidBusinessRuleException("Độ ưu tiên phải là số nguyên dương.");
                if (item.OverrideNearExpiryDiscountPercent < 0 || item.OverrideNearExpiryDiscountPercent > 100)
                    throw new InvalidBusinessRuleException("Mức giảm giá ghi đè phải trong khoảng 0-100.");
                if (!item.StartAtUtc.HasValue || !item.EndAtUtc.HasValue)
                    throw new InvalidBusinessRuleException("Thời gian bắt đầu và kết thúc hiệu lực là bắt buộc.");
                if (item.StartAtUtc.Value >= item.EndAtUtc.Value)
                    throw new InvalidBusinessRuleException("Thời gian hiệu lực không hợp lệ: thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

                DateTime? referenceReceiptDate = null;
                if (item.LotId.HasValue && item.LotId.Value > 0)
                {
                    var lot = await _lotRepository.GetByIdWithDetailAndReceiptAsync(item.LotId.Value)
                        ?? throw new NotFoundException($"Lot #{item.LotId.Value} không tồn tại.");
                    var lotProductVariantId = lot.ProductVariantId;
                    if (lotProductVariantId != item.ProductVariantId)
                        throw new InvalidBusinessRuleException(
                            $"Lot #{item.LotId.Value} không thuộc biến thể sản phẩm #{item.ProductVariantId}.");
                    referenceReceiptDate = lot.ReceivedDate;
                }
                else
                {
                    var variantLots = await _lotRepository.GetByProductVariantIdAsync(item.ProductVariantId);
                    if (variantLots.Count > 0)
                        referenceReceiptDate = variantLots.Min(l => l.ReceivedDate);
                }

                if (referenceReceiptDate.HasValue
                    && !EffectiveStartCalendarDayIsAfterReceiptDay(item.StartAtUtc.Value, referenceReceiptDate.Value))
                {
                    throw new InvalidBusinessRuleException(
                        "Ngày bắt đầu hiệu lực phải sau ngày nhập hàng.");
                }
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .Select(item => new ProductVariantDiscountOverride
                {
                    ProductVariantId = item.ProductVariantId,
                    Priority = item.Priority,
                    OverrideNearExpiryDiscountPercent = item.OverrideNearExpiryDiscountPercent,
                    Reason = BuildEmbeddedLotReason(item.LotId, item.Reason),
                    IsActive = item.IsActive,
                    StartAtUtc = item.StartAtUtc,
                    EndAtUtc = item.EndAtUtc,
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _variantOverrideRepo.ReplaceAllAsync(entities);
        }

        /// <summary>
        /// So sánh theo ngày lịch (UTC date) để khớp FE chỉ chọn ngày — StartAtUtc/EndAtUtc là mốc UTC từ đầu/cuối ngày local.
        /// </summary>
        private static bool EffectiveStartCalendarDayIsAfterReceiptDay(DateTime startUtc, DateTime received)
        {
            var s = startUtc.Kind == DateTimeKind.Utc ? startUtc : startUtc.ToUniversalTime();
            var r = received.Kind == DateTimeKind.Utc ? received : received.ToUniversalTime();
            var startDay = new DateTime(s.Year, s.Month, s.Day, 0, 0, 0, DateTimeKind.Utc);
            var receiptDay = new DateTime(r.Year, r.Month, r.Day, 0, 0, 0, DateTimeKind.Utc);
            return startDay > receiptDay;
        }

        private static decimal GetSuggestedDiscountPercent(int daysLeft, List<NearExpiryDiscountRule> rules)
        {
            if (rules == null || rules.Count == 0)
                return 0m;
            if (daysLeft < 0)
                return 0m;
            foreach (var rule in rules.OrderBy(r => r.Priority).ThenBy(r => r.MaxDaysLeft))
            {
                if (!rule.IsActive) continue;
                if (rule.MinDaysLeft.HasValue && daysLeft < rule.MinDaysLeft.Value) continue;
                if (daysLeft <= rule.MaxDaysLeft)
                    return rule.DiscountPercent;
            }

            return 0m;
        }

        private static string? BuildEmbeddedLotReason(int? lotId, string? reason)
        {
            var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            if (!lotId.HasValue || lotId.Value <= 0)
                return cleanReason;
            return cleanReason == null
                ? $"[LOT:{lotId.Value}]"
                : $"[LOT:{lotId.Value}] {cleanReason}";
        }

        private static (int? lotId, string? reason) ParseEmbeddedLot(string? rawReason)
        {
            if (string.IsNullOrWhiteSpace(rawReason))
                return (null, null);
            var text = rawReason.Trim();
            if (!text.StartsWith("[LOT:", StringComparison.OrdinalIgnoreCase))
                return (null, text);

            var closeBracket = text.IndexOf(']');
            if (closeBracket <= 5)
                return (null, text);
            var numberPart = text.Substring(5, closeBracket - 5);
            if (!int.TryParse(numberPart, out var lotId) || lotId <= 0)
                return (null, text);

            var reasonPart = text[(closeBracket + 1)..].Trim();
            return (lotId, string.IsNullOrWhiteSpace(reasonPart) ? null : reasonPart);
        }
    }
}
