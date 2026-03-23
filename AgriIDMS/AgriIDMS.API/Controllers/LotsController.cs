using AgriIDMS.Application.DTOs.Lot;
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

        [HttpGet("near-expiry-lots")]
        public async Task<IActionResult> GetNearExpiryLotsAsync()
        {
            var lots = await _lotService.GetNearExpiryLotsAsync();
            return Ok(lots);
        }

        [HttpGet("near-expiry-dashboard")]
        public async Task<IActionResult> GetNearExpiryDashboardAsync([FromQuery] int days = 3)
        {
            var dashboard = await _lotService.GetNearExpiryDashboardAsync(days);
            return Ok(dashboard);
        }
    }
}
