using AgriIDMS.Application.DTOs.Review;
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
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");

            var result = await _reviewService.CreateReviewAsync(request, customerId);
            return Ok(result);
        }

        [HttpGet("order-details/{orderDetailId:int:min(1)}/is-reviewable")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> IsReviewable(int orderDetailId)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");

            var isReviewable = await _reviewService.IsReviewableAsync(orderDetailId, customerId);
            return Ok(new { OrderDetailId = orderDetailId, IsReviewable = isReviewable });
        }
    }
}
