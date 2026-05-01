using AgriIDMS.Application.DTOs.Discount;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class NearExpiryDiscountService : INearExpiryDiscountService
    {
        private readonly IBoxRepository _boxRepo;
        private readonly INearExpiryDiscountRuleRepository _ruleRepo;
        private readonly IProductVariantDiscountOverrideRepository _overrideRepo;

        public NearExpiryDiscountService(
            IBoxRepository boxRepo,
            INearExpiryDiscountRuleRepository ruleRepo,
            IProductVariantDiscountOverrideRepository overrideRepo)
        {
            _boxRepo = boxRepo;
            _ruleRepo = ruleRepo;
            _overrideRepo = overrideRepo;
        }

        public async Task<NearExpiryDiscountResultDto> CalculateForVariantAsync(
            int productVariantId,
            decimal baseUnitPrice,
            bool includeOfflineOnly = false)
        {
            if (baseUnitPrice <= 0)
            {
                return new NearExpiryDiscountResultDto
                {
                    AppliedPercent = 0,
                    FinalUnitPrice = baseUnitPrice,
                    DiscountAmount = 0,
                    SourceType = NearExpiryDiscountSourceType.None
                };
            }

            var now = DateTime.UtcNow;
            var hasAnyOverrideConfig = (await _overrideRepo.GetAllAsync()).Count > 0;
            // Nghiệp vụ mới: danh sách override trống => tắt toàn bộ giảm giá.
            if (!hasAnyOverrideConfig)
            {
                return new NearExpiryDiscountResultDto
                {
                    AppliedPercent = 0,
                    FinalUnitPrice = baseUnitPrice,
                    DiscountAmount = 0,
                    SourceType = NearExpiryDiscountSourceType.None
                };
            }

            var activeOverride = await _overrideRepo.GetActiveOverrideForVariantAsync(productVariantId, now);
            var availableBoxes = await _boxRepo.GetAvailableBoxesForVariantAsync(productVariantId, includeOfflineOnly);

            // Override theo lot: áp trực tiếp cho lot được chỉ định, không phụ thuộc điều kiện near-expiry.
            var overrideLotId = TryParseEmbeddedLotId(activeOverride?.Reason);
            if (activeOverride != null
                && activeOverride.OverrideNearExpiryDiscountPercent > 0
                && overrideLotId.HasValue
                && availableBoxes.Any(b => b.LotId == overrideLotId.Value))
            {
                return BuildResult(
                    baseUnitPrice,
                    activeOverride.OverrideNearExpiryDiscountPercent,
                    null,
                    activeOverride.Id,
                    NearExpiryDiscountSourceType.ProductOverride);
            }

            var nearestExpiry = availableBoxes
                .Select(b => b.Lot?.ExpiryDate)
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .DefaultIfEmpty(DateTime.MaxValue.Date)
                .Min();

            var daysLeft = nearestExpiry == DateTime.MaxValue.Date
                ? int.MaxValue
                : (nearestExpiry - now.Date).Days;

            if (daysLeft < 0 || daysLeft == int.MaxValue)
            {
                return new NearExpiryDiscountResultDto
                {
                    AppliedPercent = 0,
                    FinalUnitPrice = baseUnitPrice,
                    DiscountAmount = 0,
                    SourceType = NearExpiryDiscountSourceType.None
                };
            }

            if (activeOverride != null && activeOverride.OverrideNearExpiryDiscountPercent > 0)
            {
                return BuildResult(
                    baseUnitPrice,
                    activeOverride.OverrideNearExpiryDiscountPercent,
                    null,
                    activeOverride.Id,
                    NearExpiryDiscountSourceType.ProductOverride);
            }

            var activeRules = await _ruleRepo.GetActiveRulesAsync(now);
            var matchedRule = FindMatchedRule(daysLeft, activeRules);
            if (matchedRule == null || matchedRule.DiscountPercent <= 0)
            {
                return new NearExpiryDiscountResultDto
                {
                    AppliedPercent = 0,
                    FinalUnitPrice = baseUnitPrice,
                    DiscountAmount = 0,
                    SourceType = NearExpiryDiscountSourceType.None
                };
            }

            return BuildResult(
                baseUnitPrice,
                matchedRule.DiscountPercent,
                matchedRule.Id,
                null,
                NearExpiryDiscountSourceType.SystemRule);
        }

        private static NearExpiryDiscountRule? FindMatchedRule(int daysLeft, IReadOnlyList<NearExpiryDiscountRule> rules)
        {
            return rules
                .Where(r => r.IsActive)
                .Where(r => !r.MinDaysLeft.HasValue || daysLeft >= r.MinDaysLeft.Value)
                .Where(r => daysLeft <= r.MaxDaysLeft)
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.DiscountPercent)
                .ThenBy(r => r.Id)
                .FirstOrDefault();
        }

        private static NearExpiryDiscountResultDto BuildResult(
            decimal baseUnitPrice,
            decimal appliedPercent,
            int? ruleId,
            int? overrideId,
            NearExpiryDiscountSourceType sourceType)
        {
            var clampedPercent = Math.Max(0, Math.Min(100, appliedPercent));
            var finalUnitPrice = Math.Round(
                Math.Max(baseUnitPrice * (1 - (clampedPercent / 100m)), 0.01m),
                2,
                MidpointRounding.AwayFromZero);
            var discountAmount = Math.Round(
                Math.Max(baseUnitPrice - finalUnitPrice, 0m),
                2,
                MidpointRounding.AwayFromZero);

            return new NearExpiryDiscountResultDto
            {
                AppliedPercent = clampedPercent,
                RuleId = ruleId,
                OverrideId = overrideId,
                SourceType = sourceType,
                DiscountAmount = discountAmount,
                FinalUnitPrice = finalUnitPrice
            };
        }

        private static int? TryParseEmbeddedLotId(string? rawReason)
        {
            if (string.IsNullOrWhiteSpace(rawReason))
                return null;

            var text = rawReason.Trim();
            if (!text.StartsWith("[LOT:", StringComparison.OrdinalIgnoreCase))
                return null;

            var closeBracket = text.IndexOf(']');
            if (closeBracket <= 5)
                return null;

            var numberPart = text.Substring(5, closeBracket - 5);
            return int.TryParse(numberPart, out var lotId) && lotId > 0 ? lotId : null;
        }
    }
}
