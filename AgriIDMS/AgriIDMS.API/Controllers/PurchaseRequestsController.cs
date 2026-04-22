using AgriIDMS.Application.DTOs.PurchaseRequest;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
    public class PurchaseRequestsController : ControllerBase
    {
        private readonly IPurchaseRequestService _service;

        public PurchaseRequestsController(IPurchaseRequestService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseRequestRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var id = await _service.CreateAsync(request, userId!);
            return Ok(new { Message = "Tạo purchase request thành công", PurchaseRequestId = id });
        }

        [HttpPost("{id:int:min(1)}/create-purchase-order")]
        public async Task<IActionResult> CreatePurchaseOrder(int id, [FromBody] CreatePurchaseOrderFromRequestRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var poId = await _service.CreatePurchaseOrderAsync(id, request, userId!);
            return Ok(new { Message = "Tạo purchase order từ request thành công", PurchaseOrderId = poId });
        }
    }
}
