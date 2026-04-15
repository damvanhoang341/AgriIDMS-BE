using AgriIDMS.Application.DTOs.Lot;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
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
                var productVariant = detail?.ProductVariant;
                return new LotListItemDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    QrImageUrl = l.QrImageUrl,
                    TotalQuantity = l.TotalQuantity,
                    RemainingQuantity = l.RemainingQuantity,
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
            var productVariant = detail?.ProductVariant;

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
                var productVariant = detail?.ProductVariant;
                return new LotListItemDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    QrImageUrl = l.QrImageUrl,
                    TotalQuantity = l.TotalQuantity,
                    RemainingQuantity = l.RemainingQuantity,
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

            var productVariant = lot.GoodsReceiptDetail?.ProductVariant;
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

                return new NearExpiryLotDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    ProductVariantId = l.GoodsReceiptDetail.ProductVariant.Id,
                    ProductName = l.GoodsReceiptDetail.ProductVariant.Product.Name,
                    Grade = l.GoodsReceiptDetail.ProductVariant.Grade.ToString(),
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
                .ThenBy(r => r.Id)
                .Select(r => new NearExpiryDiscountRuleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    MinDaysLeft = r.MinDaysLeft,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority,
                    IsActive = r.IsActive,
                    StartAtUtc = r.StartAtUtc,
                    EndAtUtc = r.EndAtUtc,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();
        }

        public async Task UpdateNearExpiryDiscountRulesAsync(string userId, List<UpsertNearExpiryDiscountRuleDto> rules)
        {
            if (rules == null)
                throw new InvalidBusinessRuleException("Rules không hợp lệ");

            var normalized = rules
                .Select(r => new UpsertNearExpiryDiscountRuleDto
                {
                    Name = r.Name?.Trim() ?? string.Empty,
                    MinDaysLeft = r.MinDaysLeft,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority,
                    IsActive = r.IsActive,
                    StartAtUtc = r.StartAtUtc,
                    EndAtUtc = r.EndAtUtc
                })
                .ToList();

            foreach (var r in normalized)
            {
                if (r.MaxDaysLeft <= 0)
                    throw new InvalidBusinessRuleException("MaxDaysLeft phải lớn hơn 0");
                if (r.MinDaysLeft.HasValue && r.MinDaysLeft.Value < 0)
                    throw new InvalidBusinessRuleException("MinDaysLeft phải >= 0");
                if (r.MinDaysLeft.HasValue && r.MinDaysLeft.Value > r.MaxDaysLeft)
                    throw new InvalidBusinessRuleException("MinDaysLeft không được lớn hơn MaxDaysLeft");
                if (r.DiscountPercent < 0 || r.DiscountPercent > 100)
                    throw new InvalidBusinessRuleException("DiscountPercent phải trong khoảng 0-100");
                if (r.Priority <= 0)
                    throw new InvalidBusinessRuleException("Priority phải lớn hơn 0");
                if (r.StartAtUtc.HasValue && r.EndAtUtc.HasValue && r.StartAtUtc > r.EndAtUtc)
                    throw new InvalidBusinessRuleException("Khoảng thời gian hiệu lực không hợp lệ");
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .Select(r => new Domain.Entities.NearExpiryDiscountRule
                {
                    Name = string.IsNullOrWhiteSpace(r.Name) ? $"Near-expiry <= {r.MaxDaysLeft} day(s)" : r.Name.Trim(),
                    MinDaysLeft = r.MinDaysLeft,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority,
                    IsActive = r.IsActive,
                    StartAtUtc = r.StartAtUtc,
                    EndAtUtc = r.EndAtUtc,
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _nearExpiryRuleRepo.ReplaceAllRulesAsync(entities);
        }

        public async Task<List<ProductVariantDiscountOverrideDto>> GetProductVariantDiscountOverridesAsync()
        {
            var overrides = await _variantOverrideRepo.GetAllAsync();
            return overrides
                .Select(x => new ProductVariantDiscountOverrideDto
                {
                    Id = x.Id,
                    ProductVariantId = x.ProductVariantId,
                    OverrideNearExpiryDiscountPercent = x.OverrideNearExpiryDiscountPercent,
                    Reason = x.Reason,
                    IsActive = x.IsActive,
                    StartAtUtc = x.StartAtUtc,
                    EndAtUtc = x.EndAtUtc,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();
        }

        public async Task UpdateProductVariantDiscountOverridesAsync(string userId, List<UpsertProductVariantDiscountOverrideDto> overrides)
        {
            if (overrides == null)
                throw new InvalidBusinessRuleException("Overrides không hợp lệ");

            var normalized = overrides.Select(o => new UpsertProductVariantDiscountOverrideDto
            {
                ProductVariantId = o.ProductVariantId,
                OverrideNearExpiryDiscountPercent = o.OverrideNearExpiryDiscountPercent,
                Reason = o.Reason?.Trim(),
                IsActive = o.IsActive,
                StartAtUtc = o.StartAtUtc,
                EndAtUtc = o.EndAtUtc
            }).ToList();

            foreach (var o in normalized)
            {
                if (o.ProductVariantId <= 0)
                    throw new InvalidBusinessRuleException("ProductVariantId không hợp lệ");
                if (o.OverrideNearExpiryDiscountPercent < 0 || o.OverrideNearExpiryDiscountPercent > 100)
                    throw new InvalidBusinessRuleException("OverrideNearExpiryDiscountPercent phải trong khoảng 0-100");
                if (o.StartAtUtc.HasValue && o.EndAtUtc.HasValue && o.StartAtUtc > o.EndAtUtc)
                    throw new InvalidBusinessRuleException("Khoảng thời gian hiệu lực override không hợp lệ");
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .OrderBy(x => x.ProductVariantId)
                .ThenByDescending(x => x.StartAtUtc ?? DateTime.MinValue)
                .Select(o => new Domain.Entities.ProductVariantDiscountOverride
                {
                    ProductVariantId = o.ProductVariantId,
                    OverrideNearExpiryDiscountPercent = o.OverrideNearExpiryDiscountPercent,
                    Reason = o.Reason,
                    IsActive = o.IsActive,
                    StartAtUtc = o.StartAtUtc,
                    EndAtUtc = o.EndAtUtc,
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _variantOverrideRepo.ReplaceAllAsync(entities);
        }

        private static decimal GetSuggestedDiscountPercent(int daysLeft, List<Domain.Entities.NearExpiryDiscountRule> rules)
        {
            if (rules == null || rules.Count == 0)
                return 0m;

            if (daysLeft < 0)
                return 0m;

            var matchedRule = rules
                .Where(r => !r.MinDaysLeft.HasValue || daysLeft >= r.MinDaysLeft.Value)
                .Where(r => daysLeft <= r.MaxDaysLeft)
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.DiscountPercent)
                .FirstOrDefault();

            if (matchedRule != null)
            {
                return matchedRule.DiscountPercent;
            }

            return 0m;
        }
    }
}
