using AgriIDMS.Application.DTOs.Home;
using AgriIDMS.Application.DTOs.ProductVariant;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    /// <summary>Service dữ liệu trang chủ: hiển thị sản phẩm theo luồng Category → Product → ProductVariant.</summary>
    public class HomePageService : IHomePageService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly INearExpiryDiscountRuleRepository _nearExpiryRuleRepo;
        private readonly IProductVariantDiscountOverrideRepository _variantOverrideRepo;
        private readonly ILogger<HomePageService> _logger;
        private readonly IProductVariantRepository _repo;

        public HomePageService(
            ICategoryRepository categoryRepo,
            IBoxRepository boxRepo,
            INearExpiryDiscountRuleRepository nearExpiryRuleRepo,
            IProductVariantDiscountOverrideRepository variantOverrideRepo,
            ILogger<HomePageService> logger,
            IProductVariantRepository repo)
        {
            _categoryRepo = categoryRepo;
            _boxRepo = boxRepo;
            _nearExpiryRuleRepo = nearExpiryRuleRepo;
            _variantOverrideRepo = variantOverrideRepo;
            _logger = logger;
            _repo = repo;
        }

        public async Task<IEnumerable<ProductVariantResponseCustomerHomeDto>> GetAllProductVariantAsync()
        {
            _logger.LogInformation("Getting all product variants");

            var variants = await _repo.GetAllAsync();
            var now = DateTime.UtcNow;
            var activeRules = await _nearExpiryRuleRepo.GetActiveRulesAsync(now);
            var activeOverrides = await _variantOverrideRepo.GetActiveOverridesByVariantIdsAsync(
                variants.Select(v => v.Id),
                now);
            var result = new List<ProductVariantResponseCustomerHomeDto>();
            foreach (var x in variants)
            {
                var pricing = await BuildNearExpiryPricingAsync(
                    x.Id,
                    x.Price,
                    activeRules,
                    activeOverrides.TryGetValue(x.Id, out var ov) ? ov : null);
                result.Add(new ProductVariantResponseCustomerHomeDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = $"{x.Product.Name}",
                    Grade = x.Grade,
                    Price = x.Price,
                    ImageUrl = x.ImageUrl,
                    HasNearExpiryStock = pricing.HasNearExpiryStock,
                    NearExpiryDiscountPercent = pricing.NearExpiryDiscountPercent,
                    NearExpiryPricePerKg = pricing.NearExpiryPricePerKg,
                    NearExpiryPriceTiers = pricing.Tiers,
                });
            }
            return result;
        }

        public async Task<ProductVariantResponseCustomerDto> GetDetailAsync(int idProductVariant)
        {
            _logger.LogInformation("Getting detail product variants");

            var variant = await _repo.GetProductVariantByIdAsync(idProductVariant);

            if (variant == null)
                throw new Exception("Product variant not found");

            var now = DateTime.UtcNow;
            var activeRules = await _nearExpiryRuleRepo.GetActiveRulesAsync(now);
            var activeOverride = await _variantOverrideRepo.GetActiveOverrideForVariantAsync(variant.Id, now);
            var pricing = await BuildNearExpiryPricingAsync(
                variant.Id,
                variant.Price,
                activeRules,
                activeOverride);

            var boxTypeSummaries = await _boxRepo.GetAvailableBoxTypeSummaryByVariantIdAsync(variant.Id);

            var boxTypes = boxTypeSummaries
                .Select(bt => new BoxTypeDto
                {
                    BoxType = bt.IsPartial ? "Partial" : "Full",
                    Weight = bt.Weight,
                    AvailableCount = bt.AvailableCount,
                    BoxPrice = variant.Price * bt.Weight
                })
                .OrderBy(bt => bt.Weight)
                .ToList();

            var boxCount = boxTypes.Sum(bt => bt.AvailableCount);

            var result = new ProductVariantResponseCustomerDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ProductName = $"{variant.Product.Name} {variant.Grade}",
                Grade = variant.Grade,
                Price = variant.Price,
                IsActive = variant.IsActive,
                ShelfLifeDays = variant.ShelfLifeDays,
                ImageUrl = variant.ImageUrl,
                AvailableBoxCount = boxCount,
                BoxTypes = boxTypes,
                HasNearExpiryStock = pricing.HasNearExpiryStock,
                NearExpiryDiscountPercent = pricing.NearExpiryDiscountPercent,
                NearExpiryPricePerKg = pricing.NearExpiryPricePerKg,
                NearExpiryPriceTiers = pricing.Tiers
            };

            return result;
        }

        private async Task<(bool HasNearExpiryStock, decimal? NearExpiryDiscountPercent, decimal? NearExpiryPricePerKg, List<NearExpiryPriceTierDto> Tiers)> BuildNearExpiryPricingAsync(
            int productVariantId,
            decimal basePricePerKg,
            List<NearExpiryDiscountRule> activeRules,
            ProductVariantDiscountOverride? activeOverride)
        {
            if (basePricePerKg <= 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            var orderedRules = (activeRules ?? new List<NearExpiryDiscountRule>())
                .Where(r => r.IsActive && r.MaxDaysLeft > 0)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .ToList();

            // Không có rule hệ thống: vẫn hiển thị giá ưu đãi nếu Manager bật ghi đè variant (khớp hướng áp dụng của NearExpiryDiscountService khi checkout).
            if (orderedRules.Count == 0)
                return await BuildPricingWhenNoSystemRulesAsync(productVariantId, basePricePerKg, activeOverride);

            var maxRuleDays = orderedRules.Max(r => r.MaxDaysLeft);
            var today = DateTime.UtcNow.Date;
            var boxes = await _boxRepo.GetAvailableBoxesForVariantAsync(productVariantId, includeOfflineOnly: false);
            if (boxes == null || boxes.Count == 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            var nearExpiryDaysLeft = boxes
                .Select(b => b.Lot?.ExpiryDate.Date)
                .Where(d => d.HasValue)
                .Select(d => (d!.Value - today).Days)
                .Where(daysLeft => daysLeft >= 0 && daysLeft <= maxRuleDays)
                .ToList();

            if (nearExpiryDaysLeft.Count == 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            List<NearExpiryPriceTierDto> tiers;
            if (activeOverride != null && activeOverride.OverrideNearExpiryDiscountPercent > 0)
            {
                var percent = activeOverride.OverrideNearExpiryDiscountPercent;
                var pricePerKg = Math.Round(
                    Math.Max(basePricePerKg * (1 - (percent / 100m)), 0.01m),
                    2,
                    MidpointRounding.AwayFromZero);
                tiers = new List<NearExpiryPriceTierDto>
                {
                    new NearExpiryPriceTierDto
                    {
                        MaxDaysLeft = maxRuleDays,
                        DiscountPercent = percent,
                        PricePerKg = pricePerKg,
                        BoxCount = nearExpiryDaysLeft.Count
                    }
                };
            }
            else
            {
                tiers = orderedRules
                    .Select(rule =>
                    {
                        var min = rule.MinDaysLeft ?? 0;
                        var boxCount = nearExpiryDaysLeft.Count(daysLeft =>
                            daysLeft >= min &&
                            daysLeft <= rule.MaxDaysLeft);
                        if (boxCount <= 0) return null;

                        var effectivePercent = rule.DiscountPercent;
                        var pricePerKg = Math.Round(
                            Math.Max(basePricePerKg * (1 - (effectivePercent / 100m)), 0.01m),
                            2,
                            MidpointRounding.AwayFromZero);
                        return new NearExpiryPriceTierDto
                        {
                            MaxDaysLeft = rule.MaxDaysLeft,
                            DiscountPercent = effectivePercent,
                            PricePerKg = pricePerKg,
                            BoxCount = boxCount
                        };
                    })
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToList();
            }

            if (tiers.Count == 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            var nearestDaysLeft = nearExpiryDaysLeft.Min();
            var suggestedByRule = activeOverride != null && activeOverride.OverrideNearExpiryDiscountPercent > 0
                ? activeOverride.OverrideNearExpiryDiscountPercent
                : ResolveDiscountPercentByRule(nearestDaysLeft, orderedRules);
            var effectiveDiscount = suggestedByRule;

            if (effectiveDiscount <= 0)
                return (true, 0m, basePricePerKg, tiers);

            var discounted = Math.Round(
                Math.Max(basePricePerKg * (1 - (effectiveDiscount / 100m)), 0.01m),
                2,
                MidpointRounding.AwayFromZero);
            return (true, effectiveDiscount, discounted, tiers);
        }

        private static decimal ResolveDiscountPercentByRule(int daysLeft, List<NearExpiryDiscountRule> rules)
        {
            return rules
                .Where(r => !r.MinDaysLeft.HasValue || daysLeft >= r.MinDaysLeft.Value)
                .Where(r => daysLeft <= r.MaxDaysLeft)
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.DiscountPercent)
                .Select(r => r.DiscountPercent)
                .FirstOrDefault();
        }

        /// <summary>
        /// Khi chưa cấu hình NearExpiryDiscountRule nhưng có ProductVariantDiscountOverride đang hiệu lực — trả dữ liệu hiển thị shop (không đổi logic tính tiền ở OrderService).
        /// </summary>
        private async Task<(bool HasNearExpiryStock, decimal? NearExpiryDiscountPercent, decimal? NearExpiryPricePerKg, List<NearExpiryPriceTierDto> Tiers)> BuildPricingWhenNoSystemRulesAsync(
            int productVariantId,
            decimal basePricePerKg,
            ProductVariantDiscountOverride? activeOverride)
        {
            if (basePricePerKg <= 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            if (activeOverride == null || activeOverride.OverrideNearExpiryDiscountPercent <= 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            var today = DateTime.UtcNow.Date;
            var boxes = await _boxRepo.GetAvailableBoxesForVariantAsync(productVariantId, includeOfflineOnly: false);
            if (boxes == null || boxes.Count == 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            var nearExpiryDaysLeft = boxes
                .Select(b => b.Lot?.ExpiryDate.Date)
                .Where(d => d.HasValue)
                .Select(d => (d!.Value - today).Days)
                .Where(daysLeft => daysLeft >= 0)
                .ToList();

            if (nearExpiryDaysLeft.Count == 0)
                return (false, null, null, new List<NearExpiryPriceTierDto>());

            var percent = activeOverride.OverrideNearExpiryDiscountPercent;
            var pricePerKg = Math.Round(
                Math.Max(basePricePerKg * (1 - (percent / 100m)), 0.01m),
                2,
                MidpointRounding.AwayFromZero);

            var tiers = new List<NearExpiryPriceTierDto>
            {
                new NearExpiryPriceTierDto
                {
                    MaxDaysLeft = nearExpiryDaysLeft.Max(),
                    DiscountPercent = percent,
                    PricePerKg = pricePerKg,
                    BoxCount = nearExpiryDaysLeft.Count
                }
            };

            if (percent <= 0)
                return (true, 0m, basePricePerKg, tiers);

            var discounted = Math.Round(
                Math.Max(basePricePerKg * (1 - (percent / 100m)), 0.01m),
                2,
                MidpointRounding.AwayFromZero);

            return (true, percent, discounted, tiers);
        }
    }
}
