using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotsController : ControllerBase
    {
        private readonly ILotService _lotService;

        public LotsController(ILotService lotService)
        {
            _lotService = lotService;
        }

        [HttpGet("by-goods-receipt/{goodsReceiptId}")]
        public async Task<IActionResult> GetLotsByGoodsReceiptId(int goodsReceiptId)
        {
            var lots = await _lotService.GetLotsByGoodsReceiptIdAsync(goodsReceiptId);

            return Ok(lots);
        }
    }
}
