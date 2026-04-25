using AgriIDMS.Application.DTOs.PurchaseRequest;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    /// <summary>API đề xuất mua (Purchase Request) — gom nhu cầu trước khi tạo PO.</summary>
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

        /// <summary>Danh sách tất cả purchase request.</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng.</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Chi tiết một purchase request theo Id.</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng.</remarks>
        [HttpGet("{id:int:min(1)}")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>Tạo purchase request mới (phiếu nhu cầu: sản phẩm, khối lượng, giá mục tiêu).</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng.</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseRequestRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var id = await _service.CreateAsync(request, userId!);
            return Ok(new { Message = "Tạo purchase request thành công", PurchaseRequestId = id });
        }

        /// <summary>Từ purchase request, tạo purchase order (chọn NCC + phân bổ dòng theo PR).</summary>
        /// <remarks>Admin, Quản lý, Nhân viên mua hàng — cùng nhóm quyền với tạo PO trực tiếp.</remarks>
        [HttpPost("{id:int:min(1)}/create-purchase-order")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> CreatePurchaseOrder(int id, [FromBody] CreatePurchaseOrderFromRequestRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var poId = await _service.CreatePurchaseOrderAsync(id, request, userId!);
            return Ok(new { Message = "Tạo purchase order từ request thành công", PurchaseOrderId = poId });
        }
    }
}
