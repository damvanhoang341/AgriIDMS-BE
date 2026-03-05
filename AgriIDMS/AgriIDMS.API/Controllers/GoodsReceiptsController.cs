using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using BaseApp.API.Controllers;
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
        private readonly ILogger<AuthController> _logger;
        private readonly GoodsReceiptService _receiptService;

        public GoodsReceiptsController(ILogger<AuthController> logger, GoodsReceiptService receiptService)
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst("sub")?.Value;

            var receiptId = await _receiptService.CreateGoodsReceiptAsync(request, currentUserId!);

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

            await _receiptService.ApproveGoodsReceiptAsync(id, currentUserId!);

            return Ok(new
            {
                Message = "Duyệt phiếu nhập thành công"
            });
        }
    }
}
