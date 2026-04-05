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

        /// <summary>
        /// Đơn sẵn sàng xuất kho: đã chọn PaymentTiming; PayBefore cần Paid; PayAfter cho phép chưa thanh toán hoặc Cash Pending; còn allocation Reserved.
        /// Trả về kèm phiếu xuất đang hoạt động (không Cancelled) nếu có.
        /// Query: skip, take, sort (paidAtDesc mặc định, paidAtAsc, createdAtDesc, createdAtAsc), orderId, source (Online|POS).
        /// </summary>
        [HttpGet("staff/paid-pending-export")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetPaidPendingExportOrders([FromQuery] GetPaidPendingExportOrdersQuery query)
        {
            var result = await _orderService.GetPaidPendingExportOrdersAsync(query);
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

        /// <summary>Danh sách đơn đã allocate xong (Confirmed) để staff xem lại box đã reserve.</summary>
        [HttpGet("staff/allocation-completed")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetConfirmedAllocationOrders([FromQuery] GetPendingAllocationOrdersQuery query)
        {
            var result = await _orderService.GetConfirmedAllocationOrdersAsync(query);
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

        /// <summary>Lịch sử allocation (Proposed/Reserved/Cancelled) theo từng allocation record.</summary>
        [HttpGet("{id:int:min(1)}/allocation/history")]
        [Authorize(Roles = "SalesStaff,WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetAllocationHistory(int id)
        {
            var result = await _orderService.GetAllocationHistoryAsync(id);
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetMyOrderById(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetMyOrderByIdAsync(id, userId);
            return Ok(result);
        }

        /// <summary>Sau khi sale xác nhận (Confirmed): khách chọn PayBefore (trả trước) hoặc PayAfter (trả sau). Chỉ gọi một lần.</summary>
        [HttpPatch("{id:int:min(1)}/online/payment-timing")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SetOnlineOrderPaymentTiming(int id, [FromBody] SetOnlineOrderPaymentTimingRequest? request)
        {
            if (request == null)
                return BadRequest(new { message = "Thiếu body JSON." });
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _orderService.SetOnlineOrderPaymentTimingAsync(id, userId, request.PaymentTiming);
            return Ok(result);
        }

        /// <summary>Lịch sử đơn theo bộ lọc trạng thái (mặc định Delivered). Ví dụ <c>?statusOrder=complete</c> hoặc tên enum.</summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryOrders([FromQuery] string? statusOrder)
        {
            var userId = GetCurrentUserId();

            string statusFilter;
            if (string.IsNullOrWhiteSpace(statusOrder))
                statusFilter = OrderStatus.Delivered.ToString();
            else if (string.Equals(statusOrder.Trim(), "complete", StringComparison.OrdinalIgnoreCase))
                statusFilter = OrderStatus.Delivered.ToString();
            else
                statusFilter = statusOrder.Trim();

            var query = new GetOrdersQuery { Status = statusFilter };
            var result = await _orderService.GetMyOrdersAsync(userId, query);
            return Ok(result);
        }

        /// <summary>Gợi ý họ tên, SĐT, địa chỉ từ tài khoản để màn checkout (khách có thể sửa trước khi đặt).</summary>
        [HttpGet("checkout-defaults")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCheckoutDefaults()
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetOrderCheckoutDefaultsAsync(userId);
            return Ok(result);
        }

        /// <summary>Đặt hàng online: bắt buộc thông tin người nhận (checkout).</summary>
        [HttpPost("from-cart")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateFromCart([FromBody] OrderRecipientCheckoutDto? recipient)
        {
            if (recipient == null)
                return BadRequest(new { message = "Thiếu thông tin đặt hàng (body JSON)." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _orderService.CreateOrderFromCartAsync(userId, recipient);
            return Ok(result);
        }

        /// <summary>
        /// Tạo đơn theo danh sách ProductVariantId trong giỏ hàng.
        /// Chỉ xóa các CartItem thuộc các ProductVariantId được chọn. Kèm thông tin người nhận (checkout).
        /// </summary>
        [HttpPost("from-cart/variants")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateFromCartByVariants([FromBody] CreateOrderFromCartRequest? request)
        {
            if (request == null)
                return BadRequest(new { message = "Thiếu thông tin đặt hàng (body JSON)." });

            if (request.Recipient == null)
                return BadRequest(new { message = "Thiếu thông tin người nhận (recipient)." });

            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Bạn phải chọn ít nhất 1 loại sản phẩm (items)." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _orderService.CreateOrderFromCartByVariantIdsAsync(userId, request.Items, request.Recipient);
            return Ok(result);
        }

        /// <summary>
        /// Tạo đơn bán trực tiếp (POS). TakeAway &amp; Delivery: giữ đủ thùng (Reserved) ngay, <c>Confirmed</c>; Delivery gửi <c>paymentTiming</c> PayBefore/PayAfter (mặc định PayBefore) — cùng quy tắc xuất kho &amp; giao như đơn online.
        /// </summary>
        [HttpPost("pos")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> CreatePosOrder([FromBody] CreatePosOrderRequest request)
        {
            var operatorUserId = GetCurrentUserId();
            var result = await _orderService.CreatePosOrderAsync(operatorUserId, request);
            return Ok(result);
        }

        /// <summary>Sale xác nhận đơn (IF OK → Confirmed).</summary>
        [HttpPatch("{id:int:min(1)}/sale-confirm")]
        [Authorize(Roles = "SalesStaff,Admin,Manager")]
        public async Task<IActionResult> SaleConfirm(int id)
        {
            var staffUserId = GetCurrentUserId();
            var result = await _orderService.SaleConfirmOrderAsync(id, staffUserId);
            return Ok(result);
        }

        /// <summary>
        /// ELSE: không chốt được với khách — đơn online đang PendingSaleConfirmation → hủy đơn và nhả thùng (Release box).
        /// </summary>
        [HttpPatch("{id:int:min(1)}/sale-reject")]
        [Authorize(Roles = "SalesStaff,Admin,Manager")]
        public async Task<IActionResult> SaleReject(int id)
        {
            var staffUserId = GetCurrentUserId();
            var result = await _orderService.SaleRejectOrderAsync(id, staffUserId);
            return Ok(result);
        }

        /// <summary>Đơn legacy <see cref="OrderStatus.AwaitingAllocation"/>: auto-propose + xác nhận allocate. POS mới đã giữ đủ thùng khi tạo đơn.</summary>
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

        /// <summary>
        /// Đơn Delivery đã ApprovedExport: chuyển ShippingPendingPickup → ShippingInProgress (shipper bắt đầu giao).
        /// Body: { "shippingStatus": "ShippingInProgress" } (enum name, JSON number cũng được).
        /// </summary>
        [HttpPatch("{id:int:min(1)}/shipping/status")]
        [Authorize(Roles = "SalesStaff,Admin,Manager")]
        public async Task<IActionResult> UpdateShippingStatusAsStaff(int id, [FromBody] UpdateOrderShippingStatusRequest body)
        {
            var operatorUserId = GetCurrentUserId();
            var detail = await _orderService.UpdateOrderShippingStatusAsStaffAsync(id, body.ShippingStatus, operatorUserId);
            return Ok(detail);
        }

        /// <summary>
        /// Xác nhận giao thành công (Delivery, ApprovedExport). Đơn trả sau: đồng thời quyết toán tiền
        /// (tạo thanh toán tiền mặt Paid nếu chưa có; Cash/Banking Pending hoặc Processing → Paid).
        /// </summary>
        [HttpPatch("{id:int:min(1)}/delivery/confirm")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> ConfirmDelivered(int id)
        {
            var operatorUserId = GetCurrentUserId();
            await _orderService.ConfirmDeliveredAsync(id, operatorUserId);
            return Ok(new { Message = "Đã xác nhận giao hàng thành công", OrderId = id, Status = OrderStatus.Delivered.ToString() });
        }

        [HttpPatch("{id:int:min(1)}/delivery/failed")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> ConfirmFailedDelivery(int id)
        {
            var operatorUserId = GetCurrentUserId();
            await _orderService.ConfirmFailedDeliveryAsync(id, operatorUserId);
            return Ok(new { Message = "Đã xác nhận giao hàng thất bại", OrderId = id, Status = OrderStatus.FailedDelivery.ToString() });
        }

        [HttpPatch("{id:int:min(1)}/delivery/returned")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> ConfirmReturned(int id)
        {
            var operatorUserId = GetCurrentUserId();
            await _orderService.ConfirmReturnedAsync(id, operatorUserId);
            return Ok(new { Message = "Đã xác nhận hoàn hàng", OrderId = id, Status = OrderStatus.Returned.ToString() });
        }

        [HttpPatch("{id:int:min(1)}/payment/cash/confirm-paid")]
        [Authorize(Roles = "SalesStaff,Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> ConfirmCashPaidForOrder(int id)
        {
            var operatorUserId = GetCurrentUserId();
            await _orderService.ConfirmCashPaidForOrderAsync(id, operatorUserId);
            return Ok(new { Message = "Đã xác nhận thu tiền mặt thành công", OrderId = id });
        }

        /// <summary>
        /// Khách hủy đơn: gồm PendingSaleConfirmation và đơn online giao hàng đã Confirmed (chưa paid, chưa có phiếu xuất) — nhả thùng.
        /// </summary>
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
