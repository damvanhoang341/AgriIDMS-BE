using AgriIDMS.Application.DTOs.Lot;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
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
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
                .OrderBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .Select(r =>
                {
                    var payload = ParseNearExpiryPayload(r.ConditionsJson);
                    return new NearExpiryDiscountRuleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        MinDaysLeft = payload?.MinDaysLeft,
                        MaxDaysLeft = r.MaxDaysLeft ?? 0,
                        DiscountPercent = r.DiscountPercent,
                        Priority = r.Priority > 0 ? r.Priority : 1,
                        IsActive = r.IsActive,
                        StartAtUtc = payload?.StartAtUtc,
                        EndAtUtc = payload?.EndAtUtc,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    };
                })
                .ToList();
        }

        public async Task UpdateNearExpiryDiscountRulesAsync(string userId, List<UpsertNearExpiryDiscountRuleDto> rules)
        {
            if (rules == null)
                throw new InvalidBusinessRuleException("Danh sách quy tắc giảm giá không hợp lệ.");

            var normalized = rules
                .Select(r => new UpsertNearExpiryDiscountRuleDto
                {
                    Name = string.IsNullOrWhiteSpace(r.Name) ? null : r.Name.Trim(),
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
                var hasStart = r.StartAtUtc.HasValue;
                var hasEnd = r.EndAtUtc.HasValue;
                if (hasStart != hasEnd)
                    throw new InvalidBusinessRuleException("Thời gian hiệu lực không hợp lệ: phải nhập đủ cả thời gian bắt đầu và kết thúc.");
                if (hasStart && hasEnd && r.StartAtUtc > r.EndAtUtc)
                    throw new InvalidBusinessRuleException("Thời gian hiệu lực không hợp lệ: thời gian bắt đầu phải nhỏ hơn hoặc bằng thời gian kết thúc.");
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .Select(r => new DiscountRule
                {
                    RuleType = DiscountRuleType.NearExpiry,
                    Name = string.IsNullOrWhiteSpace(r.Name)
                        ? $"Near-expiry <= {r.MaxDaysLeft} day(s)"
                        : r.Name,
                    MaxDaysLeft = r.MaxDaysLeft,
                    DiscountPercent = r.DiscountPercent,
                    Priority = r.Priority,
                    IsActive = r.IsActive,
                    ConditionsJson = JsonSerializer.Serialize(new NearExpiryRulePayload
                    {
                        MinDaysLeft = r.MinDaysLeft,
                        StartAtUtc = r.StartAtUtc,
                        EndAtUtc = r.EndAtUtc
                    }, JsonOptions),
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _nearExpiryRuleRepo.ReplaceAllRulesAsync(entities);
        }

        public async Task<List<ProductVariantDiscountOverrideDto>> GetProductVariantDiscountOverridesAsync()
        {
            var rules = await _discountRuleRepo.GetAllRulesAsync(DiscountRuleType.Unknown);
            var result = new List<ProductVariantDiscountOverrideDto>();
            foreach (var rule in rules)
            {
                var payload = ParseProductVariantOverridePayload(rule.ConditionsJson);
                if (payload == null || payload.ProductVariantId <= 0)
                    continue;

                result.Add(new ProductVariantDiscountOverrideDto
                {
                    Id = rule.Id,
                    ProductVariantId = payload.ProductVariantId,
                    LotId = payload.LotId,
                    OverrideNearExpiryDiscountPercent = rule.DiscountPercent,
                    Reason = payload.Reason,
                    IsActive = rule.IsActive,
                    StartAtUtc = payload.StartAtUtc,
                    EndAtUtc = payload.EndAtUtc,
                    CreatedAt = rule.CreatedAt,
                    UpdatedAt = rule.UpdatedAt
                });
            }

            return result
                .OrderBy(x => x.ProductVariantId)
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
                OverrideNearExpiryDiscountPercent = x.OverrideNearExpiryDiscountPercent,
                Reason = string.IsNullOrWhiteSpace(x.Reason) ? null : x.Reason.Trim(),
                IsActive = x.IsActive,
                StartAtUtc = x.StartAtUtc,
                EndAtUtc = x.EndAtUtc
            }).ToList();

            foreach (var item in normalized)
            {
                if (item.ProductVariantId <= 0)
                    throw new InvalidBusinessRuleException("Mã biến thể sản phẩm phải lớn hơn 0.");
                if (item.OverrideNearExpiryDiscountPercent < 0 || item.OverrideNearExpiryDiscountPercent > 100)
                    throw new InvalidBusinessRuleException("Mức giảm giá ghi đè phải trong khoảng 0-100.");
                if (item.StartAtUtc.HasValue && item.EndAtUtc.HasValue && item.StartAtUtc > item.EndAtUtc)
                    throw new InvalidBusinessRuleException("Thời gian hiệu lực không hợp lệ: thời gian bắt đầu phải nhỏ hơn hoặc bằng thời gian kết thúc.");

                if (item.LotId.HasValue && item.LotId.Value > 0)
                {
                    var lot = await _lotRepository.GetByIdWithDetailAndReceiptAsync(item.LotId.Value)
                        ?? throw new NotFoundException($"Lot #{item.LotId.Value} không tồn tại.");
                    var lotProductVariantId = lot.GoodsReceiptDetail?.ProductVariantId ?? 0;
                    if (lotProductVariantId != item.ProductVariantId)
                        throw new InvalidBusinessRuleException(
                            $"Lot #{item.LotId.Value} không thuộc biến thể sản phẩm #{item.ProductVariantId}.");
                }
            }

            var now = DateTime.UtcNow;
            var entities = normalized
                .Select((item, index) => new DiscountRule
                {
                    RuleType = DiscountRuleType.Unknown,
                    Name = $"{ProductVariantOverrideRuleNamePrefix}-{item.ProductVariantId}",
                    MaxDaysLeft = null,
                    DiscountPercent = item.OverrideNearExpiryDiscountPercent,
                    Priority = index + 1,
                    IsActive = item.IsActive,
                    ConditionsJson = JsonSerializer.Serialize(new ProductVariantOverridePayload
                    {
                        ProductVariantId = item.ProductVariantId,
                        LotId = item.LotId,
                        Reason = item.Reason,
                        StartAtUtc = item.StartAtUtc,
                        EndAtUtc = item.EndAtUtc
                    }, JsonOptions),
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList();

            await _discountRuleRepo.ReplaceAllRulesAsync(entities, DiscountRuleType.Unknown);
        }

        public async Task<FreeStyleDiscountPreviewResponseDto> PreviewFreeStyleDiscountAsync(FreeStyleDiscountPreviewRequestDto request)
        {
            if (request == null)
                throw new InvalidBusinessRuleException("Request không hợp lệ");
            if (request.Subtotal < 0)
                throw new InvalidBusinessRuleException("Subtotal không hợp lệ");


            foreach (var rule in rules.OrderBy(r => r.Priority).ThenBy(r => r.MaxDaysLeft))
            {
                if (!rule.IsActive) continue;
                var payload = ParseNearExpiryPayload(rule.ConditionsJson);
                var nowUtc = DateTime.UtcNow;
                if (payload?.StartAtUtc.HasValue == true && nowUtc < payload.StartAtUtc.Value) continue;
                if (payload?.EndAtUtc.HasValue == true && nowUtc > payload.EndAtUtc.Value) continue;
                if (payload?.MinDaysLeft.HasValue == true && daysLeft < payload.MinDaysLeft.Value) continue;
                if (rule.MaxDaysLeft.HasValue && daysLeft <= rule.MaxDaysLeft.Value)
                    return rule.DiscountPercent;
            }

            return 0m;
        }

        private static ProductVariantOverridePayload? ParseProductVariantOverridePayload(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonSerializer.Deserialize<ProductVariantOverridePayload>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static NearExpiryRulePayload? ParseNearExpiryPayload(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonSerializer.Deserialize<NearExpiryRulePayload>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private sealed class NearExpiryRulePayload
        {
            public int? MinDaysLeft { get; set; }
            public DateTime? StartAtUtc { get; set; }
            public DateTime? EndAtUtc { get; set; }
        }

        private sealed class ProductVariantOverridePayload
        {
            public int ProductVariantId { get; set; }
            public int? LotId { get; set; }
            public string? Reason { get; set; }
            public DateTime? StartAtUtc { get; set; }
            public DateTime? EndAtUtc { get; set; }
        }

        private static FreeStyleDiscountConditionsDto ParseFreeStyleConditions(string? conditionsJson)
        {
            if (string.IsNullOrWhiteSpace(conditionsJson))
                return new FreeStyleDiscountConditionsDto();

            try
            {
                return JsonSerializer.Deserialize<FreeStyleDiscountConditionsDto>(conditionsJson, JsonOptions)
                    ?? new FreeStyleDiscountConditionsDto();
            }
            catch
            {
                return new FreeStyleDiscountConditionsDto();
            }
        }

        private static bool IsFreeStyleRuleMatched(DiscountRule rule, FreeStyleDiscountPreviewRequestDto request)
        {
            var conditions = ParseFreeStyleConditions(rule.ConditionsJson);
            if (conditions.Channels.Count > 0 &&
                !conditions.Channels.Any(c => string.Equals(c, request.Channel, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!conditions.IsGuestAllowed && request.IsGuest)
                return false;

            if (conditions.MinSubtotal.HasValue && request.Subtotal < conditions.MinSubtotal.Value)
                return false;

            if (conditions.ProductVariantIds.Count > 0)
            {
                var requestedIds = request.ProductVariantIds ?? new List<int>();
                if (!requestedIds.Any(id => conditions.ProductVariantIds.Contains(id)))
                    return false;
            }

            return true;
        }
    }
}
