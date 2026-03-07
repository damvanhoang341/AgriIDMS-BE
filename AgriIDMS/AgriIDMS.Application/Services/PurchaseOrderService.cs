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

        var variantIds = request.Details.Select(d => d.ProductVariantId).Distinct().ToList();
        var variantsById = await _productVariantRepository.GetByIdsAsync(variantIds);
        if (variantsById.Count != variantIds.Count)
        {
            var missing = variantIds.FirstOrDefault(id => !variantsById.ContainsKey(id));
            throw new NotFoundException($"ProductVariant {missing} không tồn tại");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
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
                if (item.OrderedWeight <= 0)
                    throw new InvalidBusinessRuleException("Khối lượng đặt phải lớn hơn 0");

                order.Details.Add(new PurchaseOrderDetail
                {
                    ProductVariantId = item.ProductVariantId,
                    OrderedWeight = item.OrderedWeight,
                    UnitPrice = item.UnitPrice,
                    TolerancePercent = item.TolerancePercent,
                    ReceivedWeight = 0
                });
            }

            await _repository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("PurchaseOrder {OrderCode} created successfully", orderCode);
            return order.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
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
                Id = d.Id,
                ProductVariantId = d.ProductVariantId,
                ProductName = d.ProductVariant.Product.Name,
                OrderedWeight = d.OrderedWeight,
                UnitPrice = d.UnitPrice,
                TolerancePercent = d.TolerancePercent,
                ReceivedWeight = d.ReceivedWeight
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

        if (po.Details == null || !po.Details.Any())
            throw new InvalidBusinessRuleException("Đơn mua phải có ít nhất một dòng chi tiết mới được duyệt");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            po.Status = PurchaseOrderStatus.Approved;
            po.ApprovedBy = userId;
            po.ApprovedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(po);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("PurchaseOrder {Id} approved successfully", id);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(int id, UpdatePurchaseOrderRequest request, string userId)
    {
        _logger.LogInformation("User {UserId} updating PurchaseOrder {Id}", userId, id);

        if (request == null)
            throw new InvalidBusinessRuleException("Dữ liệu không hợp lệ");

        var po = await _repository.GetByIdAsync(id);
        if (po == null)
            throw new NotFoundException("Purchase Order không tồn tại");

        EnsureCanEdit(po);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (request.SupplierId.HasValue && request.SupplierId.Value != po.SupplierId)
            {
                var supplier = await _supplierRepository.GetSupplierByIdAsync(request.SupplierId.Value);
                if (supplier == null) throw new NotFoundException("Nhà cung cấp không tồn tại");
                po.SupplierId = request.SupplierId.Value;
            }

            if (request.Details != null)
            {
                if (!request.Details.Any())
                    throw new InvalidBusinessRuleException("Đơn mua phải có ít nhất một dòng chi tiết");

                var variantIds = request.Details.Select(d => d.ProductVariantId).Distinct().ToList();
                var variantsById = await _productVariantRepository.GetByIdsAsync(variantIds);
                if (variantsById.Count != variantIds.Count)
                {
                    var missing = variantIds.FirstOrDefault(vid => !variantsById.ContainsKey(vid));
                    throw new NotFoundException($"ProductVariant {missing} không tồn tại");
                }

                var requestDetailIds = request.Details
                    .Where(d => d.Id.HasValue && d.Id.Value > 0)
                    .Select(d => d.Id!.Value)
                    .ToHashSet();

                var toRemove = po.Details
                    .Where(d => d.ReceivedWeight == 0 && !requestDetailIds.Contains(d.Id))
                    .ToList();
                var newCount = request.Details.Count(d => !d.Id.HasValue || d.Id.Value == 0);
                if (po.Details.Count - toRemove.Count + newCount < 1)
                    throw new InvalidBusinessRuleException("Đơn mua phải có ít nhất một dòng chi tiết");
                _repository.RemoveDetails(toRemove);

                foreach (var item in request.Details)
                {
                    if (item.OrderedWeight <= 0)
                        throw new InvalidBusinessRuleException("Khối lượng đặt phải lớn hơn 0");

                    if (!item.Id.HasValue || item.Id.Value == 0)
                    {
                        po.Details.Add(new PurchaseOrderDetail
                        {
                            ProductVariantId = item.ProductVariantId,
                            OrderedWeight = item.OrderedWeight,
                            UnitPrice = item.UnitPrice,
                            TolerancePercent = item.TolerancePercent,
                            ReceivedWeight = 0
                        });
                    }
                    else
                    {
                        var existing = po.Details.FirstOrDefault(d => d.Id == item.Id.Value);
                        if (existing == null)
                            throw new NotFoundException($"Không tìm thấy dòng đơn mua Id={item.Id}");
                        if (existing.ReceivedWeight > 0)
                            throw new InvalidBusinessRuleException($"Không thể sửa dòng đã có nhập kho (Id={existing.Id})");
                        existing.ProductVariantId = item.ProductVariantId;
                        existing.OrderedWeight = item.OrderedWeight;
                        existing.UnitPrice = item.UnitPrice;
                        existing.TolerancePercent = item.TolerancePercent;
                    }
                }

                if (!po.Details.Any())
                    throw new InvalidBusinessRuleException("Đơn mua phải có ít nhất một dòng chi tiết");
            }

            await _repository.UpdateAsync(po);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("PurchaseOrder {Id} updated successfully", id);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting PurchaseOrder {Id}", id);

        var po = await _repository.GetByIdWithGoodsReceiptsAsync(id);
        if (po == null)
            throw new NotFoundException("Purchase Order không tồn tại");

        EnsureCanEdit(po);

        if (po.GoodsReceipts != null && po.GoodsReceipts.Any())
            throw new InvalidBusinessRuleException("Không thể xóa đơn mua đã có phiếu nhập kho. Chỉ xóa được đơn ở trạng thái Nháp và chưa có phiếu nhập.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _repository.DeleteAsync(po);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("PurchaseOrder {Id} deleted successfully", id);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>Gọi trước khi chỉnh sửa PO; ném nếu đã Approved (khóa sửa sau duyệt).</summary>
    private static void EnsureCanEdit(PurchaseOrder po)
    {
        if (po.Status == PurchaseOrderStatus.Approved)
            throw new InvalidBusinessRuleException("Không được chỉnh sửa đơn mua sau khi đã duyệt.");
    }
}