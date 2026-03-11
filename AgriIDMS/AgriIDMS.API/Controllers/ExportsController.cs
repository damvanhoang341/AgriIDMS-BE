using AgriIDMS.Application.DTOs.Export;
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
    public class ExportsController : ControllerBase
    {
        private readonly IExportService _exportService;

        public ExportsController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> CreateExportReceipt([FromBody] CreateExportReceiptRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _exportService.CreateExportReceiptAsync(request.OrderId, userId);
            return Ok(result);
        }

        [HttpPatch("{exportId:int:min(1)}/confirm-pick")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> ConfirmPick(int exportId)
        {
            var userId = GetCurrentUserId();
            var result = await _exportService.ConfirmPickAsync(exportId, userId);
            return Ok(result);
        }

        [HttpPatch("{exportId:int:min(1)}/approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ApproveExport(int exportId)
        {
            var userId = GetCurrentUserId();
            var result = await _exportService.ApproveExportAsync(exportId, userId);
            return Ok(result);
        }

        [HttpPatch("{exportId:int:min(1)}/cancel")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> CancelExport(int exportId)
        {
            var userId = GetCurrentUserId();
            var result = await _exportService.CancelExportAsync(exportId, userId);
            return Ok(result);
        }

        [HttpGet("{exportId:int:min(1)}")]
        public async Task<IActionResult> GetExportReceipt(int exportId)
        {
            var result = await _exportService.GetExportReceiptAsync(exportId);
            return Ok(result);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Application.Exceptions.UnauthorizedException("Không xác định được người dùng hiện tại");
        }
    }
}
