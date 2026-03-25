using AgriIDMS.Application.DTOs.Complaint;
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
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        /// <summary>Khách tạo khiếu nại (đơn Shipping/Completed, box thuộc đơn).</summary>
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateComplaintRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.CreateAsync(request, userId);
            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMine()
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.GetMineAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách box của đơn để customer chọn khiếu nại.
        /// Order thuộc user hiện tại + status phải Shipping/Completed.
        /// </summary>
        [HttpGet("orders/{orderId:int:min(1)}/boxes")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetOrderBoxesForComplaint([FromRoute] int orderId)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.GetOrderBoxesForComplaintAsync(orderId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Danh sách đơn của customer có thể khiếu nại (Shipping/Completed) + số lượng box eligible.
        /// </summary>
        [HttpGet("my/eligible-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetEligibleOrdersForComplaint([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.GetEligibleOrdersForCustomerAsync(userId, skip, take);
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.GetByIdAsync(id, userId);
            return Ok(result);
        }

        /// <summary>Danh sách khiếu nại cho nội bộ xử lý.</summary>
        [HttpGet("staff/all")]
        [Authorize(Roles = "Admin,SalesStaff,Manager")]
        public async Task<IActionResult> GetAllForStaff([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var result = await _complaintService.GetAllForStaffAsync(skip, take);
            return Ok(result);
        }

        [HttpGet("staff/{id:int:min(1)}")]
        [Authorize(Roles = "Admin,SalesStaff,Manager")]
        public async Task<IActionResult> GetByIdForStaff(int id)
        {
            var result = await _complaintService.GetByIdForStaffAsync(id);
            return Ok(result);
        }

        /// <summary>Nội bộ: duyệt hoặc từ chối khiến nại (chưa hoàn tiền).</summary>
        [HttpPatch("{id:int:min(1)}/verify")]
        [Authorize(Roles = "Admin,SalesStaff,Manager")]
        public async Task<IActionResult> Verify(int id, [FromBody] VerifyComplaintRequest request)
        {
            var staffId = GetCurrentUserId();
            var result = await _complaintService.VerifyAsync(id, request, staffId);
            return Ok(result);
        }

        /// <summary>Khách hủy khiếu nại khi còn Pending.</summary>
        [HttpPost("{id:int:min(1)}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.CancelPendingAsync(id, userId);
            return Ok(result);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");
        }
    }
}
