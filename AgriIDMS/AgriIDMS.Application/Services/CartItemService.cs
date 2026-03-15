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
                UnitPrice = i.UnitPrice
            }).ToList();
        }
    }
}
