using AgriIDMS.Application.DTOs.Cart;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace AgriIDMS.Application.Services
{
    /// <summary>Service hiển thị sản phẩm mua của khách hàng trong giỏ (cart items).</summary>
    public class CartItemService : ICartItemService
    {
        private const decimal PriceComparisonTolerance = 0.0001m;

        public IReadOnlyList<CartItemDto> GetCartItemDtos(Cart? cart)
        {
            if (cart?.Items == null || !cart.Items.Any())
                return new List<CartItemDto>();

            return cart.Items.Select(i => new CartItemDto
            {
                ProductVariantId = i.ProductVariantId,
                ProductVariantName = i.ProductVariant?.Name ?? string.Empty,
                ProductName = i.ProductVariant?.Product?.Name ?? string.Empty,
                Grade = i.ProductVariant?.Grade.ToString() ?? string.Empty,
                ImageUrl = i.ProductVariant?.ImageUrl,
                Quantity = (int)i.Quantity,
                BoxWeight = i.BoxWeight,
                IsPartial = i.IsPartial,
                UnitPrice = NormalizeUnitPricePerKg(i.UnitPrice, i.BoxWeight, i.ProductVariant?.Price)
            }).ToList();
        }

        private static decimal NormalizeUnitPricePerKg(decimal storedUnitPrice, decimal boxWeight, decimal? variantPricePerKg)
        {
            // Backward compatibility: old data may store UnitPrice as "price per box" (= price/kg * boxWeight).
            // Normalize to "price per kg" so LineAmount formula stays consistent.
            if (variantPricePerKg.HasValue && boxWeight > 0)
            {
                var expectedPricePerBox = variantPricePerKg.Value * boxWeight;
                if (Math.Abs(storedUnitPrice - expectedPricePerBox) <= PriceComparisonTolerance)
                    return variantPricePerKg.Value;
            }

            return storedUnitPrice;
        }
    }
}
