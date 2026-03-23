using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/goods-receipt-details")]
    [ApiController]
    [Authorize]
    public class GoodsReceiptDetailsController : ControllerBase
    {
        private readonly IGoodsReceiptDetailService _detailService;

        public GoodsReceiptDetailsController(IGoodsReceiptDetailService detailService)
        {
            _detailService = detailService;
        }

        // ADD RECEIPT DETAIL
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> AddDetail([FromBody] AddGoodsReceiptDetailRequest request)
        {
            await _detailService.AddGoodsReceiptDetailAsync(request);

            return Ok(new
            {
                Message = "Thêm chi tiết phiếu nhập thành công"
            });
        }

        // UPDATE RECEIPT DETAIL
        [HttpPut]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> UpdateDetail([FromBody] UpdateGoodsReceiptDetailRequest request)
        {
            await _detailService.UpdateGoodsReceiptDetailAsync(request);

            return Ok(new
            {
                Message = "Cập nhật chi tiết phiếu nhập thành công"
            });
        }

        // DELETE RECEIPT DETAIL
        [HttpDelete("{detailId:int:min(1)}")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> DeleteDetail(int detailId)
        {
            await _detailService.DeleteGoodsReceiptDetailAsync(detailId);

            return Ok(new
            {
                Message = "Xóa chi tiết phiếu nhập thành công"
            });
        }
    }
}

