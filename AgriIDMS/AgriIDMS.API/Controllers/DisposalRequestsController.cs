using AgriIDMS.Application.DTOs.Disposal;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DisposalRequestsController : ControllerBase
    {
        private readonly IDisposalRequestService _service;

        public DisposalRequestsController(IDisposalRequestService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateDisposalRequestDto dto)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("Bạn cần đăng nhập để gửi yêu cầu tiêu hủy.");

            var id = await _service.CreateRequestAsync(dto, userId);
            return Ok(new { id, message = "Đã gửi yêu cầu tiêu hủy, chờ Quản lí duyệt." });
        }

        [HttpPost("direct-dispose")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> DirectDispose([FromBody] CreateDisposalRequestDto dto)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("Bạn cần đăng nhập để tiêu hủy.");

            await _service.DirectDisposeAsync(dto, userId);
            return Ok(new { message = "Đã tiêu hủy hàng hóa thành công." });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetList([FromQuery] string? status = null, [FromQuery] int? warehouseId = null)
        {
            DisposalRequestStatus? st = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DisposalRequestStatus>(status, true, out var parsed))
                st = parsed;

            var items = await _service.GetRequestsAsync(st, warehouseId);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var item = await _service.GetRequestDetailAsync(id);
            return Ok(item);
        }

        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Approve(int id, [FromBody] string? reviewNote = null)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("Bạn cần đăng nhập để duyệt yêu cầu.");

            await _service.ApproveAsync(id, userId, reviewNote);
            return Ok(new { message = "Đã duyệt yêu cầu tiêu hủy." });
        }

        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? reviewNote = null)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("Bạn cần đăng nhập để từ chối yêu cầu.");

            await _service.RejectAsync(id, userId, reviewNote);
            return Ok(new { message = "Đã từ chối yêu cầu tiêu hủy." });
        }
    }
}

