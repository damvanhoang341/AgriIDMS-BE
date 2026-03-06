using AgriIDMS.Application.DTOs.PurchaseOrder;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PurchaseOrderService> _logger;
    private readonly ISupplierService _supplierRepository;
    private readonly IProductVariantRepository _productVariantRepository;

    public PurchaseOrderService(
        IPurchaseOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PurchaseOrderService> logger,
        ISupplierService supplierRepository,
        IProductVariantRepository productVariantRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _supplierRepository = supplierRepository;
        _productVariantRepository = productVariantRepository;
    }

    public async Task<int> CreateAsync(CreatePurchaseOrderRequest request, string userId)
    {
        _logger.LogInformation("User {UserId} creating PurchaseOrder", userId);

        if (request == null)
            throw new InvalidBusinessRuleException("Dữ liệu không hợp lệ");

        if (request.Details == null || !request.Details.Any())
            throw new InvalidBusinessRuleException("Đơn hàng phải có ít nhất một sản phẩm");

        var supplier = await _supplierRepository.GetSupplierByIdAsync(request.SupplierId);
        if (supplier == null) throw new NotFoundException("Supplier không tồn tại");

        await _unitOfWork.BeginTransactionAsync();

        var orderCode = await _repository.GenerateOrderCodeAsync();

        var order = new PurchaseOrder
        {
            OrderCode = orderCode,
            SupplierId = request.SupplierId,
            CreatedBy = userId,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Pending
        };

        foreach (var item in request.Details)
        {
            var variant = await _productVariantRepository.GetProductVariantByIdAsync(item.ProductVariantId);
            if (variant == null)
                throw new NotFoundException($"ProductVariant {item.ProductVariantId} không tồn tại");
            if (item.OrderedWeight <= 0)
                throw new InvalidBusinessRuleException("Khối lượng đặt phải lớn hơn 0");

            order.Details.Add(new PurchaseOrderDetail
            {
                ProductVariantId = item.ProductVariantId,
                OrderedWeight = item.OrderedWeight,
                UnitPrice = item.UnitPrice
            });
        }

        await _repository.AddAsync(order);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.InnerException?.Message);
            throw new Exception(ex.InnerException?.Message);
        }
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("PurchaseOrder {OrderCode} created successfully", orderCode);

        return order.Id;
    }

    public async Task<PurchaseOrderResponse> GetByIdAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Purchase Order không tồn tại");

        return new PurchaseOrderResponse
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            SupplierId = order.SupplierId,
            SupplierName = order.Supplier.Name,
            Status = order.Status.ToString(),
            OrderDate = order.OrderDate,
            Details = order.Details.Select(d => new PurchaseOrderDetailResponse
            {
                ProductVariantId = d.ProductVariantId,
                ProductName = d.ProductVariant.Product.Name,
                OrderedWeight = d.OrderedWeight,
                UnitPrice = d.UnitPrice
            }).ToList()
        };
    }

    public async Task ApprovePurchaseOrderAsync(int id, string userId)
    {
        _logger.LogInformation("User {UserId} approving PurchaseOrder {Id}", userId, id);

        var po = await _repository.GetByIdAsync(id);

        if (po == null)
            throw new NotFoundException("Purchase Order không tồn tại");

        if (po.Status != PurchaseOrderStatus.Pending)
            throw new InvalidBusinessRuleException("Chỉ có thể duyệt đơn hàng ở trạng thái Pending");

        po.Status = PurchaseOrderStatus.Approved;
        po.ApprovedBy = userId;
        po.ApprovedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(po);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("PurchaseOrder {Id} approved successfully", id);
    }
}