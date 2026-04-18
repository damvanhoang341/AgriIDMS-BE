using AgriIDMS.Application.DTOs.DamageReport;
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
    public class DamageReportsController : ControllerBase
    {
        private readonly IDamageReportService _service;

        public DamageReportsController(IDamageReportService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "WarehouseStaff,Manager,Admin")]
        public async Task<IActionResult> GetList([FromQuery] string? status = null, [FromQuery] int? warehouseId = null)
        {
            DamageReportStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<DamageReportStatus>(status, true, out var tmp))
            {
                parsedStatus = tmp;
            }

            int? filterWarehouse = null;
            string? filterReporter = null;
            var isStaffOnly = User.IsInRole("WarehouseStaff") && !User.IsInRole("Manager") && !User.IsInRole("Admin");
            if (isStaffOnly)
            {
                if (warehouseId.HasValue && warehouseId.Value > 0)
                    filterWarehouse = warehouseId;
                else
                    filterReporter = GetCurrentUserId();
            }
            else if (warehouseId.HasValue && warehouseId.Value > 0)
                filterWarehouse = warehouseId;

            var result = await _service.GetListAsync(parsedStatus, filterWarehouse, filterReporter);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "WarehouseStaff,Manager,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateDamageReportRequest request)
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var result = await _service.CreateAsync(request, userId, username);
            return Ok(new { id = result.Id, message = "Đã gửi phiếu hỏng." });
        }

        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Approve([FromRoute] int id, [FromBody] ApproveDamageReportRequest request)
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var result = await _service.ApproveAsync(id, request, userId, username);
            return Ok(new { message = "Đã duyệt phiếu hỏng.", data = result });
        }

        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] RejectDamageReportRequest request)
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var result = await _service.RejectAsync(id, request, userId, username);
            return Ok(new { message = "Đã từ chối phiếu hỏng.", data = result });
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("userId")
                ?? User.FindFirstValue("id")
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại.");
        }

        private string GetCurrentUsername()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("unique_name")
                ?? User.FindFirstValue("username")
                ?? "unknown";
        }
    }
}

