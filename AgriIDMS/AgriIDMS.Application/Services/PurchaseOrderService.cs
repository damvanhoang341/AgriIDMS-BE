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
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;

    public PurchaseOrderService(
        IPurchaseOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PurchaseOrderService> logger,
        ISupplierService supplierRepository,
        IProductRepository productRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;

    }

    public async Task<int> CreateAsync(CreatePurchaseOrderRequest request, string userId)
    {
        _logger.LogInformation("User {UserId} creating PurchaseOrder", userId);

        if (request == null)
            throw new InvalidBusinessRuleException("Dữ liệu không hợp lệ");

        if (request.Details == null || !request.Details.Any())
            throw new InvalidBusinessRuleException("Đơn hàng phải có ít nhất một sản phẩm");

        // Không cho phép trùng ProductId trong cùng một đơn mua
        var duplicateProductIds = request.Details
            .GroupBy(d => d.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProductIds.Any())
            throw new InvalidBusinessRuleException("Không được tạo nhiều dòng cho cùng một sản phẩm trong một đơn mua");

        var supplier = await _supplierRepository.GetSupplierByIdAsync(request.SupplierId);
        if (supplier == null) throw new NotFoundException("Supplier không tồn tại");

        var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
                throw new NotFoundException($"Product {productId} không tồn tại");
        }

        int createdOrderId = 0;
        string? logOrderCode = null;
        await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
        {
            var orderCode = await _repository.GenerateOrderCodeAsync();
            logOrderCode = orderCode;

            var order = new PurchaseOrder
            {
                OrderCode = orderCode,
                SupplierId = request.SupplierId,
                ProcurementMode = ProcurementMode.LegacySingleSupplier,
                CreatedBy = userId,
                OrderDate = DateTime.UtcNow,
                Status = PurchaseOrderStatus.Pending
            };

            foreach (var item in request.Details)
            {
                if (item.OrderedWeight <= 0)
                    throw new InvalidBusinessRuleException("Khối lượng đặt phải lớn hơn 0");

                if (item.HarvestDate == default)
                    throw new InvalidBusinessRuleException("HarvestDate phải được cung cấp cho từng dòng đơn mua");
                if (item.HarvestDate > DateTime.UtcNow)
                    throw new InvalidBusinessRuleException("Ngày thu hoạch không được ở tương lai");
                if (item.HarvestDate < DateTime.UtcNow.AddDays(-7))
                {
                    throw new InvalidBusinessRuleException(
                        $"Sản phẩm chỉ cho phép thu hoạch trong 7 ngày gần đây"
                    );
                }
                order.Details.Add(new PurchaseOrderDetail
                {
                    ProductId = item.ProductId,
                    OrderedWeight = item.OrderedWeight,
                    UnitPrice = item.UnitPrice,
                    TolerancePercent = item.TolerancePercent,
                    ReceivedWeight = 0,
                    HarvestDate = item.HarvestDate
                });
            }

            await _repository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            createdOrderId = order.Id;
        });

        _logger.LogInformation("PurchaseOrder {OrderCode} created successfully", logOrderCode);
        return createdOrderId;
    }

    public async Task<int> CreateMultiSupplierAsync(CreateMultiSupplierPurchaseOrderRequest request, string userId)
    {
        _logger.LogInformation("User {UserId} creating multi-supplier PurchaseOrder", userId);

        if (request == null || request.SupplierPlans == null || request.SupplierPlans.Count == 0)
            throw new InvalidBusinessRuleException("Đơn mua đa nhà cung cấp phải có ít nhất 1 kế hoạch nhà cung cấp");

        int createdOrderId = 0;
        await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
        {
            var orderCode = await _repository.GenerateOrderCodeAsync();
            var firstSupplierId = request.SupplierPlans[0].SupplierId;

            var order = new PurchaseOrder
            {
                OrderCode = orderCode,
                SupplierId = firstSupplierId,
                ProcurementMode = ProcurementMode.MultiSupplierStrictReceipt,
                CreatedBy = userId,
                OrderDate = DateTime.UtcNow,
                Status = PurchaseOrderStatus.Pending
            };

            foreach (var supplierPlanRequest in request.SupplierPlans)
            {
                var supplier = await _supplierRepository.GetSupplierByIdAsync(supplierPlanRequest.SupplierId);
                if (supplier == null)
                    throw new NotFoundException($"Nhà cung cấp #{supplierPlanRequest.SupplierId} không tồn tại");

                if (supplierPlanRequest.Details == null || supplierPlanRequest.Details.Count == 0)
                    throw new InvalidBusinessRuleException($"Kế hoạch của nhà cung cấp #{supplierPlanRequest.SupplierId} phải có ít nhất 1 dòng");

                var supplierPlan = new PurchaseOrderSupplierPlan
                {
                    SupplierId = supplierPlanRequest.SupplierId,
                    OrderDate = supplierPlanRequest.OrderDate,
                    Notes = string.IsNullOrWhiteSpace(supplierPlanRequest.Notes) ? null : supplierPlanRequest.Notes.Trim()
                };

                foreach (var detailRequest in supplierPlanRequest.Details)
                {
                    var product = await _productRepository.GetProductByIdAsync(detailRequest.ProductId);
                    if (product == null)
                        throw new NotFoundException($"Sản phẩm #{detailRequest.ProductId} không tồn tại");

                    if (detailRequest.PriceDate.Date > supplierPlanRequest.OrderDate.Date)
                        throw new InvalidBusinessRuleException("Ngày áp giá không được sau ngày đặt của kế hoạch nhà cung cấp");

                    var supplierPlanDetail = new PurchaseOrderSupplierPlanDetail
                    {
                        ProductId = detailRequest.ProductId,
                        OrderedWeight = detailRequest.OrderedWeight,
                        UnitPriceAtOrder = detailRequest.UnitPriceAtOrder,
                        PriceDate = detailRequest.PriceDate.Date,
                        TolerancePercent = detailRequest.TolerancePercent
                    };
                    supplierPlan.Details.Add(supplierPlanDetail);

                    order.Details.Add(new PurchaseOrderDetail
                    {
                        ProductId = detailRequest.ProductId,
                        OrderedWeight = detailRequest.OrderedWeight,
                        UnitPrice = detailRequest.UnitPriceAtOrder,
                        TolerancePercent = detailRequest.TolerancePercent,
                        ReceivedWeight = 0,
                        HarvestDate = supplierPlanRequest.OrderDate.Date,
                        SupplierPlanDetail = supplierPlanDetail
                    });
                }

                order.SupplierPlans.Add(supplierPlan);
            }

            await _repository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            createdOrderId = order.Id;
        });

        return createdOrderId;
    }

    public async Task<PurchaseOrderResponse> GetByIdAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Purchase Order không tồn tại");

        var creator = await _userRepository.GetByIdAsync(order.CreatedBy);
        string? approverName = null;
        if (!string.IsNullOrWhiteSpace(order.ApprovedBy))
        {
            var approver = await _userRepository.GetByIdAsync(order.ApprovedBy);
            approverName = approver?.FullName;
        }

        return new PurchaseOrderResponse
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            SupplierId = order.SupplierId,
            SupplierName = order.Supplier.Name,
            Status = order.Status.ToString(),
            ProcurementMode = order.ProcurementMode.ToString(),
            OrderDate = order.OrderDate,
            NameCreater = creator?.FullName ?? "Không xác định",
            Details = order.Details.Select(d => new PurchaseOrderDetailResponse
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product.Name,
                OrderedWeight = d.OrderedWeight,
                UnitPrice = d.UnitPrice,
                TolerancePercent = d.TolerancePercent,
                ReceivedWeight = d.ReceivedWeight,
                HarvestDate = d.HarvestDate,
                NameApprover = approverName
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

        await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
        {
            po.Status = PurchaseOrderStatus.Approved;
            po.ApprovedBy = userId;
            po.ApprovedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(po);
        });
        _logger.LogInformation("PurchaseOrder {Id} approved successfully", id);
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

        await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
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

                // Không cho phép trùng ProductId trong cùng một đơn mua
                var duplicateProductIds = request.Details
                    .GroupBy(d => d.ProductId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if (duplicateProductIds.Any())
                    throw new InvalidBusinessRuleException("Không được tạo nhiều dòng cho cùng một sản phẩm trong một đơn mua");

                var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
                foreach (var productId in productIds)
                {
                    var product = await _productRepository.GetProductByIdAsync(productId);
                    if (product == null)
                        throw new NotFoundException($"Product {productId} không tồn tại");
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

                    if (item.HarvestDate == default)
                        throw new InvalidBusinessRuleException("HarvestDate phải được cung cấp cho từng dòng đơn mua");

                    if (!item.Id.HasValue || item.Id.Value == 0)
                    {
                        po.Details.Add(new PurchaseOrderDetail
                        {
                            ProductId = item.ProductId,
                            OrderedWeight = item.OrderedWeight,
                            UnitPrice = item.UnitPrice,
                            TolerancePercent = item.TolerancePercent,
                            ReceivedWeight = 0,
                            HarvestDate = item.HarvestDate
                        });
                    }
                    else
                    {
                        var existing = po.Details.FirstOrDefault(d => d.Id == item.Id.Value);
                        if (existing == null)
                            throw new NotFoundException($"Không tìm thấy dòng đơn mua Id={item.Id}");
                        if (existing.ReceivedWeight > 0)
                            throw new InvalidBusinessRuleException($"Không thể sửa dòng đã có nhập kho (Id={existing.Id})");
                        existing.ProductId = item.ProductId;
                        existing.OrderedWeight = item.OrderedWeight;
                        existing.UnitPrice = item.UnitPrice;
                        existing.TolerancePercent = item.TolerancePercent;
                        existing.HarvestDate = item.HarvestDate;
                    }
                }

                if (!po.Details.Any())
                    throw new InvalidBusinessRuleException("Đơn mua phải có ít nhất một dòng chi tiết");
            }

            await _repository.UpdateAsync(po);
        });
        _logger.LogInformation("PurchaseOrder {Id} updated successfully", id);
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

        await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
        {
            await _repository.DeleteAsync(po);
        });
        _logger.LogInformation("PurchaseOrder {Id} deleted successfully", id);
    }

    /// <summary>Gọi trước khi chỉnh sửa PO; ném nếu đã Approved (khóa sửa sau duyệt).</summary>
    private static void EnsureCanEdit(PurchaseOrder po)
    {
        if (po.Status == PurchaseOrderStatus.Approved)
            throw new InvalidBusinessRuleException("Không được chỉnh sửa đơn mua sau khi đã duyệt.");
    }

    public async Task<IEnumerable<PurchaseOrderGetAllResponse>> GetAllAsync()
    {
        var orders = await _repository.GetAllAsync();

        var result = new List<PurchaseOrderGetAllResponse>();

        foreach (var order in orders)
        {
            const decimal EPS = 0.0001m;
            // Hide PO that has no remaining weight to receive (all details fully received).
            var hasRemaining =
                order.Details != null &&
                order.Details.Any(d => d.OrderedWeight > d.ReceivedWeight + EPS);
            if (!hasRemaining)
                continue;

            var peopleCreate = await _userRepository.GetByIdAsync(order.CreatedBy);

            result.Add(new PurchaseOrderGetAllResponse
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier.Name,
                Status = order.Status.ToString(),
                ProcurementMode = order.ProcurementMode.ToString(),
                OrderDate = order.OrderDate,
                NameCreater = peopleCreate?.FullName ?? "Không xác định"
            });
        }

        return result;
    }
}