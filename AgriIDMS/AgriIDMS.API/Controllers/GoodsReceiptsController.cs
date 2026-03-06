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
        private readonly IGoodsReceiptService _receiptService;

        public GoodsReceiptsController(ILogger<GoodsReceiptsController> logger, IGoodsReceiptService receiptService)
        {
            _logger = logger;
            _receiptService = receiptService;
        }

        /// <summary>
        /// Tạo phiếu nhập kho
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateGoodsReceipt(
        [FromBody] CreateGoodsReceiptRequest request)
        {
            _logger.LogInformation("CreateGoodsReceipt API called");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst("sub")?.Value;

            var receiptId = await _receiptService.CreateGoodsReceiptAsync(request, currentUserId!);

            _logger.LogInformation("GoodsReceipt created successfully with Id {Id}", receiptId);

            return Ok(new
            {
                Message = "Tạo phiếu nhập thành công",
                ReceiptId = receiptId
            });
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveGoodsReceipt(int id)
        {
            var currentUserId = User.FindFirst("sub")?.Value;

            _logger.LogInformation(
                "User {UserId} is approving GoodsReceipt {ReceiptId}",
                currentUserId,
                id
            );

            await _receiptService.ApproveGoodsReceiptAsync(id, currentUserId!);

            _logger.LogInformation(
                "GoodsReceipt {ReceiptId} approved successfully",
                id
            );

            return Ok(new
            {
                Message = "Duyệt phiếu nhập thành công"
            });
        }
    }
}
