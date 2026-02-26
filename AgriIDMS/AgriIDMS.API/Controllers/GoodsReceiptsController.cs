using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Services;
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
        public async Task<IActionResult> Create(
            [FromBody] CreateGoodsReceiptRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = User?.Identity?.Name ?? "065cc85f-bcca-4076-85e5-b913741f5df9";

                var id = await _receiptService.CreateGoodsReceiptAsync(request, currentUserId);

                return Ok(new
                {
                    message = "Tạo phiếu nhập kho thành công",
                    id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
