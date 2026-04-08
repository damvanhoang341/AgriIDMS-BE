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

        /// <summary>
        /// Tạo phiếu xuất: đơn <c>Confirmed</c>, thanh toán đủ điều kiện theo <c>PaymentTiming</c>, có allocation <c>Reserved</c>, chưa có phiếu xuất hoạt động.
        /// </summary>
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
            var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Admin");
            var result = await _exportService.CancelExportAsync(exportId, userId, isManagerOrAdmin);
            return Ok(result);
        }

        /// <summary>Dữ liệu in phiếu xuất (FE dựng HTML). PendingPick = preview; từ ReadyToExport = snapshot đã chốt.</summary>
        [HttpGet("{exportId:int:min(1)}/print-data")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetExportPrintData(int exportId)
        {
            var result = await _exportService.GetExportPrintDataAsync(exportId);
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

        /// <summary>
        /// Danh sách phiếu <c>ReadyToExport</c> chờ Manager/Admin duyệt. Query: skip, take, sort (createdAtDesc mặc định, createdAtAsc).
        /// </summary>
        [HttpGet("staff/pending-approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetPendingApproveExports([FromQuery] GetPendingApproveExportsQuery query)
        {
            var result = await _exportService.GetPendingApproveExportsAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// Phiếu đã xác nhận pick (<c>ReadyToExport</c>), chờ Manager duyệt — kho xem lại các phiếu đã xác nhận lấy hàng.
        /// Cùng dữ liệu với <c>staff/pending-approve</c>, mở thêm cho WarehouseStaff.
        /// </summary>
        [HttpGet("warehouse/post-pick")]
        [Authorize(Roles = "WarehouseStaff,Admin,Manager")]
        public async Task<IActionResult> GetWarehousePostPickExports([FromQuery] GetPendingApproveExportsQuery query)
        {
            var result = await _exportService.GetPendingApproveExportsAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// Phiếu xuất đã duyệt xuất kho thành công (<c>Approved</c>) — Manager/Admin xem lại lịch sử.
        /// </summary>
        [HttpGet("staff/approved")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetApprovedExports([FromQuery] GetPendingApproveExportsQuery query)
        {
            var result = await _exportService.GetApprovedExportsAsync(query);
            return Ok(result);
        }

        [HttpGet("get-all-export")]
        public async Task<IActionResult> GetAllExport()
        {
            var result = await _exportService.GetAllExport();
            return Ok(result);
        }
    }
}
