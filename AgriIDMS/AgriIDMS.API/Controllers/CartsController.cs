using AgriIDMS.Application.DTOs.Cart;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartsController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.GetMyCartAsync(userId);
            return Ok(cart);
        }

        /// <summary>Lấy danh sách sản phẩm trong giỏ để hiển thị cho khách hàng.</summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetCartItems()
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.GetMyCartAsync(userId);
            return Ok(cart.Items);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            var userId = GetCurrentUserId();
            await _cartService.AddOrUpdateItemAsync(request, userId);
            return Ok(new { Message = "Thêm sản phẩm vào giỏ hàng thành công" });
        }

        [HttpPut("items/{productVariantId:int:min(1)}")]
        public async Task<IActionResult> UpdateItemQuantity(
            int productVariantId,
            [FromBody] UpdateCartItemRequest request)
        {
            var userId = GetCurrentUserId();
            await _cartService.UpdateItemQuantityAsync(productVariantId, request, userId);
            return Ok(new { Message = "Cập nhật số lượng thành công" });
        }

        [HttpDelete("items/{productVariantId:int:min(1)}")]
        public async Task<IActionResult> RemoveItem(
            int productVariantId,
            [FromQuery] decimal boxWeight,
            [FromQuery] bool isPartial)
        {
            var userId = GetCurrentUserId();
            await _cartService.RemoveItemAsync(productVariantId, boxWeight, isPartial, userId);
            return Ok(new { Message = "Xóa sản phẩm khỏi giỏ hàng thành công" });
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetCurrentUserId();
            await _cartService.ClearCartAsync(userId);
            return Ok(new { Message = "Xóa giỏ hàng thành công" });
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");
        }
    }
}
