using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly ILogger<GoodsReceiptsController> _logger;
        private readonly IGoodsReceiptService _goodsReceiptService;

        public GoodsReceiptsController(ILogger<GoodsReceiptsController> logger, IGoodsReceiptService receiptService)
        {
            _logger = logger;
            _goodsReceiptService = receiptService;
        }

        // ===============================
        // GET ALL / GET BY ID / GET DETAILS
        // ===============================

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _goodsReceiptService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _goodsReceiptService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}/details")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var result = await _goodsReceiptService.GetByIdAsync(id);
            return Ok(result.Details);
        }

        /// <summary>Phiếu nhập kèm giá nhập để Manager/Admin xem xét khi Approve/Reject. Warehouse không gọi được.</summary>
        [HttpGet("{id:int:min(1)}/for-approval")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetForApproval(int id)
        {
            var result = await _goodsReceiptService.GetByIdForApprovalAsync(id);
            return Ok(result);
        }

        // ===============================
        // CREATE RECEIPT (WarehouseStaff / Manager / Admin)
        // ===============================
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> CreateReceipt([FromBody] CreateGoodsReceiptRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var receiptId = await _goodsReceiptService.CreateGoodsReceiptAsync(request, userId);

            return Ok(new
            {
                Message = "Tạo phiếu nhập thành công",
                ReceiptId = receiptId
            });
        }

        // ===============================
        // ADD RECEIPT DETAIL
        // ===============================
        [HttpPost("detail")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> AddDetail([FromBody] AddGoodsReceiptDetailRequest request)
        {
            await _goodsReceiptService.AddGoodsReceiptDetailAsync(request);

            return Ok(new
            {
                Message = "Thêm chi tiết phiếu nhập thành công"
            });
        }

        // ===============================
        // UPDATE RECEIPT DETAIL
        // ===============================
        [HttpPut("detail")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> UpdateDetail([FromBody] UpdateGoodsReceiptDetailRequest request)
        {
            await _goodsReceiptService.UpdateGoodsReceiptDetailAsync(request);

            return Ok(new
            {
                Message = "Cập nhật chi tiết phiếu nhập thành công"
            });
        }

        // ===============================
        // DELETE RECEIPT DETAIL
        // ===============================
        [HttpDelete("detail/{detailId:int:min(1)}")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> DeleteDetail(int detailId)
        {
            await _goodsReceiptService.DeleteGoodsReceiptDetailAsync(detailId);

            return Ok(new
            {
                Message = "Xóa chi tiết phiếu nhập thành công"
            });
        }


        // ===============================
        // QC INSPECTION
        // ===============================
        [HttpPost("qc")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> QCInspection([FromBody] QCInspectionRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            await _goodsReceiptService.QCInspectionAsync(request, userId);

            return Ok(new
            {
                Message = "QC kiểm tra thành công"
            });
        }

        // ===============================
        // GENERATE BOXES (chỉ sau khi phiếu Approved)
        // ===============================
        [HttpPost("boxes")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GenerateBoxes([FromBody] CreateBoxesRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.GenerateBoxesAsync(request, userId);

            return Ok(new { Message = "Tạo box thành công" });
        }

        // ===============================
        // APPROVE RECEIPT (Manager/Admin; tolerance check → Approved hoặc PendingManagerApproval)
        // ===============================
        [HttpPost("{receiptId}/approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ApproveReceipt(int receiptId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.ApproveGoodsReceiptAsync(receiptId, userId);
            return Ok(new { Message = "Phiếu nhập đã được xử lý (duyệt hoặc chuyển chờ Manager)" });
        }

        // ===============================
        // MANAGER APPROVE (khi status = PendingManagerApproval)
        // ===============================
        [HttpPost("{receiptId}/manager-approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManagerApproveReceipt(int receiptId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.ManagerApproveReceiptAsync(receiptId, userId);
            return Ok(new { Message = "Phiếu nhập đã được Manager duyệt" });
        }

        // ===============================
        // MANAGER REJECT (khi status = PendingManagerApproval)
        // ===============================
        [HttpPost("{receiptId}/manager-reject")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManagerRejectReceipt(int receiptId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.ManagerRejectReceiptAsync(receiptId, userId);
            return Ok(new { Message = "Phiếu nhập đã bị Manager từ chối" });
        }
    }
}
