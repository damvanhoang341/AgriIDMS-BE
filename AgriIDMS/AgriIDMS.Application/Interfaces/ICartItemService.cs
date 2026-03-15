using AgriIDMS.Application.DTOs.Cart;
using AgriIDMS.Domain.Entities;
using System.Collections.Generic;

namespace AgriIDMS.Application.Interfaces
{
    /// <summary>Service xử lý hiển thị sản phẩm trong giỏ (cart items) cho khách hàng.</summary>
    public interface ICartItemService
    {
        /// <summary>Map giỏ hàng sang danh sách DTO hiển thị sản phẩm mua của khách.</summary>
        IReadOnlyList<CartItemDto> GetCartItemDtos(Cart? cart);
    }
}
