using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public GoodsReceiptsController(
            ILogger<GoodsReceiptsController> logger,
            IGoodsReceiptService receiptService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _goodsReceiptService = receiptService;
            _userManager = userManager;
        }

        private async Task<string> ResolveCurrentUserIdOrThrowAsync()
        {
            var candidateId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("sub");

            if (!string.IsNullOrWhiteSpace(candidateId))
            {
                // Chỉ chấp nhận trực tiếp khi candidate đúng là AspNetUsers.Id.
                var byId = await _userManager.FindByIdAsync(candidateId);
                if (byId != null && !string.IsNullOrWhiteSpace(byId.Id))
                    return byId.Id;
            }

            // Fallback cho token chỉ có username nhưng thiếu NameIdentifier/Sub.
            var userName =
                User.FindFirstValue(ClaimTypes.Name) ??
                User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ??
                User.FindFirstValue("unique_name");

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var user = await _userManager.FindByNameAsync(userName);
                if (user != null && !string.IsNullOrWhiteSpace(user.Id))
                    return user.Id;
            }

            // Một số token/custom provider có thể nhét username vào sub.
            if (!string.IsNullOrWhiteSpace(candidateId))
            {
                var byName = await _userManager.FindByNameAsync(candidateId);
                if (byName != null && !string.IsNullOrWhiteSpace(byName.Id))
                    return byName.Id;

                var byEmail = await _userManager.FindByEmailAsync(candidateId);
                if (byEmail != null && !string.IsNullOrWhiteSpace(byEmail.Id))
                    return byEmail.Id;
            }

            throw new UnauthorizedAccessException("Không xác định được UserId từ access token.");
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

        /// <summary>Dữ liệu in "Phiếu nhập kho" (FE HTML). phase: afterQc | afterApprove (mặc định tự chọn). preview=true: xem trước.</summary>
        [HttpGet("{id:int:min(1)}/print-data")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> GetPrintData(int id, [FromQuery] string? phase, [FromQuery] bool preview = false)
        {
            var result = await _goodsReceiptService.GetGoodsReceiptPrintDataAsync(id, phase, preview);
            return Ok(result);
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
            var userId = await ResolveCurrentUserIdOrThrowAsync();
            // Admin/Manager: bỏ qua duyệt bước 1 (Draft → Received), vẫn phải QC và duyệt bước 2 như phiếu thường.
            var autoSkipFirstApproval = User.IsInRole("Admin") || User.IsInRole("Manager");

            var receiptId = await _goodsReceiptService.CreateGoodsReceiptAsync(
                request,
                userId,
                autoSkipFirstApproval);

            return Ok(new
            {
                Message = "Tạo phiếu nhập thành công",
                ReceiptId = receiptId
            });
        }

        // ===============================
        // QC INSPECTION
        // ===============================
        [HttpPost("qc")]
        [Authorize(Roles = "WarehouseStaff")]
        public async Task<IActionResult> QCInspection([FromBody] QCInspectionRequest request)
        {
            var userId = await ResolveCurrentUserIdOrThrowAsync();
            var autoApproveWhenEligible = false;

            await _goodsReceiptService.QCInspectionAsync(request, userId, autoApproveWhenEligible);

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
            var userId = await ResolveCurrentUserIdOrThrowAsync();
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
            var userId = await ResolveCurrentUserIdOrThrowAsync();
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
            var userId = await ResolveCurrentUserIdOrThrowAsync();
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
            var userId = await ResolveCurrentUserIdOrThrowAsync();
            await _goodsReceiptService.ManagerReviewMinWeightAsync(receiptId, request.Approve, userId);

            return Ok(new
            {
                Message = request.Approve
                    ? "Phiếu nhập dưới định mức tối thiểu đã được Manager cho phép tiếp tục QC/Approve"
                    : "Phiếu nhập dưới định mức tối thiểu đã bị Manager từ chối"
            });
        }

        [HttpPost("{receiptId}/manager-allow-qc")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManagerAllowQc(int receiptId)
        {
            var userId = await ResolveCurrentUserIdOrThrowAsync();
            await _goodsReceiptService.ManagerAllowQcAsync(receiptId, userId);

            return Ok(new
            {
                Message = "Đã cho phép kiểm tra chất lượng lại"
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
            var userId = await ResolveCurrentUserIdOrThrowAsync();
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
