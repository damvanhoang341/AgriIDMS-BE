using AgriIDMS.Application.DTOs.PurchaseOrder;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(
            IPurchaseOrderService purchaseOrderService,
            ILogger<PurchaseOrderController> logger)
        {
            _purchaseOrderService = purchaseOrderService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo Purchase Order (PurchasingStaff tạo đơn, Manager duyệt)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "User {UserId} is creating PurchaseOrder for Supplier {SupplierId}",
                userId,
                request.SupplierId);

            var id = await _purchaseOrderService.CreateAsync(request, userId!);

            _logger.LogInformation(
                "PurchaseOrder {PurchaseOrderId} created successfully",
                id);

            return Ok(new
            {
                Message = "Tạo đơn mua thành công",
                PurchaseOrderId = id
            });
        }

        /// <summary>
        /// Lấy PurchaseOrder theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> GetPurchaseOrderById(int id)
        {
            _logger.LogInformation("Fetching PurchaseOrder {PurchaseOrderId}", id);

            var result = await _purchaseOrderService.GetByIdAsync(id);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật PurchaseOrder (chỉ khi trạng thái Pending, chưa duyệt).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> UpdatePurchaseOrder(int id, [FromBody] UpdatePurchaseOrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _purchaseOrderService.UpdateAsync(id, request, userId!);
            return Ok(new { Message = "Cập nhật đơn mua thành công" });
        }

        /// <summary>
        /// Duyệt PurchaseOrder (chỉ Manager hoặc Admin)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ApprovePurchaseOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "User {UserId} is approving PurchaseOrder {PurchaseOrderId}",
                userId,
                id);

            await _purchaseOrderService.ApprovePurchaseOrderAsync(id, userId!);

            _logger.LogInformation(
                "PurchaseOrder {PurchaseOrderId} approved successfully",
                id);

            return Ok(new
            {
                Message = "Duyệt đơn mua thành công"
            });
        }

        /// <summary>
        /// Xóa PurchaseOrder (chỉ khi Pending và chưa có phiếu nhập kho).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> DeletePurchaseOrder(int id)
        {
            await _purchaseOrderService.DeleteAsync(id);
            return Ok(new { Message = "Xóa đơn mua thành công" });
        }

        [HttpGet()]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _purchaseOrderService.GetAllAsync();
            return Ok(result);
        }
    }
}
