using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("from-cart")]
        public async Task<IActionResult> CreateFromCart()
        {
            var userId = GetCurrentUserId();
            var orderId = await _orderService.CreateOrderFromCartAsync(userId);
            return Ok(new
            {
                Message = "Tạo đơn hàng từ giỏ thành công",
                OrderId = orderId
            });
        }

        [HttpPost("{id:int:min(1)}/allocate")]
        public async Task<IActionResult> Allocate(int id)
        {
            var userId = GetCurrentUserId();
            await _orderService.AllocateInventoryAsync(id, userId);
            return Ok(new
            {
                Message = "Đã kiểm tra và giữ hàng cho đơn hàng",
                OrderId = id
            });
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");
        }
    }
}
