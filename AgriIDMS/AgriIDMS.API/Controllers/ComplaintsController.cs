using AgriIDMS.Application.DTOs.Complaint;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        /// <summary>Khách tạo khiếu nại (đơn Shipping/Completed, box thuộc đơn).</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateComplaintRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.CreateAsync(request, userId);
            return Ok(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.GetMineAsync(userId);
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _complaintService.GetByIdAsync(id, userId);
            return Ok(result);
        }

        /// <summary>Danh sách khiếu nại cho nội bộ xử lý.</summary>
        [HttpGet("staff/all")]
        //[Authorize(Roles = "Admin,Sale,Manager")]
        public async Task<IActionResult> GetAllForStaff([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var result = await _complaintService.GetAllForStaffAsync(skip, take);
            return Ok(result);
        }

        [HttpGet("staff/{id:int:min(1)}")]
        //[Authorize(Roles = "Admin,Sale,Manager")]
        public async Task<IActionResult> GetByIdForStaff(int id)
        {
            var result = await _complaintService.GetByIdForStaffAsync(id);
            return Ok(result);
        }

        /// <summary>Nội bộ: duyệt hoặc từ chối khiến nại (chưa hoàn tiền).</summary>
        [HttpPatch("{id:int:min(1)}/verify")]
        //[Authorize(Roles = "Admin,Sale,Manager")]
        public async Task<IActionResult> Verify(int id, [FromBody] VerifyComplaintRequest request)
        {
            var staffId = GetCurrentUserId();
            var result = await _complaintService.VerifyAsync(id, request, staffId);
            return Ok(result);
        }

        /// <summary>Khách hủy khiếu nại khi còn Pending.</summary>
        [HttpPost("{id:int:min(1)}/cancel")]
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
