using AgriIDMS.Application.DTOs.PurchaseOrder;
using AgriIDMS.Application.DTOs.PurchaseRequest;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;

namespace AgriIDMS.Application.Services
{
    public class PurchaseRequestService : IPurchaseRequestService
    {
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IUnitOfWork _unitOfWork;

        public PurchaseRequestService(
            IPurchaseRequestRepository purchaseRequestRepository,
            IProductRepository productRepository,
            IPurchaseOrderService purchaseOrderService,
            IUnitOfWork unitOfWork)
        {
            _purchaseRequestRepository = purchaseRequestRepository;
            _productRepository = productRepository;
            _purchaseOrderService = purchaseOrderService;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateAsync(CreatePurchaseRequestRequest request, string userId)
        {
            if (request.Details == null || request.Details.Count == 0)
                throw new InvalidBusinessRuleException("Phiếu đề xuất mua phải có ít nhất 1 dòng.");

            var duplicateProductIds = request.Details
                .GroupBy(d => d.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateProductIds.Count > 0)
                throw new InvalidBusinessRuleException("Không được trùng sản phẩm trong cùng phiếu đề xuất mua.");

            foreach (var detail in request.Details)
            {
                var product = await _productRepository.GetProductByIdAsync(detail.ProductId);
                if (product == null)
                    throw new NotFoundException($"Sản phẩm #{detail.ProductId} không tồn tại");
            }

            var requestCode = await _purchaseRequestRepository.GenerateRequestCodeAsync();
            var entity = new PurchaseRequest
            {
                RequestCode = requestCode,
                CreatedBy = userId,
                RequestedDate = DateTime.UtcNow,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Status = PurchaseRequestStatus.Draft,
                Details = request.Details.Select(d => new PurchaseRequestDetail
                {
                    ProductId = d.ProductId,
                    RequestedWeight = d.RequestedWeight,
                    AllocatedWeight = 0,
                    TargetUnitPrice = d.TargetUnitPrice
                }).ToList()
            };

            await _purchaseRequestRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<IEnumerable<PurchaseRequestResponse>> GetAllAsync()
        {
            var data = await _purchaseRequestRepository.GetAllAsync();
            return data.Select(MapResponse);
        }

        public async Task<PurchaseRequestResponse> GetByIdAsync(int id)
        {
            var entity = await _purchaseRequestRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException("Phiếu đề xuất mua không tồn tại");
            return MapResponse(entity);
        }

        public async Task<int> CreatePurchaseOrderAsync(int requestId, CreatePurchaseOrderFromRequestRequest request, string userId)
        {
            var entity = await _purchaseRequestRepository.GetByIdAsync(requestId);
            if (entity == null)
                throw new NotFoundException("Phiếu đề xuất mua không tồn tại");
            if (entity.Status == PurchaseRequestStatus.Closed)
                throw new InvalidBusinessRuleException("Phiếu đề xuất mua đã đóng.");
            if (request.Details == null || request.Details.Count == 0)
                throw new InvalidBusinessRuleException("Thiếu dòng phân bổ tạo PO.");

            var mapDetails = entity.Details.ToDictionary(x => x.Id);
            var poDetails = new List<CreatePurchaseOrderDetailRequest>();
            foreach (var d in request.Details)
            {
                if (!mapDetails.TryGetValue(d.PurchaseRequestDetailId, out var reqDetail))
                    throw new NotFoundException($"Chi tiết phiếu đề xuất mua #{d.PurchaseRequestDetailId} không tồn tại");
                if (d.OrderedWeight > reqDetail.RemainingWeight + 0.0001m)
                    throw new InvalidBusinessRuleException($"Dòng yêu cầu {reqDetail.Id} vượt khối lượng còn lại.");

                reqDetail.AllocatedWeight += d.OrderedWeight;
                poDetails.Add(new CreatePurchaseOrderDetailRequest
                {
                    ProductId = reqDetail.ProductId,
                    OrderedWeight = d.OrderedWeight,
                    UnitPrice = d.UnitPrice,
                    TolerancePercent = d.TolerancePercent,
                    HarvestDate = d.HarvestDate
                });
            }

            var poId = await _purchaseOrderService.CreateAsync(new CreatePurchaseOrderRequest
            {
                SupplierId = request.SupplierId,
                Details = poDetails
            }, userId);

            var allAllocated = entity.Details.All(x => x.RemainingWeight <= 0.0001m);
            var anyAllocated = entity.Details.Any(x => x.AllocatedWeight > 0);
            entity.Status = allAllocated
                ? PurchaseRequestStatus.FullyAllocated
                : anyAllocated
                    ? PurchaseRequestStatus.PartiallyAllocated
                    : PurchaseRequestStatus.Draft;

            await _unitOfWork.SaveChangesAsync();
            return poId;
        }

        private static PurchaseRequestResponse MapResponse(PurchaseRequest entity)
        {
            return new PurchaseRequestResponse
            {
                Id = entity.Id,
                RequestCode = entity.RequestCode,
                Status = entity.Status.ToString(),
                RequestedDate = entity.RequestedDate,
                Notes = entity.Notes,
                Details = entity.Details.Select(d => new PurchaseRequestDetailResponse
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? string.Empty,
                    RequestedWeight = d.RequestedWeight,
                    AllocatedWeight = d.AllocatedWeight,
                    RemainingWeight = d.RemainingWeight,
                    TargetUnitPrice = d.TargetUnitPrice
                }).ToList()
            };
        }
    }
}
