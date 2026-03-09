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

        /// <summary>Tạo đơn hàng từ giỏ hiện tại của user.</summary>
        [HttpPost("from-cart")]
        public async Task<IActionResult> CreateFromCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderId = await _orderService.CreateOrderFromCartAsync(userId!);
            return Ok(new
            {
                Message = "Tạo đơn hàng từ giỏ thành công",
                OrderId = orderId
            });
        }

        /// <summary>Kiểm tra & giữ hàng (allocate Box) cho đơn hàng.</summary>
        [HttpPost("{id}/allocate")]
        public async Task<IActionResult> Allocate(int id)
        {
            await _orderService.AllocateInventoryAsync(id);
            return Ok(new
            {
                Message = "Đã kiểm tra và giữ hàng cho đơn hàng",
                OrderId = id
            });
        }
    }
}

