using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class GoodsReceiptService : IGoodsReceiptService
    {
        private static readonly JsonSerializerOptions PrintJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly IGoodsReceiptRepository _receiptRepo;
        private readonly IGoodsReceiptDetailRepository _detailRepo;
        private readonly IGoodsReceiptDetailService _detailService;
        private readonly ILotRepository _lotRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IProductVariantRepository _productVariantRepo;
        private readonly ILogger<GoodsReceiptService> _logger;
        private readonly IPurchaseOrderRepository _purchaseOrderRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly INotificationService _notificationService;
        private const decimal CapacityTolerance = 0.0001m;
        private const decimal MaxSlotUtilizationRatio = 0.8m;
        private const decimal OperationalBufferRatio = 0.8m;

        public GoodsReceiptService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            IGoodsReceiptDetailService detailService,
            ILotRepository lotRepo,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepo,
            IProductVariantRepository productVariantRepository,
            ILogger<GoodsReceiptService> logger,
            IPurchaseOrderRepository purchaseOrderRepo,
            IBoxRepository boxRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            INotificationService notificationService)
        {
            _receiptRepo = receiptRepo;
            _detailRepo = detailRepo;
            _detailService = detailService;
            _lotRepo = lotRepo;
            _unitOfWork = unitOfWork;
            _supplierRepo = supplierRepository;
            _warehouseRepo = warehouseRepo;
            _productVariantRepo = productVariantRepository;
            _logger = logger;
            _purchaseOrderRepo = purchaseOrderRepo;
            _boxRepo = boxRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _notificationService = notificationService;
        }

        // ===============================
        // CREATE GOODS RECEIPT (3.5: PurchaseOrderId bắt buộc, validate PO tồn tại, đã duyệt, cùng NCC)
        // ===============================
        public async Task<int> CreateGoodsReceiptAsync(
            CreateGoodsReceiptRequest request,
            string userId,
            bool autoApproveWhenCreatedByManager = false)
        {
            _logger.LogInformation("User {UserId} tạo phiếu nhập kho", userId);
            var receiptIdResult = 0;
            var notifyPendingManager = false;
            var shouldApplyPrivilegedFirstApproval = false;
            await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
            {
                var po = await _purchaseOrderRepo.GetByIdAsync(request.PurchaseOrderId);
                if (po == null)
                    throw new NotFoundException("Đơn mua không tồn tại");
                if (po.Status != PurchaseOrderStatus.Approved)
                    throw new InvalidBusinessRuleException("Chỉ được tạo phiếu nhập theo đơn mua đã duyệt");

                var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
                if (warehouse == null)
                    throw new NotFoundException("Kho không tồn tại");

                var receiptSupplierId = po.SupplierId;
                if (po.ProcurementMode == ProcurementMode.MultiSupplierStrictReceipt)
                {
                    if (request.Details == null || request.Details.Count == 0)
                        throw new InvalidBusinessRuleException(
                            "Đơn mua đa nhà cung cấp yêu cầu nhập đầy đủ tất cả dòng hàng.");

                    var expectedSupplierPlanDetailIds = po.SupplierPlans
                        .SelectMany(p => p.Details)
                        .Select(d => d.Id)
                        .Distinct()
                        .ToHashSet();
                    if (expectedSupplierPlanDetailIds.Count == 0)
                        throw new InvalidBusinessRuleException("Đơn mua đa nhà cung cấp chưa có dòng kế hoạch hợp lệ.");

                    var seenSupplierPlanDetailIds = new HashSet<int>();
                    foreach (var line in request.Details)
                    {
                        if (!line.SupplierPlanDetailId.HasValue)
                            throw new InvalidBusinessRuleException("Đơn mua đa nhà cung cấp yêu cầu nhập đầy đủ tất cả dòng hàng.");
                        if (!seenSupplierPlanDetailIds.Add(line.SupplierPlanDetailId.Value))
                            throw new InvalidBusinessRuleException("Không được nhập trùng dòng kế hoạch nhà cung cấp.");

                        var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(line.PurchaseOrderDetailId)
                            ?? throw new NotFoundException($"Không tìm thấy dòng đơn mua #{line.PurchaseOrderDetailId}");
                        if (poDetail.PurchaseOrderId != po.Id)
                            throw new InvalidBusinessRuleException("Dòng nhập không thuộc đơn mua đã chọn.");

                        if (!poDetail.SupplierPlanDetailId.HasValue)
                            throw new InvalidBusinessRuleException(
                                $"Dòng đơn mua #{poDetail.Id} chưa có nguồn kế hoạch nhà cung cấp.");

                        if (poDetail.SupplierPlanDetailId.Value != line.SupplierPlanDetailId.Value)
                            throw new InvalidBusinessRuleException(
                                $"Chi tiết nhập của dòng đơn mua #{poDetail.Id} không khớp SupplierPlanDetail.");
                        if (!expectedSupplierPlanDetailIds.Contains(line.SupplierPlanDetailId.Value))
                            throw new InvalidBusinessRuleException("Dòng nhập không thuộc đơn mua đã chọn.");
                        if (Math.Abs(line.ReceivedWeight - poDetail.OrderedWeight) > 0.0001m)
                            throw new InvalidBusinessRuleException("Khối lượng nhận phải bằng khối lượng đặt mua.");
                    }

                    if (seenSupplierPlanDetailIds.Count != expectedSupplierPlanDetailIds.Count ||
                        expectedSupplierPlanDetailIds.Except(seenSupplierPlanDetailIds).Any())
                    {
                        throw new InvalidBusinessRuleException("Đơn mua đa nhà cung cấp yêu cầu nhập đầy đủ tất cả dòng hàng.");
                    }
                }

                var receipt = new GoodsReceipt
                {
                    ReceiptCode = await _receiptRepo.GenerateReceiptCodeAsync(),
                    PurchaseOrderId = request.PurchaseOrderId,
                    InboundReceiptKind = InboundReceiptKind.FromPurchaseOrder,
                    SupplierId = receiptSupplierId,
                    WarehouseId = request.WarehouseId,
                    VehicleNumber = request.VehicleNumber,
                    DriverName = request.DriverName,
                    TransportCompany = request.TransportCompany,
                    CreatedBy = userId,
                    ReceivedBy = userId,
                    ReceivedDate = DateTime.UtcNow,
                    Status = GoodsReceiptStatus.Draft
                };

                await _receiptRepo.AddGoodsReceiptAsync(receipt);
                await _unitOfWork.SaveChangesAsync();

                // Nếu request có kèm danh sách chi tiết thì thêm luôn các dòng detail vào phiếu vừa tạo
                if (request.Details != null && request.Details.Count > 0)
                {
                    foreach (var line in request.Details)
                    {
                        var addDetailRequest = new AddGoodsReceiptDetailRequest
                        {
                            GoodsReceiptId = receipt.Id,
                            PurchaseOrderDetailId = line.PurchaseOrderDetailId,
                            SupplierPlanDetailId = line.SupplierPlanDetailId,
                            ReceivedWeight = line.ReceivedWeight
                        };

                        await _detailService.AddGoodsReceiptDetailAsync(addDetailRequest);
                    }

                    await EnsureWarehouseCapacityAtCreateAsync(request.WarehouseId, request.Details);
                }

                // Không gọi auto approve ngay trong transaction hiện tại để tránh nested transaction.
                shouldApplyPrivilegedFirstApproval = autoApproveWhenCreatedByManager;

                receiptIdResult = receipt.Id;
                notifyPendingManager = !autoApproveWhenCreatedByManager;
            });

            if (shouldApplyPrivilegedFirstApproval && receiptIdResult > 0)
                await AutoApproveCreatedReceiptByManagerAsync(receiptIdResult, userId);

            if (notifyPendingManager)
                await _notificationService.NotifyGoodsReceiptPendingManagerAsync(receiptIdResult);
            return receiptIdResult;
        }


        // ===============================
        // QC INSPECTION (ghi nhận inspected/damaged + phân loại ProductVariant sau QC)
        // ===============================
        public async Task QCInspectionAsync(QCInspectionRequest request, string userId, bool autoApproveWhenEligible = false)
        {
            var detail = await _detailRepo.GetByIdAsync(request.DetailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết phiếu nhập không tồn tại");

            // Không cho QC khi phiếu đang chờ Manager duyệt (PendingManagerApproval / PendingManagerApprovalQc)
            var parentReceipt = await _receiptRepo.GetGoodsReceiptByIdAsync(detail.GoodsReceiptId);
            if (parentReceipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (parentReceipt.Status != GoodsReceiptStatus.Received)
                throw new InvalidBusinessRuleException("Chỉ được kiểm tra chất lượng sau khi phiếu đã được duyệt bước 1 (trạng thái Đã nhận).");
            if (parentReceipt.Status == GoodsReceiptStatus.PendingManagerApproval ||
                parentReceipt.Status == GoodsReceiptStatus.PendingManagerApprovalQc)
                throw new InvalidBusinessRuleException("Phiếu nhập đang chờ Manager duyệt, không được QC. Vui lòng đợi Manager xử lý.");

            if (request.InspectedWeight <= 0)
                throw new InvalidBusinessRuleException("InspectedWeight phải lớn hơn 0.");
            if (request.InspectedWeight > detail.ReceivedWeight)
                throw new InvalidBusinessRuleException("InspectedWeight không được vượt quá ReceivedWeight của dòng.");
            if (request.DamagedWeight < 0)
                throw new InvalidBusinessRuleException("DamagedWeight không được âm.");
            if (request.DamagedWeight > request.InspectedWeight)
                throw new InvalidBusinessRuleException("DamagedWeight không được vượt quá InspectedWeight.");
            if (request.ClassificationDetails == null || request.ClassificationDetails.Count == 0)
                throw new InvalidBusinessRuleException("Phải có ít nhất 1 classification detail.");

            var duplicateVariantIds = request.ClassificationDetails
                .GroupBy(x => x.ProductVariantId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateVariantIds.Count > 0)
                throw new InvalidBusinessRuleException("Không được trùng ProductVariant trong classification details.");

            var totalClassified = request.ClassificationDetails.Sum(x => x.Quantity);
            var passedWeight = request.InspectedWeight - request.DamagedWeight;
            if (Math.Abs(totalClassified - passedWeight) > 0.0001m)
                throw new InvalidBusinessRuleException("Tổng phân loại + hỏng phải khớp với tổng kiểm.");

            var variantIds = request.ClassificationDetails.Select(x => x.ProductVariantId).Distinct().ToList();
            var variantsById = await _productVariantRepo.GetByIdsAsync(variantIds);
            if (variantsById.Count != variantIds.Count)
            {
                var missing = variantIds.FirstOrDefault(id => !variantsById.ContainsKey(id));
                throw new NotFoundException($"ProductVariant {missing} không tồn tại");
            }

            // Tính QCResult theo tolerance dựa trên phần hỏng
            var poDetail = detail.PurchaseOrderDetail ?? await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId)
                ?? throw new NotFoundException("Chi tiết đơn mua không tồn tại");

            decimal allowedLoss = poDetail.OrderedWeight * poDetail.TolerancePercent / 100m;
            var qcResult = request.DamagedWeight > allowedLoss ? QCResult.Failed : QCResult.Passed;

            if (detail.QcRecord == null)
                detail.QcRecord = new QcRecord { GoodsReceiptDetail = detail };

            detail.QcRecord.InspectedWeight = request.InspectedWeight;
            detail.QcRecord.DamagedWeight = request.DamagedWeight;
            detail.QcRecord.PassedWeight = passedWeight;
            detail.QcRecord.QCResult = qcResult;
            detail.QcRecord.QCNote = $"Damaged {request.DamagedWeight:N2} kg / Passed {passedWeight:N2} kg";
            detail.QcRecord.InspectedBy = userId;
            detail.QcRecord.InspectedAt = DateTime.UtcNow;
            detail.QcRecord.ClassificationDetails.Clear();
            foreach (var c in request.ClassificationDetails)
            {
                detail.QcRecord.ClassificationDetails.Add(new QcClassificationDetail
                {
                    ProductVariantId = c.ProductVariantId,
                    Quantity = c.Quantity
                });
            }

            // Backward compatibility: lưu variant đầu tiên để các luồng cũ vẫn đọc được.
            detail.ProductVariantId = request.ClassificationDetails
                .OrderByDescending(x => x.Quantity)
                .First().ProductVariantId;

            await _unitOfWork.SaveChangesAsync();

            // Sau khi QC xong 1 dòng: nếu tất cả dòng đã QC, kiểm tra dung sai + định mức tối thiểu để quyết định QCCompleted hay PendingManagerApproval
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(detail.GoodsReceiptId);
            if (receipt == null) return;

            // Nếu vẫn còn dòng chưa QC thì chưa kết luận
            if (receipt.Details.Any(d => d.QCResult == QCResult.Pending))
                return;

            bool toleranceExceeded = CheckToleranceExceeded(receipt);
            string? minReceiptWarning = await TryGetMinReceiptWeightWarningAsync(receipt);

            if (toleranceExceeded || minReceiptWarning != null)
            {
                receipt.Status = GoodsReceiptStatus.PendingManagerApproval;
                var reason = "";
                if (toleranceExceeded)
                    reason += "Vượt dung sai cho phép cho ít nhất một dòng PO. ";
                if (minReceiptWarning != null)
                    reason += minReceiptWarning;

                receipt.PendingReason = reason;
                await _unitOfWork.SaveChangesAsync();
                await _notificationService.NotifyGoodsReceiptPendingManagerAsync(receipt.Id);
                await PersistPrintSnapshotAfterQcAsync(receipt.Id);
            }
            else
            {
                // Tất cả dòng đã QC và nằm trong dung sai + đạt định mức tối thiểu
                receipt.Status = GoodsReceiptStatus.QCCompleted;
                receipt.PendingReason = null;
                await _unitOfWork.SaveChangesAsync();
                await _notificationService.NotifyGoodsReceiptPendingManagerAsync(receipt.Id);
                await PersistPrintSnapshotAfterQcAsync(receipt.Id);

                // Admin/Manager: tự duyệt bước 2 ngay khi QC hoàn tất và đủ điều kiện.
                if (autoApproveWhenEligible)
                {
                    await ApproveGoodsReceiptAsync(receipt.Id, userId);
                }
            }
        }

        // ===============================
        // APPROVE RECEIPT (sau khi QC xong & đã xử lý dung sai/định mức; tạo Lots; không còn check dung sai ở đây)
        // ===============================
        public async Task ApproveGoodsReceiptAsync(int receiptId, string userId)
        {
            await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
            {
                var receipt = await _receiptRepo.GetGoodsReceiptForApproveAsync(receiptId);
                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");
                // Duyệt bước 1: cho phép vào QC (Draft -> Received).
                if (receipt.Status == GoodsReceiptStatus.Draft)
                {
                    receipt.Status = GoodsReceiptStatus.Received;
                    await _unitOfWork.SaveChangesAsync();
                    return;
                }
                // Sau khi đổi luồng: chỉ cho phép duyệt khi đã QCCompleted hoặc đang PendingManagerApproval (đã được Manager xem xét dung sai trước đó).
                if (receipt.Status != GoodsReceiptStatus.QCCompleted && receipt.Status != GoodsReceiptStatus.PendingManagerApproval)
                    throw new InvalidBusinessRuleException("Chỉ được duyệt phiếu nhập ở trạng thái Đã QC (QCCompleted) hoặc Đang chờ duyệt (PendingManagerApproval)");

                if (!receipt.Details.Any())
                    throw new InvalidBusinessRuleException("Phiếu nhập chưa có chi tiết");

                foreach (var d in receipt.Details)
                {
                    if (d.QCResult == QCResult.Pending)
                        throw new InvalidBusinessRuleException("Có sản phẩm chưa QC");
                }

                await EnsureWarehouseCapacityAsync(receipt);

                await CreateLotsAndSetApprovedAsync(receipt, userId);
            });

            var afterApprove = await _receiptRepo.GetGoodsReceiptByIdAsync(receiptId);
            if (afterApprove?.Status == GoodsReceiptStatus.Approved)
                await _notificationService.NotifyWarehouseStaffGoodsReceiptApprovedAsync(receiptId);

            _logger.LogInformation("Receipt {ReceiptId} đã được approve bởi {UserId}", receiptId, userId);
        }

        // ===============================
        // MANAGER REVIEW MIN WEIGHT (Approve / Reject khi status = PendingManagerApprovalQc - dưới định mức tối thiểu)
        // ===============================
        public async Task ManagerReviewMinWeightAsync(int receiptId, bool isApproved, string userId)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(receiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.Status != GoodsReceiptStatus.PendingManagerApprovalQc)
                throw new InvalidBusinessRuleException("Chỉ xử lý phiếu đang chờ duyệt định mức tối thiểu (PendingManagerApprovalQc)");

            if (isApproved)
            {
                receipt.Status = GoodsReceiptStatus.Received;
                receipt.PendingReason = null;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Receipt {ReceiptId} được Manager cho phép tiếp tục flow QC/Approve (ngoại lệ định mức tối thiểu) bởi {UserId}", receiptId, userId);
            }
            else
            {
                receipt.Status = GoodsReceiptStatus.Rejected;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Receipt {ReceiptId} đã bị Manager từ chối do dưới định mức tối thiểu bởi {UserId}", receiptId, userId);
            }
        }

        public async Task ManagerAllowQcAsync(int receiptId, string userId)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(receiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");

            if (receipt.Status != GoodsReceiptStatus.PendingManagerApproval &&
                receipt.Status != GoodsReceiptStatus.PendingManagerApprovalQc)
            {
                throw new InvalidBusinessRuleException(
                    "Chỉ xử lý phiếu đang chờ duyệt dung sai hoặc định mức tối thiểu (PendingManagerApproval/PendingManagerApprovalQc)");
            }

            receipt.Status = GoodsReceiptStatus.Received;
            receipt.PendingReason = null;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Receipt {ReceiptId} được Manager cho phép mở lại QC bởi {UserId}",
                receiptId,
                userId);
        }

        // ===============================
        // MANAGER REVIEW TOLERANCE (Approve / Reject khi status = PendingManagerApproval - vượt dung sai)
        // ===============================
        public async Task ManagerReviewToleranceAsync(int receiptId, bool isApproved, string userId)
        {
            await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
            {
                var receipt = await _receiptRepo.GetGoodsReceiptForApproveAsync(receiptId);
                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");
                if (receipt.Status != GoodsReceiptStatus.PendingManagerApproval)
                    throw new InvalidBusinessRuleException("Chỉ xử lý phiếu đang chờ duyệt do vượt dung sai (PendingManagerApproval)");

                if (isApproved)
                {
                    await EnsureWarehouseCapacityAsync(receipt);
                    await CreateLotsAndSetApprovedAsync(receipt, userId);
                    _logger.LogInformation("Receipt {ReceiptId} đã được Manager approve (vượt dung sai) bởi {UserId}", receiptId, userId);
                }
                else
                {
                    receipt.Status = GoodsReceiptStatus.Rejected;
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Receipt {ReceiptId} đã bị Manager từ chối (vượt dung sai) bởi {UserId}", receiptId, userId);
                }
            });

            var afterTolerance = await _receiptRepo.GetGoodsReceiptByIdAsync(receiptId);
            if (afterTolerance?.Status == GoodsReceiptStatus.Approved)
                await _notificationService.NotifyWarehouseStaffGoodsReceiptApprovedAsync(receiptId);
        }

        // ===============================
        // UPDATE WAREHOUSE (chỉ khi phiếu chưa Approved – chưa tạo Lot/Box)
        // ===============================
        public async Task UpdateWarehouseAsync(int receiptId, UpdateGoodsReceiptWarehouseRequest request, string userId)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(receiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");

            if (receipt.Status == GoodsReceiptStatus.Approved || receipt.Status == GoodsReceiptStatus.Rejected ||
                receipt.Status == GoodsReceiptStatus.Cancelled)
                throw new InvalidBusinessRuleException(
                    "Chỉ được đổi kho khi phiếu nhập chưa duyệt (Draft, Received, QCCompleted, PendingManagerApprovalQc, PendingManagerApproval). Phiếu đã Approved/Rejected/Cancelled không thể đổi kho.");

            if (receipt.WarehouseId == request.WarehouseId)
                return;

            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
            if (warehouse == null)
                throw new NotFoundException("Kho đích không tồn tại");

            receipt.WarehouseId = request.WarehouseId;
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Phiếu nhập {ReceiptId} đã chuyển sang kho {WarehouseId} bởi {UserId}", receiptId, request.WarehouseId, userId);
        }

        // ===============================
        // GENERATE LOTS (internal: at Approve / ManagerApprove)
        // ===============================
        private async Task CreateLotsAndSetApprovedAsync(GoodsReceipt receipt, string userId)
        {
            foreach (var detail in receipt.Details)
            {
                var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId);
                if (poDetail == null)
                    continue;

                // Đối chiếu khối lượng PO theo khối lượng thực nhận (ReceivedWeight), không phải khối lượng sau QC
                if (poDetail.ReceivedWeight + detail.ReceivedWeight > poDetail.OrderedWeight)
                    throw new InvalidBusinessRuleException(
                        $"Dòng đơn mua Id={poDetail.Id}: tổng đã nhận ({poDetail.ReceivedWeight} + {detail.ReceivedWeight}) vượt quá khối lượng đặt ({poDetail.OrderedWeight}).");
                poDetail.ReceivedWeight += detail.ReceivedWeight;

                if (detail.QcRecord == null)
                    throw new InvalidBusinessRuleException("Có dòng chưa QC. Vui lòng QC trước khi duyệt phiếu.");
                if (detail.QcRecord.ClassificationDetails == null || !detail.QcRecord.ClassificationDetails.Any())
                    throw new InvalidBusinessRuleException("Có dòng chưa có phân loại ProductVariant sau QC.");

                foreach (var classification in detail.QcRecord.ClassificationDetails)
                {
                    var variant = await _productVariantRepo.GetProductVariantByIdAsync(classification.ProductVariantId)
                        ?? throw new NotFoundException($"ProductVariant {classification.ProductVariantId} không tồn tại");
                    var costUnitPrice = poDetail.SupplierPlanDetail?.UnitPriceAtOrder ?? poDetail.UnitPrice;
                    var costPriceDate = poDetail.SupplierPlanDetail?.PriceDate;
                    var costSourceType = detail.SupplierPlanDetailId.HasValue
                        ? "SupplierPlanDetail"
                        : "LegacyPurchaseOrderDetail";
                    var costSourceRefId = detail.SupplierPlanDetailId ?? poDetail.Id;

                    var shelfLifeDays = variant.ShelfLifeDays;
                    var receivedAt = DateTime.UtcNow;
                    if (poDetail.HarvestDate > receivedAt)
                        throw new InvalidBusinessRuleException("HarvestDate không được lớn hơn ReceivedDate");

                    var expiryDate = poDetail.HarvestDate.AddDays(shelfLifeDays);
                    var remainingDays = (expiryDate - receivedAt).TotalDays;
                    var minRemaining = shelfLifeDays * 0.3;
                    if (remainingDays < minRemaining)
                        throw new InvalidBusinessRuleException("Thời hạn còn lại của lô phải >= 30% ShelfLifeDays tại thời điểm nhập kho");

                    var lot = new Lot
                    {
                        LotCode = $"LOT-{DateTime.UtcNow.Ticks}-{detail.Id}-{classification.ProductVariantId}",
                        GoodsReceiptDetailId = detail.Id,
                        ProductVariantId = classification.ProductVariantId,
                        TotalQuantity = classification.Quantity,
                        RemainingQuantity = classification.Quantity,
                        CostUnitPrice = costUnitPrice,
                        CostPriceDate = costPriceDate,
                        CostSourceType = costSourceType,
                        CostSourceRefId = costSourceRefId,
                        ReceivedDate = receivedAt,
                        ExpiryDate = expiryDate
                    };
                    await _lotRepo.AddRangeAsync(new List<Lot> { lot });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            receipt.Status = GoodsReceiptStatus.Approved;
            receipt.ApprovedBy = userId;
            receipt.ApprovedAt = DateTime.UtcNow;
            receipt.PendingReason = null; // Đã duyệt, xóa lý do chờ Manager
            await _unitOfWork.SaveChangesAsync();

            await PersistPrintSnapshotAfterApproveAsync(receipt.Id);
        }

        /// <summary>Check Capacity: đảm bảo kho đích còn đủ dung lượng trống cho tổng UsableWeight của phiếu.</summary>
        private async Task EnsureWarehouseCapacityAsync(GoodsReceipt receipt)
        {
            decimal totalInboundVolumeM3 = 0m;
            foreach (var detail in receipt.Details)
            {
                var classifications = detail.QcRecord?.ClassificationDetails ?? new List<QcClassificationDetail>();
                foreach (var c in classifications)
                {
                    if (c.Quantity <= 0) continue;
                    var density = (await _productVariantRepo.GetProductVariantByIdAsync(c.ProductVariantId))?.DensityKgPerM3 ?? 0m;
                    if (density <= 0)
                    {
                        throw new InvalidBusinessRuleException(
                            $"Sản phẩm (ProductVariantId={c.ProductVariantId}) chưa cấu hình khối lượng riêng > 0.");
                    }
                    totalInboundVolumeM3 += c.Quantity / density;
                }
            }

            if (totalInboundVolumeM3 <= 0)
                return;
            var operationalRequiredVolume = totalInboundVolumeM3 / OperationalBufferRatio;

            // Dung lượng kho cần tính cả hàng đã xếp slot và hàng chưa xếp slot
            // để tránh duyệt phiếu nhập vượt sức chứa "ảo".
            decimal totalCapacity = await _warehouseRepo.GetTotalCapacityByWarehouseIdAsync(receipt.WarehouseId);
            decimal effectiveCapacity = totalCapacity * MaxSlotUtilizationRatio;
            decimal assignedVolume = await _boxRepo.GetAssignedStockVolumeByWarehouseIdAsync(receipt.WarehouseId);
            decimal unassignedVolume = await _boxRepo.GetUnassignedStockVolumeByWarehouseIdAsync(receipt.WarehouseId);
            decimal usedCapacity = assignedVolume + unassignedVolume;
            decimal remainingCapacity = Math.Max(0, effectiveCapacity - usedCapacity);

            if (operationalRequiredVolume - remainingCapacity > CapacityTolerance)
            {
                var warehouseName = receipt.Warehouse?.Name ?? $"Id={receipt.WarehouseId}";
                throw new InvalidBusinessRuleException(
                    $"Kho [{warehouseName}] chỉ còn {remainingCapacity:N4} m³ trống (đã gồm hàng chưa xếp slot). " +
                    $"Phiếu nhập cần khoảng {operationalRequiredVolume:N4} m³ (đã tính đệm vận hành 80% từ thể tích quy đổi). " +
                    $"Kho đang áp dụng ngưỡng vận hành tối đa 80% sức chứa để chừa lối thao tác. Không đủ dung lượng.");
            }
        }

        /// <summary>
        /// Check Capacity sớm khi tạo phiếu nhập có kèm dòng chi tiết.
        /// Dùng ReceivedWeight theo ProductVariant của PO line để ước tính thể tích cần nhập.
        /// </summary>
        private async Task EnsureWarehouseCapacityAtCreateAsync(
            int warehouseId,
            IEnumerable<CreateGoodsReceiptDetailLineRequest> detailLines)
        {
            var allVariants = (await _productVariantRepo.GetAllAsync())?.ToList() ?? new List<ProductVariant>();
            var minDensityByProductId = allVariants
                .Where(v => v.ProductId > 0 && v.DensityKgPerM3 > 0)
                .GroupBy(v => v.ProductId)
                .ToDictionary(g => g.Key, g => g.Min(v => v.DensityKgPerM3));

            decimal totalInboundVolumeM3 = 0m;
            foreach (var line in detailLines)
            {
                if (line.ReceivedWeight <= 0) continue;

                var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(line.PurchaseOrderDetailId)
                    ?? throw new NotFoundException($"Không tìm thấy dòng chi tiết đơn mua #{line.PurchaseOrderDetailId}");

                var productId = poDetail.ProductId;
                if (productId <= 0)
                    throw new InvalidBusinessRuleException(
                        $"Dòng chi tiết đơn mua #{poDetail.Id} chưa xác định sản phẩm để quy đổi dung tích.");

                var density = minDensityByProductId.TryGetValue(productId, out var minDensity)
                    ? minDensity
                    : 0m;
                if (density <= 0)
                {
                    throw new InvalidBusinessRuleException(
                        $"Sản phẩm mã #{productId} chưa có biến thể cấu hình khối lượng riêng lớn hơn 0.");
                }

                totalInboundVolumeM3 += line.ReceivedWeight / density;
            }

            if (totalInboundVolumeM3 <= 0) return;

            var operationalRequiredVolume = totalInboundVolumeM3 / OperationalBufferRatio;
            decimal totalCapacity = await _warehouseRepo.GetTotalCapacityByWarehouseIdAsync(warehouseId);
            decimal effectiveCapacity = totalCapacity * MaxSlotUtilizationRatio;
            decimal assignedVolume = await _boxRepo.GetAssignedStockVolumeByWarehouseIdAsync(warehouseId);
            decimal unassignedVolume = await _boxRepo.GetUnassignedStockVolumeByWarehouseIdAsync(warehouseId);
            decimal usedCapacity = assignedVolume + unassignedVolume;
            decimal remainingCapacity = Math.Max(0, effectiveCapacity - usedCapacity);

            if (operationalRequiredVolume - remainingCapacity > CapacityTolerance)
            {
                var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(warehouseId);
                var warehouseName = warehouse?.Name ?? $"Id={warehouseId}";
                throw new InvalidBusinessRuleException(
                    $"Kho [{warehouseName}] chỉ còn {remainingCapacity:N4} m³ trống (đã gồm hàng chưa xếp slot). " +
                    $"Phiếu nhập cần khoảng {operationalRequiredVolume:N4} m³ (đã tính đệm vận hành 80% từ thể tích quy đổi). " +
                    $"Kho đang áp dụng ngưỡng vận hành tối đa 80% sức chứa để chừa lối thao tác. Không đủ dung lượng.");
            }
        }

        /// <summary>Định mức tối thiểu chỉ là cảnh báo. Nếu dưới định mức (kho hoặc sản phẩm) trả về thông báo để chuyển Manager xem xét; null = đạt định mức.</summary>
        private async Task<string?> TryGetMinReceiptWeightWarningAsync(GoodsReceipt receipt)
        {
            decimal totalUsableWeight = receipt.Details.Sum(d => d.UsableWeight ?? 0m);
            var warnings = new List<string>();

            // Theo kho: tổng phiếu nhỏ hơn định mức tối thiểu của kho
            decimal? warehouseMin = receipt.Warehouse?.MinReceiptWeight;
            if (warehouseMin.HasValue && warehouseMin.Value > 0 && totalUsableWeight < warehouseMin.Value)
            {
                var warehouseName = receipt.Warehouse?.Name ?? $"Id={receipt.WarehouseId}";
                warnings.Add($"Tổng khối lượng nhập {totalUsableWeight:N2} kg thấp hơn định mức tối thiểu của kho [{warehouseName}] ({warehouseMin.Value:N2} kg).");
            }

            var classificationGroups = receipt.Details
                .SelectMany(d => d.QcRecord?.ClassificationDetails ?? Enumerable.Empty<QcClassificationDetail>())
                .GroupBy(x => x.ProductVariantId)
                .Select(g => new { ProductVariantId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();
            foreach (var group in classificationGroups)
            {
                var variant = await _productVariantRepo.GetProductVariantByIdAsync(group.ProductVariantId);
                if (variant?.MinReceiptWeight is decimal minVariant && minVariant > 0 && group.Quantity < minVariant)
                {
                    var variantName = string.IsNullOrWhiteSpace(variant.Name) ? $"Variant #{group.ProductVariantId}" : variant.Name;
                    warnings.Add($"Biến thể [{variantName}] có khối lượng phân loại {group.Quantity:N2} kg thấp hơn định mức tối thiểu ({minVariant:N2} kg).");
                }
            }

            if (warnings.Count == 0) return null;
            return "Cảnh báo định mức tối thiểu: " + string.Join(" ", warnings) + " Cần Manager xem xét Approve hoặc Reject.";
        }

        /// <summary>Dung sai theo từng dòng PO: mất mát thực tế vượt quá OrderedWeight * TolerancePercent của dòng.</summary>
        private bool CheckToleranceExceeded(GoodsReceipt receipt)
        {
            foreach (var d in receipt.Details)
            {
                var po = d.PurchaseOrderDetail;
                if (po == null || po.OrderedWeight <= 0) continue;
                decimal allowedLoss = po.OrderedWeight * po.TolerancePercent / 100;
                decimal actualLoss = d.RejectWeight;
                if (actualLoss > allowedLoss)
                    return true;
            }
            return false;
        }

        private async Task AutoApproveCreatedReceiptByManagerAsync(int receiptId, string userId)
        {
            _ = userId;
            await ApplyPrivilegedFirstApprovalIfDraftAsync(receiptId);
        }

        /// <inheritdoc />
        public async Task ApplyPrivilegedFirstApprovalIfDraftAsync(int goodsReceiptId)
        {
            await _unitOfWork.ExecuteInRetryableTransactionAsync(async () =>
            {
                var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(goodsReceiptId);
                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");
                if (receipt.Status != GoodsReceiptStatus.Draft)
                    return;
                if (!receipt.Details.Any())
                {
                    _logger.LogWarning(
                        "Receipt {ReceiptId}: skip bỏ qua duyệt bước 1 — chưa có dòng chi tiết",
                        goodsReceiptId);
                    return;
                }

                receipt.Status = GoodsReceiptStatus.Received;
                if (!string.IsNullOrWhiteSpace(receipt.PendingReason))
                    receipt.PendingReason = null;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation(
                    "Receipt {ReceiptId}: Admin/Manager bỏ qua duyệt bước 1 → Received (vẫn phải QC thủ công)",
                    goodsReceiptId);
            });
        }

        // ===============================
        // GENERATE BOXES (only after receipt Approved; remainder box; create Import transactions)
        // ===============================
        public async Task<IReadOnlyList<BoxCreatedItemDto>> GenerateBoxesAsync(CreateBoxesRequest request, string userId)
        {
            const int maxBoxesPerRequest = 5000;
            const decimal boxUsableRatio = 0.8m;

            var lot = await _lotRepo.GetByIdWithDetailAndReceiptAsync(request.LotId);
            if (lot == null)
                throw new NotFoundException("Lot không tồn tại");
            if (lot.GoodsReceiptDetail?.GoodsReceipt == null)
                throw new InvalidBusinessRuleException("Lot không thuộc phiếu nhập hợp lệ");
            if (lot.GoodsReceiptDetail.GoodsReceipt.Status != GoodsReceiptStatus.Approved)
                throw new InvalidBusinessRuleException("Chỉ được tạo Box sau khi phiếu nhập đã được duyệt (Approved)");

            // Chống tạo box vô hạn: tính trực tiếp theo DB (TotalQuantity - tổng Weight các box đã tạo).
            // Đồng thời đồng bộ lại RemainingQuantity nếu trước đó bị lệch.
            var alreadyBoxed = await _boxRepo.GetTotalBoxWeightByLotIdAsync(lot.Id);
            decimal total = Math.Max(0, lot.TotalQuantity - alreadyBoxed);
            if (lot.RemainingQuantity != total)
                lot.RemainingQuantity = total;
            decimal requestedBoxSize = request.BoxSize;
            if (requestedBoxSize <= 0)
                throw new InvalidBusinessRuleException("BoxSize phải lớn hơn 0");
            decimal boxSize = decimal.Round(requestedBoxSize * boxUsableRatio, 6, MidpointRounding.AwayFromZero);
            if (boxSize <= 0)
                throw new InvalidBusinessRuleException("BoxSize hiệu dụng (80%) phải lớn hơn 0");
            if (request.BoxType == BoxType.Unknown)
                throw new InvalidBusinessRuleException("Vui lòng chọn BoxType khác Unknown khi tạo box");
            if (total <= 0)
                throw new InvalidBusinessRuleException("Lot đã hết khối lượng khả dụng để tạo box");
            var densityKgPerM3 =
                lot.ProductVariant?.DensityKgPerM3
                ?? lot.GoodsReceiptDetail?.ProductVariant?.DensityKgPerM3
                ?? 0m;
            if (densityKgPerM3 <= 0)
                throw new InvalidBusinessRuleException("Biến thể sản phẩm chưa có khối lượng riêng hợp lệ để quy đổi thể tích.");

            // Thể tích một thùng đầy theo kích cỡ chuẩn (không nhân 80%). Hệ số 80% chỉ áp dụng cho KL hàng/thùng
            // khi chia lô; chiếm chỗ kho/slot phải theo thùng vật lý (vd. 0,1 m³), tránh đếm thiếu so với thực tế.
            var nominalFullBoxVolumeM3 = decimal.Round(
                requestedBoxSize / densityKgPerM3,
                6,
                MidpointRounding.AwayFromZero);

            int fullCount = (int)(total / boxSize);
            decimal remainder = total - fullCount * boxSize;
            int estimatedBoxes = fullCount + (remainder > 0 ? 1 : 0);
            if (estimatedBoxes > maxBoxesPerRequest)
            {
                throw new InvalidBusinessRuleException(
                    $"Số box dự kiến ({estimatedBoxes:N0}) vượt giới hạn {maxBoxesPerRequest:N0}/lần tạo. " +
                    "Vui lòng tăng BoxSize hoặc chia nhỏ thao tác tạo box.");
            }
            var boxesToCreate = new List<Box>();
            string baseCode = $"BOX-{DateTime.UtcNow:yyyyMMddHHmmss}";

            for (int i = 0; i < fullCount; i++)
            {
                var boxCode = $"{baseCode}-{i + 1}";
                boxesToCreate.Add(new Box
                {
                    LotId = lot.Id,
                    Weight = boxSize,
                    VolumeM3 = nominalFullBoxVolumeM3,
                    Status = BoxStatus.Stored,
                    BoxCode = boxCode,
                    QRCode = boxCode,
                    BoxType = request.BoxType,
                    IsPartial = false
                });
            }
            if (remainder > 0)
            {
                var boxCode = $"{baseCode}-{fullCount + 1}";
                boxesToCreate.Add(new Box
                {
                    LotId = lot.Id,
                    Weight = remainder,
                    VolumeM3 = remainder / densityKgPerM3,
                    Status = BoxStatus.Stored,
                    BoxCode = boxCode,
                    QRCode = boxCode,
                    BoxType = request.BoxType,
                    IsPartial = true
                });
            }

            foreach (var box in boxesToCreate)
            {
                await _boxRepo.CreateAsync(box);
            }
            await _unitOfWork.SaveChangesAsync();


            foreach (var box in boxesToCreate)
            {
                await _inventoryTranRepo.CreateAsync(new InventoryTransaction
                {
                    BoxId = box.Id,
                    TransactionType = InventoryTransactionType.Import,
                    Quantity = box.Weight,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Trừ phần đã đóng box khỏi lot để lần sau không thể tạo vượt quá khối lượng còn lại.
            var createdWeight = boxesToCreate.Sum(b => b.Weight);
            lot.RemainingQuantity = Math.Max(0, lot.RemainingQuantity - createdWeight);
            await _unitOfWork.SaveChangesAsync();

            return boxesToCreate
                .Select(b => new BoxCreatedItemDto
                {
                    Id = b.Id,
                    BoxCode = b.BoxCode,
                    QrPayload = b.QRCode ?? b.BoxCode
                })
                .ToList();
        }

        /// <summary>
        /// Legacy: Lot is now created at Approve. Exposed for backward compatibility or admin use only.
        /// </summary>
        public async Task GenerateLotAsync(int goodsReceiptDetailId)
        {
            var detail = await _detailRepo.GetByIdAsync(goodsReceiptDetailId);
            if (detail == null)
                throw new NotFoundException("Không tìm thấy chi tiết phiếu nhập");

            var usable = detail.UsableWeight;
            if (!usable.HasValue || usable.Value <= 0)
                throw new InvalidBusinessRuleException("UsableWeight phải lớn hơn 0 để tạo Lot");

            var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId);
            if (poDetail == null)
                throw new NotFoundException("Chi tiết đơn mua không tồn tại");

            if (!detail.ProductVariantId.HasValue)
                throw new InvalidBusinessRuleException("Chi tiết chưa có ProductVariant sau QC.");
            var variant = await _productVariantRepo.GetProductVariantByIdAsync(detail.ProductVariantId.Value)
                ?? throw new NotFoundException("ProductVariant không tồn tại");
            var shelfLifeDays = variant.ShelfLifeDays;
            var receivedAt = DateTime.UtcNow;

            if (poDetail.HarvestDate > receivedAt)
                throw new InvalidBusinessRuleException("HarvestDate không được lớn hơn ReceivedDate");

            var expiryDate = poDetail.HarvestDate.AddDays(shelfLifeDays);

            var remainingDays = (expiryDate - receivedAt).TotalDays;
            var minRemaining = shelfLifeDays * 0.3;
            if (remainingDays < minRemaining)
                throw new InvalidBusinessRuleException("Thời hạn còn lại của lô phải >= 30% ShelfLifeDays tại thời điểm nhập kho");

            var lot = new Lot
            {
                LotCode = $"LOT-{DateTime.UtcNow.Ticks}",
                GoodsReceiptDetailId = goodsReceiptDetailId,
                ProductVariantId = variant.Id,
                TotalQuantity = usable.Value,
                RemainingQuantity = usable.Value,
                CostUnitPrice = poDetail.SupplierPlanDetail?.UnitPriceAtOrder ?? poDetail.UnitPrice,
                CostPriceDate = poDetail.SupplierPlanDetail?.PriceDate,
                CostSourceType = detail.SupplierPlanDetailId.HasValue ? "SupplierPlanDetail" : "LegacyPurchaseOrderDetail",
                CostSourceRefId = detail.SupplierPlanDetailId ?? poDetail.Id,
                ReceivedDate = receivedAt,
                ExpiryDate = expiryDate
            };
            await _lotRepo.AddRangeAsync(new List<Lot> { lot });
            await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // PRINT: Phiếu nhập kho (HTML — FE template)
        // ===============================

        public async Task<GoodsReceiptPrintDataDto> GetGoodsReceiptPrintDataAsync(int receiptId, string? phase, bool preview)
        {
            if (preview)
            {
                var rPreview = await _receiptRepo.GetGoodsReceiptForPrintAsync(receiptId)
                    ?? throw new NotFoundException("Phiếu nhập không tồn tại");
                return BuildGoodsReceiptPrintDataDto(rPreview, "preview", isPreview: true);
            }

            var receipt = await _receiptRepo.GetGoodsReceiptForPrintAsync(receiptId)
                ?? throw new NotFoundException("Phiếu nhập không tồn tại");

            var p = phase?.Trim().ToLowerInvariant();

            if (p == "afterqc")
            {
                if (!string.IsNullOrWhiteSpace(receipt.PrintSnapshotAfterQcJson))
                {
                    var dto = DeserializeGoodsReceiptPrint(receipt.PrintSnapshotAfterQcJson);
                    dto.SupplierName = BuildReceiptSupplierDisplayName(receipt);
                    return dto;
                }
                if (receipt.Status == GoodsReceiptStatus.QCCompleted
                    || receipt.Status == GoodsReceiptStatus.PendingManagerApproval)
                    return BuildGoodsReceiptPrintDataDto(receipt, "afterQc", isPreview: false);
                throw new InvalidBusinessRuleException(
                    "Chưa có bản in sau QC. Vui lòng hoàn tất QC toàn bộ dòng.");
            }

            if (p == "afterapprove")
            {
                if (!string.IsNullOrWhiteSpace(receipt.PrintSnapshotAfterApproveJson))
                {
                    var dto = DeserializeGoodsReceiptPrint(receipt.PrintSnapshotAfterApproveJson);
                    dto.SupplierName = BuildReceiptSupplierDisplayName(receipt);
                    return dto;
                }
                if (receipt.Status == GoodsReceiptStatus.Approved)
                    return BuildGoodsReceiptPrintDataDto(receipt, "afterApprove", isPreview: false);
                throw new InvalidBusinessRuleException(
                    "Chưa có bản in sau duyệt. Phiếu cần ở trạng thái đã duyệt nhập kho.");
            }

            if (!string.IsNullOrWhiteSpace(receipt.PrintSnapshotAfterApproveJson))
            {
                var dto = DeserializeGoodsReceiptPrint(receipt.PrintSnapshotAfterApproveJson);
                dto.SupplierName = BuildReceiptSupplierDisplayName(receipt);
                return dto;
            }
            if (!string.IsNullOrWhiteSpace(receipt.PrintSnapshotAfterQcJson))
            {
                var dto = DeserializeGoodsReceiptPrint(receipt.PrintSnapshotAfterQcJson);
                dto.SupplierName = BuildReceiptSupplierDisplayName(receipt);
                return dto;
            }
            if (receipt.Status == GoodsReceiptStatus.Approved)
                return BuildGoodsReceiptPrintDataDto(receipt, "afterApprove", isPreview: false);
            if (receipt.Status == GoodsReceiptStatus.QCCompleted
                || receipt.Status == GoodsReceiptStatus.PendingManagerApproval)
                return BuildGoodsReceiptPrintDataDto(receipt, "afterQc", isPreview: false);

            throw new InvalidBusinessRuleException(
                "Chưa đủ dữ liệu in. Hoàn tất QC, hoặc truyền preview=true để xem trước.");
        }

        private async Task PersistPrintSnapshotAfterQcAsync(int receiptId)
        {
            var r = await _receiptRepo.GetGoodsReceiptForPrintAsync(receiptId);
            if (r == null) return;
            if (r.Status != GoodsReceiptStatus.QCCompleted && r.Status != GoodsReceiptStatus.PendingManagerApproval)
                return;
            if (!string.IsNullOrWhiteSpace(r.PrintSnapshotAfterQcJson))
                return;

            var dto = BuildGoodsReceiptPrintDataDto(r, "afterQc", isPreview: false);
            r.PrintSnapshotAfterQcJson = JsonSerializer.Serialize(dto, PrintJsonOptions);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task PersistPrintSnapshotAfterApproveAsync(int receiptId)
        {
            var r = await _receiptRepo.GetGoodsReceiptForPrintAsync(receiptId);
            if (r == null) return;
            if (r.Status != GoodsReceiptStatus.Approved) return;
            if (!string.IsNullOrWhiteSpace(r.PrintSnapshotAfterApproveJson))
                return;

            var dto = BuildGoodsReceiptPrintDataDto(r, "afterApprove", isPreview: false);
            r.PrintSnapshotAfterApproveJson = JsonSerializer.Serialize(dto, PrintJsonOptions);
            await _unitOfWork.SaveChangesAsync();
        }

        private static GoodsReceiptPrintDataDto DeserializeGoodsReceiptPrint(string json)
        {
            var dto = JsonSerializer.Deserialize<GoodsReceiptPrintDataDto>(json, PrintJsonOptions);
            if (dto == null)
                throw new InvalidBusinessRuleException("Dữ liệu in (snapshot) không hợp lệ.");
            return dto;
        }

        private static GoodsReceiptPrintDataDto BuildGoodsReceiptPrintDataDto(
            GoodsReceipt receipt,
            string snapshotPhase,
            bool isPreview)
        {
            var requiresManagerAttention =
                receipt.Status == GoodsReceiptStatus.PendingManagerApproval
                || receipt.Status == GoodsReceiptStatus.PendingManagerApprovalQc;

            string? printWarning = null;
            if (requiresManagerAttention)
            {
                printWarning = string.IsNullOrWhiteSpace(receipt.PendingReason)
                    ? "Phiếu đang chờ Quản lý xử lý."
                    : "Phiếu đang chờ Quản lý xử lý. " + receipt.PendingReason!.Trim();
            }

            var kind = receipt.PurchaseOrderId.HasValue
                ? receipt.InboundReceiptKind
                : InboundReceiptKind.DirectInbound;

            var lines = new List<GoodsReceiptPrintLineDto>();
            var n = 0;
            foreach (var d in receipt.Details.OrderBy(x => x.Id))
            {
                n++;
                var productName = d.Product?.Name?.Trim() ?? "N/A";
                var grade = d.QcRecord?.ClassificationDetails != null
                    ? string.Join(", ", d.QcRecord.ClassificationDetails.Select(c => c.ProductVariantId))
                    : string.Empty;

                lines.Add(new GoodsReceiptPrintLineDto
                {
                    LineNo = n,
                    DetailId = d.Id,
                    ProductName = productName,
                    Grade = grade,
                    OrderedWeightKg = d.PurchaseOrderDetail?.OrderedWeight,
                    ReceivedWeightKg = d.ReceivedWeight,
                    UsableWeightKg = d.UsableWeight,
                    UnitPrice = d.UnitPrice,
                    LineTotal = d.UnitPrice > 0 ? d.ReceivedWeight * d.UnitPrice : null,
                    QcResult = d.QCResult.ToString(),
                    QcNote = d.QcRecord?.QCNote,
                    InspectedBy = d.InspectedBy,
                    InspectedAtUtc = d.InspectedAt
                });
            }

            var approvedName = receipt.ApprovedUser?.FullName?.Trim()
                ?? receipt.ApprovedUser?.UserName?.Trim();

            return new GoodsReceiptPrintDataDto
            {
                SchemaVersion = "1",
                DocumentTitle = "Phiếu nhập kho",
                SnapshotAtUtc = DateTime.UtcNow,
                SnapshotPhase = snapshotPhase,
                IsPreview = isPreview,
                RequiresManagerAttention = requiresManagerAttention,
                PrintWarningMessage = printWarning,
                ReceiptType = kind.ToString(),
                NonPoReason = receipt.NonPoReason,
                ReceiptId = receipt.Id,
                ReceiptCode = receipt.ReceiptCode,
                ReceiptStatus = receipt.Status.ToString(),
                PurchaseOrderId = receipt.PurchaseOrderId,
                PurchaseOrderCode = receipt.PurchaseOrder?.OrderCode,
                SupplierName = BuildReceiptSupplierDisplayName(receipt),
                WarehouseName = receipt.Warehouse?.Name?.Trim() ?? "N/A",
                SourceWarehouseName = receipt.Warehouse?.Name?.Trim(),
                SourceWarehouseAddress = receipt.Warehouse?.Location?.Trim(),
                VehicleNumber = receipt.VehicleNumber,
                DriverName = receipt.DriverName,
                TransportCompany = receipt.TransportCompany,
                ReceivedDate = receipt.ReceivedDate,
                TotalReceivedWeight = receipt.TotalReceivedWeight,
                TotalUsableWeight = receipt.TotalUsableWeight,
                TotalAmount = lines.Sum(x => x.LineTotal ?? 0m),
                ApprovedByUserName = approvedName,
                ApprovedAtUtc = receipt.ApprovedAt,
                Lines = lines
            };
        }

        // ===============================
        // QUERY: Get all / get by id
        // ===============================

        public async Task<IEnumerable<GoodsReceiptSummaryDto>> GetAllAsync()
        {
            var receipts = await _receiptRepo.GetAllGoodsReceiptsAsync();

            return receipts.Select(r => new GoodsReceiptSummaryDto
            {
                Id = r.Id,
                ReceiptCode = r.ReceiptCode,
                Status = r.Status.ToString(),
                PendingReason = r.PendingReason,
                PurchaseOrderId = r.PurchaseOrderId,
                SupplierId = r.SupplierId,
                SupplierName = BuildReceiptSupplierDisplayName(r),
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                ReceivedDate = r.ReceivedDate,
                TotalReceivedWeight = r.TotalReceivedWeight,
                TotalUsableWeight = r.TotalUsableWeight,

                CreatedById = r.CreatedBy,
                CreatedByName = r.CreatedUser?.FullName ?? r.CreatedUser?.UserName ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<GoodsReceiptResponseDto> GetByIdAsync(int id)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(id)
                ?? throw new NotFoundException("Phiếu nhập không tồn tại");

            var dto = new GoodsReceiptResponseDto
            {
                Id = receipt.Id,
                ReceiptCode = receipt.ReceiptCode,
                Status = receipt.Status.ToString(),
                PendingReason = receipt.PendingReason,
                PurchaseOrderId = receipt.PurchaseOrderId,
                SupplierId = receipt.SupplierId,
                SupplierName = BuildReceiptSupplierDisplayName(receipt),
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse?.Name ?? string.Empty,
                ReceivedDate = receipt.ReceivedDate,
                TotalReceivedWeight = receipt.TotalReceivedWeight,
                TotalUsableWeight = receipt.TotalUsableWeight,

                CreatedById = receipt.CreatedBy,
                CreatedByName = receipt.CreatedUser?.FullName ?? receipt.CreatedUser?.UserName ?? string.Empty,
                CreatedAt = receipt.CreatedAt
            };

            dto.Details = receipt.Details.Select(d => new GoodsReceiptDetailLineDto
            {
                Id = d.Id,
                SupplierPlanDetailId = d.SupplierPlanDetailId,
                ProductId = d.ProductId,
                ProductVariantId = d.ProductVariantId,
                ProductName = d.Product?.Name ?? string.Empty,
                ReceivedWeight = d.ReceivedWeight,
                UsableWeight = d.UsableWeight,
                RejectWeight = d.RejectWeight,
                QCResult = d.QCResult.ToString(),
                InspectedWeight = d.QcRecord?.InspectedWeight,
                DamagedWeight = d.QcRecord?.DamagedWeight,
                ClassificationDetails = d.QcRecord?.ClassificationDetails
                    .Select(c => new QcClassificationDetailDto
                    {
                        ProductVariantId = c.ProductVariantId,
                        ProductVariantName = c.ProductVariant?.Name ?? $"Variant #{c.ProductVariantId}",
                        Quantity = c.Quantity
                    }).ToList() ?? new List<QcClassificationDetailDto>()
            }).ToList();

            return dto;
        }

        public async Task<GoodsReceiptForApprovalDto> GetByIdForApprovalAsync(int id)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(id)
                ?? throw new NotFoundException("Phiếu nhập không tồn tại");

            var details = receipt.Details.Select(d =>
            {
                var lineTotal = (d.UsableWeight ?? 0m) * d.UnitPrice;
                return new GoodsReceiptDetailLineForApprovalDto
                {
                    Id = d.Id,
                    SupplierPlanDetailId = d.SupplierPlanDetailId,
                    ProductId = d.ProductId,
                    ProductVariantId = d.ProductVariantId,
                    ProductName = d.Product?.Name ?? string.Empty,
                    ReceivedWeight = d.ReceivedWeight,
                    UsableWeight = d.UsableWeight,
                    RejectWeight = d.RejectWeight,
                    QCResult = d.QCResult.ToString(),
                    UnitPrice = d.UnitPrice,
                    LineTotal = lineTotal,
                    InspectedWeight = d.QcRecord?.InspectedWeight,
                    DamagedWeight = d.QcRecord?.DamagedWeight,
                    ClassificationDetails = d.QcRecord?.ClassificationDetails
                        .Select(c => new QcClassificationDetailDto
                        {
                            ProductVariantId = c.ProductVariantId,
                            ProductVariantName = c.ProductVariant?.Name ?? $"Variant #{c.ProductVariantId}",
                            Quantity = c.Quantity
                        }).ToList() ?? new List<QcClassificationDetailDto>()
                };
            }).ToList();

            return new GoodsReceiptForApprovalDto
            {
                Id = receipt.Id,
                ReceiptCode = receipt.ReceiptCode,
                Status = receipt.Status.ToString(),
                PendingReason = receipt.PendingReason,
                PurchaseOrderId = receipt.PurchaseOrderId,
                SupplierId = receipt.SupplierId,
                SupplierName = BuildReceiptSupplierDisplayName(receipt),
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse?.Name ?? string.Empty,
                ReceivedDate = receipt.ReceivedDate,
                TotalReceivedWeight = receipt.TotalReceivedWeight,
                TotalUsableWeight = receipt.TotalUsableWeight,
                TotalAmount = details.Sum(x => x.LineTotal),
                Details = details,

                CreatedById = receipt.CreatedBy,
                CreatedByName = receipt.CreatedUser?.FullName ?? receipt.CreatedUser?.UserName ?? string.Empty,
                CreatedAt = receipt.CreatedAt
            };
        }

        private static string BuildReceiptSupplierDisplayName(GoodsReceipt receipt)
        {
            var order = receipt.PurchaseOrder;
            if (order?.ProcurementMode == ProcurementMode.MultiSupplierStrictReceipt &&
                order.SupplierPlans != null &&
                order.SupplierPlans.Count > 0)
            {
                var names = order.SupplierPlans
                    .Select(p => p.Supplier?.Name?.Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (names.Count > 0)
                {
                    return string.Join(", ", names);
                }
            }

            return receipt.Supplier?.Name ?? string.Empty;
        }
    }
}
