using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // nếu có JWT
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
        // CREATE RECEIPT
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CreateReceipt([FromBody] CreateGoodsReceiptRequest request)
        {
            var userId = User.Identity?.Name ?? "system";

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
        public async Task<IActionResult> AddDetail([FromBody] AddGoodsReceiptDetailRequest request)
        {
            await _goodsReceiptService.AddGoodsReceiptDetailAsync(request);

            return Ok(new
            {
                Message = "Thêm chi tiết phiếu nhập thành công"
            });
        }

        // ===============================
        // UPDATE TRUCK WEIGHT
        // ===============================
        [HttpPut("truck-weight")]
        public async Task<IActionResult> UpdateTruckWeight([FromBody] UpdateTruckWeightRequest request)
        {
            await _goodsReceiptService.UpdateTruckWeightAsync(request);

            return Ok(new
            {
                Message = "Cập nhật trọng lượng xe thành công"
            });
        }

        // ===============================
        // QC INSPECTION
        // ===============================
        [HttpPost("qc")]
        public async Task<IActionResult> QCInspection([FromBody] QCInspectionRequest request)
        {
            var userId = User.Identity?.Name ?? "system";

            await _goodsReceiptService.QCInspectionAsync(request, userId);

            return Ok(new
            {
                Message = "QC kiểm tra thành công"
            });
        }

        // ===============================
        // GENERATE BOXES
        // ===============================
        [HttpPost("boxes")]
        public async Task<IActionResult> GenerateBoxes([FromBody] CreateBoxesRequest request)
        {
            await _goodsReceiptService.GenerateBoxesAsync(request);

            return Ok(new
            {
                Message = "Tạo box thành công"
            });
        }

        // ===============================
        // APPROVE RECEIPT
        // ===============================
        [HttpPost("{receiptId}/approve")]
        public async Task<IActionResult> ApproveReceipt(int receiptId)
        {
            var userId = User.Identity?.Name ?? "system";

            await _goodsReceiptService.ApproveGoodsReceiptAsync(receiptId, userId);

            return Ok(new
            {
                Message = "Phiếu nhập đã được duyệt"
            });
        }
    }
}
