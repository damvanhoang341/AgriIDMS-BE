using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.DTOs.Order;
using AgriIDMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] GetOrdersQuery query)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetMyOrdersAsync(userId, query);
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetMyOrderById(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetMyOrderByIdAsync(id, userId);
            return Ok(result);
        }

        // Gợi ý FE call: GET /api/orders/history?statusOrder=complete
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryOrders([FromQuery] string? statusOrder)
        {
            var userId = GetCurrentUserId();

            var query = new GetOrdersQuery
            {
                Status = OrderStatus.Completed.ToString()
            };

            var result = await _orderService.GetMyOrdersAsync(userId, query);
            return Ok(result);
        }

        [HttpPost("from-cart")]
        public async Task<IActionResult> CreateFromCart()
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.CreateOrderFromCartAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo đơn theo danh sách ProductVariantId trong giỏ hàng.
        /// Chỉ xóa các CartItem thuộc các ProductVariantId được chọn.
        /// </summary>
        [HttpPost("from-cart/variants")]
        public async Task<IActionResult> CreateFromCartByVariants([FromBody] CreateOrderFromCartRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.CreateOrderFromCartByVariantIdsAsync(userId, request.Items);
            return Ok(result);
        }

        /// <summary>Sale xác nhận đơn (sau bước này đơn mới được phép giữ hàng / allocate).</summary>
        [HttpPatch("{id:int:min(1)}/sale-confirm")]
        //[Authorize(Roles = "Sale,Admin,Manager")]
        public async Task<IActionResult> SaleConfirm(int id)
        {
            var staffUserId = GetCurrentUserId();
            await _orderService.SaleConfirmOrderAsync(id, staffUserId);
            return Ok(new
            {
                Message = "Sale đã xác nhận đơn — có thể thực hiện giữ hàng (allocate)",
                OrderId = id
            });
        }

        /// <summary>Giữ hàng: chỉ chủ đơn (khách), sau khi sale đã xác nhận.</summary>
        [HttpPatch("{id:int:min(1)}/ConfirmOrder")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var userId = GetCurrentUserId();
            await _orderService.ConfirmOrderAsync(id, userId, skipCustomerOwnershipCheck: false);
            return Ok(new
            {
                Message = "Đã kiểm tra và giữ hàng cho đơn hàng",
                OrderId = id
            });
        }

        /// <summary>Giữ hàng thay mặt kho/sale (operator không phải chủ đơn). Nên bật Authorize role khi deploy.</summary>
        [HttpPatch("{id:int:min(1)}/allocate/staff")]
        //[Authorize(Roles = "WarehouseStaff,Admin,Manager,Sale")]
        public async Task<IActionResult> AllocateAsStaff(int id)
        {
            var operatorUserId = GetCurrentUserId();
            await _orderService.ConfirmOrderAsync(id, operatorUserId, skipCustomerOwnershipCheck: true);
            return Ok(new
            {
                Message = "Đã kiểm tra và giữ hàng cho đơn hàng (thao tác nhân sự)",
                OrderId = id
            });
        }

        [HttpPatch("{id:int:min(1)}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetCurrentUserId();
            await _orderService.CancelOrderAsync(id, userId);
            return Ok(new
            {
                Message = "Hủy đơn hàng thành công",
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
