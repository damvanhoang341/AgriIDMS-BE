using AgriIDMS.Application.DTOs.Lot;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
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

        public LotService(ILotRepository lotRepository, IUnitOfWork unitOfWork, INearExpiryDiscountRuleRepository nearExpiryRuleRepo)
        {
            _lotRepository = lotRepository;
            _unitOfWork = unitOfWork;
            _nearExpiryRuleRepo = nearExpiryRuleRepo;
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

        public async Task<List<Lot>> GetLotsByGoodsReceiptIdAsync(int goodsReceiptId)
        {
            var lots = await _lotRepository.GetByGoodsReceiptIdAsync(goodsReceiptId);

            if (lots == null || !lots.Any())
                return new List<Lot>();

            return lots;
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
            var rules = await _nearExpiryRuleRepo.GetActiveRulesAsync();
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
                .OrderBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .Select(r => new NearExpiryDiscountRuleDto
                {
                    Id = r.Id,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    IsActive = r.IsActive,
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
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    IsActive = r.IsActive
                })
                .ToList();

            foreach (var r in normalized)
            {
                if (r.MaxDaysLeft <= 0)
                    throw new InvalidBusinessRuleException("MaxDaysLeft phải lớn hơn 0");
                if (r.DiscountPercent < 0 || r.DiscountPercent > 100)
                    throw new InvalidBusinessRuleException("DiscountPercent phải trong khoảng 0-100");
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .OrderBy(r => r.MaxDaysLeft)
                .Select(r => new NearExpiryDiscountRule
                {
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    IsActive = r.IsActive,
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _nearExpiryRuleRepo.ReplaceAllRulesAsync(entities);
        }

        private static decimal GetSuggestedDiscountPercent(int daysLeft, List<NearExpiryDiscountRule> rules)
        {
            if (rules == null || rules.Count == 0)
                return 0m;

            if (daysLeft < 0)
                return 0m;

            foreach (var rule in rules.OrderBy(r => r.MaxDaysLeft))
            {
                if (!rule.IsActive) continue;
                if (daysLeft <= rule.MaxDaysLeft)
                    return rule.DiscountPercent;
            }

            return 0m;
        }
    }
}
