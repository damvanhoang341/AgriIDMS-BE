using AgriIDMS.Application.DTOs.StockCheck;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class StockChecksController : ControllerBase
    {
        private readonly IStockCheckService _stockCheckService;

        public StockChecksController(IStockCheckService stockCheckService)
        {
            _stockCheckService = stockCheckService;
        }

        /// <summary>Tạo phiếu kiểm kê (Draft). Full = toàn bộ box trong kho; Spot = danh sách BoxIds.</summary>
        [HttpPost]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Create([FromBody] CreateStockCheckRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var id = await _stockCheckService.CreateAsync(request, userId);
            return Ok(new { Message = "Tạo phiếu kiểm kê thành công", StockCheckId = id });
        }

        /// <summary>Bắt đầu kiểm kê (Draft → InProgress), khóa snapshot.</summary>
        [HttpPost("{id}/start")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Start(int id)
        {
            await _stockCheckService.StartCheckAsync(id);
            return Ok(new { Message = "Đã bắt đầu kiểm kê" });
        }

        /// <summary>Nhập số đếm thực tế cho một dòng.</summary>
        [HttpPut("detail/counted")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> UpdateCountedWeight([FromBody] UpdateCountedWeightRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _stockCheckService.UpdateCountedWeightAsync(request, userId);
            return Ok(new { Message = "Đã cập nhật số đếm" });
        }

        /// <summary>Chốt đếm (InProgress → Counted). Tất cả dòng phải đã nhập CountedWeight.</summary>
        [HttpPost("{id}/complete")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> CompleteCount(int id)
        {
            await _stockCheckService.CompleteCountAsync(id);
            return Ok(new { Message = "Đã chốt đếm" });
        }

        /// <summary>Duyệt phiếu kiểm kê: tạo điều chỉnh tồn kho (Shortage/Excess) và cập nhật Box.Weight.</summary>
        [HttpPost("{id}/approve")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _stockCheckService.ApproveAsync(id, userId);
            return Ok(new { Message = "Đã duyệt phiếu kiểm kê" });
        }

        /// <summary>Từ chối phiếu kiểm kê (Counted → Rejected).</summary>
        [HttpPost("{id}/reject")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _stockCheckService.RejectAsync(id, userId);
            return Ok(new { Message = "Đã từ chối phiếu kiểm kê" });
        }
    }
}
