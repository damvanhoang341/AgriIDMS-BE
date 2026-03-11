using AgriIDMS.Application.DTOs.Payment;
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

        [HttpPatch("{paymentId:int:min(1)}/confirm-cod")]
        [Authorize(Roles = "Admin,Shipper")]
        public async Task<IActionResult> ConfirmCODPaid(int paymentId)
        {
            var result = await _paymentService.ConfirmCODPaidAsync(paymentId);
            return Ok(result);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");
        }
    }
}
