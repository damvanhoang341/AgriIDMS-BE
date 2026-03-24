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
    [Authorize]
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

        /// <summary>Danh sách đơn đang chờ sale xác nhận (PendingSaleConfirmation) cho Sale/Admin/Manager.</summary>
        [HttpGet("staff/pending-sale-confirm")]
        [Authorize(Roles = "SalesStaff,Admin,Manager")]
        public async Task<IActionResult> GetPendingSaleConfirmOrders([FromQuery] GetPendingSaleConfirmOrdersQuery query)
        {
            var result = await _orderService.GetPendingSaleConfirmOrdersAsync(query);
            return Ok(result);
        }

        /// <summary>Danh sách đơn đang chờ giữ hàng (AwaitingAllocation) cho Sale/Kho/Admin/Manager.</summary>
        [HttpGet("staff/pending-allocation")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetPendingAllocationOrders([FromQuery] GetPendingAllocationOrdersQuery query)
        {
            var result = await _orderService.GetPendingAllocationOrdersAsync(query);
            return Ok(result);
        }

        /// <summary>Danh sách đơn đã được system propose FEFO, chờ kho xác nhận.</summary>
        [HttpGet("staff/pending-warehouse-confirm")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetPendingWarehouseConfirmOrders([FromQuery] GetPendingAllocationOrdersQuery query)
        {
            var result = await _orderService.GetPendingWarehouseConfirmOrdersAsync(query);
            return Ok(result);
        }

        /// <summary>Danh sách đơn thiếu hàng đang chờ khách quyết định (wait/cancel-shortage).</summary>
        [HttpGet("staff/pending-customer-decision")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetPendingCustomerDecisionOrders([FromQuery] GetPendingAllocationOrdersQuery query)
        {
            var result = await _orderService.GetPendingCustomerDecisionOrdersAsync(query);
            return Ok(result);
        }

        /// <summary>Danh sách đơn khách đã chọn chờ backorder (BackorderWaiting).</summary>
        [HttpGet("staff/backorder-waiting")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetBackorderWaitingOrders([FromQuery] GetPendingAllocationOrdersQuery query)
        {
            var result = await _orderService.GetBackorderWaitingOrdersAsync(query);
            return Ok(result);
        }

        /// <summary>Chi tiết đơn đang chờ backorder để staff xử lý allocate/timeout action.</summary>
        [HttpGet("staff/backorder-waiting/{id:int:min(1)}")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetBackorderWaitingOrderDetail(int id)
        {
            var result = await _orderService.GetBackorderWaitingOrderDetailAsync(id);
            return Ok(result);
        }

        /// <summary>Chi tiết box đang được propose FEFO cho 1 đơn.</summary>
        [HttpGet("{id:int:min(1)}/allocation/proposals")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetAllocationProposals(int id)
        {
            var result = await _orderService.GetAllocationProposalsAsync(id);
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

        /// <summary>Danh sách đơn backorder đã quá hạn cho sale/staff xử lý.</summary>
        [HttpGet("backorder/overdue")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetOverdueBackorders()
        {
            var result = await _orderService.GetOverdueBackordersAsync();
            return Ok(result);
        }

        [HttpPost("from-cart")]
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateFromCartByVariants([FromBody] CreateOrderFromCartRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.CreateOrderFromCartByVariantIdsAsync(userId, request.Items);
            return Ok(result);
        }

        /// <summary>Tạo đơn bán trực tiếp (POS) và đi thẳng trạng thái AwaitingAllocation.</summary>
        [HttpPost("pos")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> CreatePosOrder([FromBody] CreatePosOrderRequest request)
        {
            var operatorUserId = GetCurrentUserId();
            var result = await _orderService.CreatePosOrderAsync(operatorUserId, request);
            return Ok(result);
        }

        /// <summary>Sale xác nhận đơn (sau bước này đơn mới được phép giữ hàng / allocate).</summary>
        [HttpPatch("{id:int:min(1)}/sale-confirm")]
        [Authorize(Roles = "SalesStaff,Admin,Manager")]
        public async Task<IActionResult> SaleConfirm(int id)
        {
            var staffUserId = GetCurrentUserId();
            var result = await _orderService.SaleConfirmOrderAsync(id, staffUserId);
            return Ok(result);
        }

        /// <summary>Giữ hàng: chỉ chủ đơn (khách), sau khi sale đã xác nhận.</summary>
        [HttpPatch("{id:int:min(1)}/ConfirmOrder")]
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
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

        /// <summary>System/staff chạy auto allocate FEFO, tạo đề xuất chờ kho xác nhận.</summary>
        [HttpPatch("{id:int:min(1)}/allocation/auto-propose")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager,SalesStaff")]
        public async Task<IActionResult> AutoProposeAllocationAsStaff(int id)
        {
            var operatorUserId = GetCurrentUserId();
            var result = await _orderService.AutoProposeAllocationAsync(id, operatorUserId, skipCustomerOwnershipCheck: true);
            return Ok(result);
        }

        /// <summary>Kho làm mới proposal FEFO cho đơn (hủy proposal cũ và đề xuất lại).</summary>
        [HttpPatch("{id:int:min(1)}/allocation/re-propose")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> ReProposeAllocationAsStaff(int id)
        {
            var operatorUserId = GetCurrentUserId();
            var result = await _orderService.ReProposeAllocationAsync(id, operatorUserId, skipCustomerOwnershipCheck: true);
            return Ok(result);
        }

        /// <summary>Kho từ chối proposal hiện tại và đưa đơn về AwaitingAllocation.</summary>
        [HttpPatch("{id:int:min(1)}/allocation/reject")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> RejectAllocationProposalAsStaff(int id)
        {
            var operatorUserId = GetCurrentUserId();
            var result = await _orderService.RejectAllocationProposalAsync(id, operatorUserId, skipCustomerOwnershipCheck: true);
            return Ok(result);
        }

        /// <summary>Kho xác nhận đề xuất allocate và commit reserve box.</summary>
        [HttpPatch("{id:int:min(1)}/allocation/confirm")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> ConfirmAllocationAsStaff(int id)
        {
            var operatorUserId = GetCurrentUserId();
            var result = await _orderService.ConfirmAllocationAsync(id, operatorUserId, skipCustomerOwnershipCheck: true);
            return Ok(result);
        }

        /// <summary>Khách chọn chờ backorder cho phần còn thiếu.</summary>
        [HttpPatch("{id:int:min(1)}/backorder/wait")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> WaitBackorder(int id)
        {
            var userId = GetCurrentUserId();
            await _orderService.WaitBackorderAsync(id, userId);
            return Ok(new
            {
                Message = "Đã chuyển trạng thái chờ backorder",
                OrderId = id
            });
        }

        /// <summary>Khách chọn hủy phần còn thiếu để chỉ ship phần đã allocate/giữ được.</summary>
        [HttpPatch("{id:int:min(1)}/backorder/cancel-shortage")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelShortage(int id)
        {
            var userId = GetCurrentUserId();
            await _orderService.CancelShortageAsync(id, userId);
            return Ok(new
            {
                Message = "Đã hủy phần thiếu: chỉ ship phần còn lại",
                OrderId = id
            });
        }

        /// <summary>
        /// Staff allocate nốt phần thiếu cho backorder.
        /// Nếu quá thời gian chờ thì xử lý theo <see cref="BackorderAllocateRequestDto.ExpiredAction"/>.
        /// </summary>
        [HttpPatch("{id:int:min(1)}/backorder/allocate")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager,SalesStaff")]
        public async Task<IActionResult> AllocateBackorderAsStaff(int id, [FromBody] BackorderAllocateRequestDto request)
        {
            var operatorUserId = GetCurrentUserId();
            await _orderService.BackorderAllocateAsync(id, operatorUserId, request?.ExpiredAction ?? BackorderExpiredAction.CancelShortage);
            return Ok(new
            {
                Message = "Đã xử lý backorder theo thời gian chờ",
                OrderId = id
            });
        }

        [HttpPatch("{id:int:min(1)}/cancel")]
        [Authorize(Roles = "Customer")]
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
