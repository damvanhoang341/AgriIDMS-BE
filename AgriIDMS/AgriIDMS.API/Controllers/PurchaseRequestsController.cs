using AgriIDMS.Application.DTOs.PurchaseRequest;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    /// <summary>API phiếu đề xuất mua — gom nhu cầu trước khi tạo đơn mua.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseRequestsController : ControllerBase
    {
        private readonly IPurchaseRequestService _service;

        public PurchaseRequestsController(IPurchaseRequestService service)
        {
            _service = service;
        }

        /// <summary>Danh sách tất cả phiếu đề xuất mua.</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng.</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Chi tiết một phiếu đề xuất mua theo Id.</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng.</remarks>
        [HttpGet("{id:int:min(1)}")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>Tạo phiếu đề xuất mua mới (gồm sản phẩm, khối lượng, giá mục tiêu).</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng.</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseRequestRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var id = await _service.CreateAsync(request, userId!);
            return Ok(new { Message = "Tạo phiếu đề xuất mua thành công", PurchaseRequestId = id });
        }

        /// <summary>Từ phiếu đề xuất mua, tạo đơn mua (chọn NCC + phân bổ theo từng dòng).</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng — cùng nhóm quyền với tạo PO trực tiếp.</remarks>
        [HttpPost("{id:int:min(1)}/create-purchase-order")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> CreatePurchaseOrder(int id, [FromBody] CreatePurchaseOrderFromRequestRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var poId = await _service.CreatePurchaseOrderAsync(id, request, userId!);
            return Ok(new { Message = "Tạo đơn mua từ phiếu đề xuất thành công", PurchaseOrderId = poId });
        }
    }
}
