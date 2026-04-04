using AgriIDMS.Application.DTOs.Payment;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _paymentService.CreatePaymentAsync(request, userId);
            return Ok(result);
        }

        [HttpGet("order/{orderId:int:min(1)}")]
        public async Task<IActionResult> GetLatestPayment(int orderId)
        {
            var userId = GetCurrentUserId();
            var result = await _paymentService.GetLatestPaymentAsync(orderId, userId);
            return Ok(result);
        }

        [HttpGet("staff/pending-cash")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff,SalesStaff")]
        public async Task<IActionResult> GetPendingCashPayments([FromQuery] GetPendingCashPaymentsQuery query)
        {
            var result = await _paymentService.GetPendingCashPaymentsAsync(query);
            return Ok(result);
        }

        /// <summary>Alias URL cũ.</summary>
        [HttpGet("staff/pending-cod")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff,SalesStaff")]
        public Task<IActionResult> GetPendingCashPaymentsLegacy([FromQuery] GetPendingCashPaymentsQuery query) =>
            GetPendingCashPayments(query);

        [HttpPatch("{paymentId:int:min(1)}/confirm-cash")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff,SalesStaff")]
        public async Task<IActionResult> ConfirmCashPaymentPaid(int paymentId)
        {
            var result = await _paymentService.ConfirmCashPaymentPaidAsync(paymentId);
            return Ok(result);
        }

        /// <summary>Alias URL cũ.</summary>
        [HttpPatch("{paymentId:int:min(1)}/confirm-cod")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff,SalesStaff")]
        public Task<IActionResult> ConfirmCashPaymentPaidLegacy(int paymentId) => ConfirmCashPaymentPaid(paymentId);

        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePayOSWebhook([FromBody] JsonElement body)
        {
            await _paymentService.HandlePayOSWebhookAsync(body.GetRawText());
            return Ok(new { success = true });
        }

        [HttpPost("{paymentId:int:min(1)}/cancel")]
        public async Task<IActionResult> CancelBankingPayment(int paymentId)
        {
            var userId = GetCurrentUserId();
            var result = await _paymentService.CancelBankingPaymentAsync(paymentId, userId);
            return Ok(result);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");
        }
    }
}
