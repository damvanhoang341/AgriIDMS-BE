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
            var autoApproveWhenCreatedByManager = User.IsInRole("Manager");

            var receiptId = await _goodsReceiptService.CreateGoodsReceiptAsync(
                request,
                userId,
                autoApproveWhenCreatedByManager);

            return Ok(new
            {
                Message = autoApproveWhenCreatedByManager
                    ? "Tạo phiếu nhập thành công và đã tự động duyệt theo quyền Manager"
                    : "Tạo phiếu nhập thành công",
                ReceiptId = receiptId
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
        // UPDATE WAREHOUSE (chuyển đổi kho đích; chỉ khi phiếu chưa Approved)
        // ===============================
        [HttpPut("{receiptId:int:min(1)}/warehouse")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> UpdateWarehouse(int receiptId, [FromBody] UpdateGoodsReceiptWarehouseRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.UpdateWarehouseAsync(receiptId, request, userId);
            return Ok(new { Message = "Đã cập nhật kho đích của phiếu nhập" });
        }

        // ===============================
        // GENERATE BOXES (chỉ sau khi phiếu Approved)
        // ===============================
        [HttpPost("boxes")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GenerateBoxes([FromBody] CreateBoxesRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var created = await _goodsReceiptService.GenerateBoxesAsync(request, userId);

            return Ok(new { message = "Tạo box thành công", boxes = created });
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
        // MANAGER REVIEW MIN WEIGHT (Approve/Reject khi status = PendingManagerApprovalQc)
        // ===============================
        public class ManagerReviewMinWeightRequest
        {
            public bool Approve { get; set; }
        }

        [HttpPost("{receiptId}/manager-review-min")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManagerReviewMin(int receiptId, [FromBody] ManagerReviewMinWeightRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.ManagerReviewMinWeightAsync(receiptId, request.Approve, userId);

            return Ok(new
            {
                Message = request.Approve
                    ? "Phiếu nhập dưới định mức tối thiểu đã được Manager cho phép tiếp tục QC/Approve"
                    : "Phiếu nhập dưới định mức tối thiểu đã bị Manager từ chối"
            });
        }

        // ===============================
        // MANAGER REVIEW TOLERANCE (Approve/Reject khi status = PendingManagerApproval)
        // ===============================
        public class ManagerReviewToleranceRequest
        {
            public bool Approve { get; set; }
        }

        [HttpPost("{receiptId}/manager-review-tolerance")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManagerReviewTolerance(int receiptId, [FromBody] ManagerReviewToleranceRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _goodsReceiptService.ManagerReviewToleranceAsync(receiptId, request.Approve, userId);

            return Ok(new
            {
                Message = request.Approve
                    ? "Phiếu nhập vượt dung sai đã được Manager duyệt"
                    : "Phiếu nhập vượt dung sai đã bị Manager từ chối"
            });
        }
    }
}
