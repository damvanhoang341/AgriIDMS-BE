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
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class GoodsReceiptService : IGoodsReceiptService
    {
        private readonly IGoodsReceiptRepository _receiptRepo;
        private readonly IGoodsReceiptDetailRepository _detailRepo;
        private readonly ILotRepository _lotRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IProductVariantRepository _productVariantRepo;
        private readonly ILogger<GoodsReceiptService> _logger;
        private readonly IPurchaseOrderRepository _purchaseOrderRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;

        public GoodsReceiptService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            ILotRepository lotRepo,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepo,
            IProductVariantRepository productVariantRepository,
            ILogger<GoodsReceiptService> logger,
            IPurchaseOrderRepository purchaseOrderRepo,
            IBoxRepository boxRepo,
            IInventoryTransactionRepository inventoryTranRepo)
        {
            _receiptRepo = receiptRepo;
            _detailRepo = detailRepo;
            _lotRepo = lotRepo;
            _unitOfWork = unitOfWork;
            _supplierRepo = supplierRepository;
            _warehouseRepo = warehouseRepo;
            _productVariantRepo = productVariantRepository;
            _logger = logger;
            _purchaseOrderRepo = purchaseOrderRepo;
            _boxRepo = boxRepo;
            _inventoryTranRepo = inventoryTranRepo;
        }

        // ===============================
        // CREATE GOODS RECEIPT (3.5: PurchaseOrderId bắt buộc, validate PO tồn tại, đã duyệt, cùng NCC)
        // ===============================
        public async Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string userId)
        {
            _logger.LogInformation("User {UserId} tạo phiếu nhập kho", userId);

            var po = await _purchaseOrderRepo.GetByIdAsync(request.PurchaseOrderId);
            if (po == null)
                throw new NotFoundException("Đơn mua không tồn tại");
            if (po.Status != PurchaseOrderStatus.Approved)
                throw new InvalidBusinessRuleException("Chỉ được tạo phiếu nhập theo đơn mua đã duyệt");

            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
            if (warehouse == null)
                throw new NotFoundException("Kho không tồn tại");

            var receipt = new GoodsReceipt
            {
                ReceiptCode = await _receiptRepo.GenerateReceiptCodeAsync(),
                PurchaseOrderId = request.PurchaseOrderId,
                SupplierId = po.SupplierId,
                WarehouseId = request.WarehouseId,
                VehicleNumber = request.VehicleNumber,
                DriverName = request.DriverName,
                TransportCompany = request.TransportCompany,
                GrossWeight = request.GrossWeight,
                TareWeight = request.TareWeight,
                CreatedBy = userId,
                ReceivedBy = userId,
                ReceivedDate = DateTime.UtcNow,
                Status = GoodsReceiptStatus.Draft
            };

            await _receiptRepo.AddGoodsReceiptAsync(receipt);
            await _unitOfWork.SaveChangesAsync();
            return receipt.Id;
        }


        // ===============================
        // QC INSPECTION (3.6: Failed => UsableWeight = 0; 3.4: chuyển QCCompleted khi tất cả dòng đã QC)
        // ===============================
        public async Task QCInspectionAsync(QCInspectionRequest request, string userId)
        {
            var detail = await _detailRepo.GetByIdAsync(request.DetailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết phiếu nhập không tồn tại");
            if (request.UsableWeight > detail.ReceivedWeight)
                throw new InvalidBusinessRuleException("Khối lượng sử dụng được (UsableWeight) không được vượt quá khối lượng thực nhận (ReceivedWeight)");

            var qcResult = Enum.TryParse<QCResult>(request.QCResult, out var result) ? result
                : throw new InvalidBusinessRuleException("QCResult không hợp lệ");

            // 3.6: Khi QC không đạt (Failed), UsableWeight phải bằng 0
            if (qcResult == QCResult.Failed && request.UsableWeight != 0)
                throw new InvalidBusinessRuleException("Khi QC không đạt (Failed), khối lượng sử dụng được (UsableWeight) phải bằng 0");

            detail.UsableWeight = request.UsableWeight;
            detail.QCResult = qcResult;
            detail.QCNote = request.QCNote;
            detail.InspectedBy = userId;
            detail.InspectedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            // 3.4: Nếu tất cả dòng đã QC xong thì chuyển phiếu sang QCCompleted
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(detail.GoodsReceiptId);
            if (receipt != null && receipt.Details.All(d => d.QCResult != QCResult.Pending))
            {
                receipt.Status = GoodsReceiptStatus.QCCompleted;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // ===============================
        // APPROVE RECEIPT (tolerance check; Lots created here; no Boxes/Transactions)
        // ===============================
        public async Task ApproveGoodsReceiptAsync(int receiptId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var receipt = await _receiptRepo.GetGoodsReceiptForApproveAsync(receiptId);
                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");
                // 3.4: Cho phép duyệt khi Draft, Received hoặc QCCompleted
                if (receipt.Status != GoodsReceiptStatus.Draft && receipt.Status != GoodsReceiptStatus.Received && receipt.Status != GoodsReceiptStatus.QCCompleted)
                    throw new InvalidBusinessRuleException("Chỉ được duyệt phiếu nhập ở trạng thái Nháp (Draft), Đã nhập số liệu (Received) hoặc Đã QC (QCCompleted)");

                if (!receipt.Details.Any())
                    throw new InvalidBusinessRuleException("Phiếu nhập chưa có chi tiết");

                foreach (var d in receipt.Details)
                {
                    if (d.QCResult == QCResult.Pending)
                        throw new InvalidBusinessRuleException("Có sản phẩm chưa QC");
                }

                bool toleranceExceeded = CheckToleranceExceeded(receipt);
                if (toleranceExceeded)
                {
                    receipt.Status = GoodsReceiptStatus.PendingManagerApproval;
                    receipt.PendingReason = "Vượt dung sai cho phép, cần Manager xem xét Approve hoặc Reject.";
                    receipt.ApprovedBy = userId;
                    receipt.ApprovedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Receipt {ReceiptId} vượt dung sai, chuyển PendingManagerApproval", receiptId);
                    return;
                }

                // Định mức tối thiểu: chỉ cảnh báo → chuyển Manager xem xét, không chặn duyệt
                string? minReceiptWarning = TryGetMinReceiptWeightWarning(receipt);
                if (minReceiptWarning != null)
                {
                    receipt.Status = GoodsReceiptStatus.PendingManagerApproval;
                    receipt.PendingReason = minReceiptWarning;
                    receipt.ApprovedBy = userId;
                    receipt.ApprovedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Receipt {ReceiptId} dưới định mức tối thiểu, chuyển PendingManagerApproval: {Reason}", receiptId, minReceiptWarning);
                    return;
                }

                await EnsureWarehouseCapacityAsync(receipt);

                await CreateLotsAndSetApprovedAsync(receipt, userId);
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("Receipt {ReceiptId} đã được approve bởi {UserId}", receiptId, userId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ===============================
        // MANAGER APPROVE (when status = PendingManagerApproval)
        // ===============================
        public async Task ManagerApproveReceiptAsync(int receiptId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var receipt = await _receiptRepo.GetGoodsReceiptForApproveAsync(receiptId);
                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");
                if (receipt.Status != GoodsReceiptStatus.PendingManagerApproval)
                    throw new InvalidBusinessRuleException("Chỉ Manager có thể duyệt phiếu đang chờ duyệt (PendingManagerApproval)");

                await EnsureWarehouseCapacityAsync(receipt);
                // Định mức tối thiểu chỉ là cảnh báo: Manager vẫn có thể Approve hoặc Reject, không chặn ở đây.

                await CreateLotsAndSetApprovedAsync(receipt, userId);
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("Receipt {ReceiptId} đã được Manager approve bởi {UserId}", receiptId, userId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ===============================
        // MANAGER REJECT (when status = PendingManagerApproval)
        // ===============================
        public async Task ManagerRejectReceiptAsync(int receiptId, string userId)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(receiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.Status != GoodsReceiptStatus.PendingManagerApproval)
                throw new InvalidBusinessRuleException("Chỉ có thể từ chối phiếu đang chờ duyệt (PendingManagerApproval)");

            receipt.Status = GoodsReceiptStatus.Rejected;
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Receipt {ReceiptId} đã bị Manager từ chối bởi {UserId}", receiptId, userId);
        }

        // ===============================
        // GENERATE LOTS (internal: at Approve / ManagerApprove)
        // ===============================
        private async Task CreateLotsAndSetApprovedAsync(GoodsReceipt receipt, string userId)
        {
            foreach (var detail in receipt.Details)
            {
                var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId);
                if (poDetail != null)
                {
                    var shelfLifeDays = poDetail.ProductVariant.ShelfLifeDays;
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
                        LotCode = $"LOT-{DateTime.UtcNow.Ticks}-{detail.Id}",
                        GoodsReceiptDetailId = detail.Id,
                        TotalQuantity = detail.UsableWeight,
                        RemainingQuantity = detail.UsableWeight,
                        ReceivedDate = receivedAt,
                        ExpiryDate = expiryDate
                    };
                    await _lotRepo.AddRangeAsync(new List<Lot> { lot });

                    if (poDetail.ReceivedWeight + detail.UsableWeight > poDetail.OrderedWeight)
                        throw new InvalidBusinessRuleException(
                            $"Dòng đơn mua Id={poDetail.Id}: tổng đã nhận ({poDetail.ReceivedWeight} + {detail.UsableWeight}) vượt quá khối lượng đặt ({poDetail.OrderedWeight}).");
                    poDetail.ReceivedWeight += detail.UsableWeight;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            receipt.Status = GoodsReceiptStatus.Approved;
            receipt.ApprovedBy = userId;
            receipt.ApprovedAt = DateTime.UtcNow;
            receipt.PendingReason = null; // Đã duyệt, xóa lý do chờ Manager
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>Check Capacity: đảm bảo kho đích còn đủ dung lượng trống cho tổng UsableWeight của phiếu.</summary>
        private async Task EnsureWarehouseCapacityAsync(GoodsReceipt receipt)
        {
            decimal totalUsableWeight = receipt.Details.Sum(d => d.UsableWeight);
            if (totalUsableWeight <= 0)
                return;

            decimal remainingCapacity = await _warehouseRepo.GetTotalRemainingCapacityByWarehouseIdAsync(receipt.WarehouseId);
            if (totalUsableWeight > remainingCapacity)
            {
                var warehouseName = receipt.Warehouse?.Name ?? $"Id={receipt.WarehouseId}";
                throw new InvalidBusinessRuleException(
                    $"Kho [{warehouseName}] chỉ còn {remainingCapacity:N2} kg trống. Phiếu nhập {totalUsableWeight:N2} kg. Không đủ dung lượng. Vui lòng giải phóng dung lượng (xuất hàng / chuyển slot) hoặc nhập vào kho khác.");
            }
        }

        /// <summary>Định mức tối thiểu chỉ là cảnh báo. Nếu dưới định mức (kho hoặc sản phẩm) trả về thông báo để chuyển Manager xem xét; null = đạt định mức.</summary>
        private string? TryGetMinReceiptWeightWarning(GoodsReceipt receipt)
        {
            decimal totalUsableWeight = receipt.Details.Sum(d => d.UsableWeight);
            var warnings = new List<string>();

            // Theo kho: tổng phiếu nhỏ hơn định mức tối thiểu của kho
            decimal? warehouseMin = receipt.Warehouse?.MinReceiptWeight;
            if (warehouseMin.HasValue && warehouseMin.Value > 0 && totalUsableWeight < warehouseMin.Value)
            {
                var warehouseName = receipt.Warehouse?.Name ?? $"Id={receipt.WarehouseId}";
                warnings.Add($"Tổng khối lượng nhập {totalUsableWeight:N2} kg thấp hơn định mức tối thiểu của kho [{warehouseName}] ({warehouseMin.Value:N2} kg).");
            }

            // Theo từng dòng: dòng nào nhỏ hơn định mức tối thiểu của sản phẩm
            foreach (var d in receipt.Details)
            {
                decimal? lineMin = d.ProductVariant?.MinReceiptWeight;
                if (!lineMin.HasValue || lineMin.Value <= 0) continue;
                if (d.UsableWeight < lineMin.Value)
                {
                    var productName = d.ProductVariant?.Name ?? $"Id={d.ProductVariantId}";
                    warnings.Add($"Dòng sản phẩm [{productName}] nhập {d.UsableWeight:N2} kg thấp hơn định mức tối thiểu ({lineMin.Value:N2} kg).");
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

        // ===============================
        // GENERATE BOXES (only after receipt Approved; remainder box; create Import transactions)
        // ===============================
        public async Task GenerateBoxesAsync(CreateBoxesRequest request, string userId)
        {
            var lot = await _lotRepo.GetByIdWithDetailAndReceiptAsync(request.LotId);
            if (lot == null)
                throw new NotFoundException("Lot không tồn tại");
            if (lot.GoodsReceiptDetail?.GoodsReceipt == null)
                throw new InvalidBusinessRuleException("Lot không thuộc phiếu nhập hợp lệ");
            if (lot.GoodsReceiptDetail.GoodsReceipt.Status != GoodsReceiptStatus.Approved)
                throw new InvalidBusinessRuleException("Chỉ được tạo Box sau khi phiếu nhập đã được duyệt (Approved)");

            decimal total = lot.TotalQuantity;
            decimal boxSize = request.BoxSize;
            if (boxSize <= 0)
                throw new InvalidBusinessRuleException("BoxSize phải lớn hơn 0");

            int fullCount = (int)(total / boxSize);
            decimal remainder = total - fullCount * boxSize;
            var boxesToCreate = new List<Box>();
            string baseCode = $"BOX-{DateTime.UtcNow:yyyyMMddHHmmss}";

            for (int i = 0; i < fullCount; i++)
            {
                boxesToCreate.Add(new Box
                {
                    LotId = lot.Id,
                    Weight = boxSize,
                    Status = BoxStatus.Stored,
                    BoxCode = $"{baseCode}-{i + 1}",
                    IsPartial = false
                });
            }
            if (remainder > 0)
            {
                boxesToCreate.Add(new Box
                {
                    LotId = lot.Id,
                    Weight = remainder,
                    Status = BoxStatus.Stored,
                    BoxCode = $"{baseCode}-{fullCount + 1}",
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
            await _unitOfWork.SaveChangesAsync();
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
            if (usable <= 0)
                throw new InvalidBusinessRuleException("UsableWeight phải lớn hơn 0 để tạo Lot");

            var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId);
            if (poDetail == null)
                throw new NotFoundException("Chi tiết đơn mua không tồn tại");

            var shelfLifeDays = poDetail.ProductVariant.ShelfLifeDays;
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
                TotalQuantity = usable,
                RemainingQuantity = usable,
                ReceivedDate = receivedAt,
                ExpiryDate = expiryDate
            };
            await _lotRepo.AddRangeAsync(new List<Lot> { lot });
            await _unitOfWork.SaveChangesAsync();
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
                SupplierName = r.Supplier?.Name ?? string.Empty,
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                ReceivedDate = r.ReceivedDate,
                TotalReceivedWeight = r.TotalReceivedWeight,
                TotalUsableWeight = r.TotalUsableWeight
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
                SupplierName = receipt.Supplier?.Name ?? string.Empty,
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse?.Name ?? string.Empty,
                ReceivedDate = receipt.ReceivedDate,
                TotalReceivedWeight = receipt.TotalReceivedWeight,
                TotalUsableWeight = receipt.TotalUsableWeight
            };

            dto.Details = receipt.Details.Select(d => new GoodsReceiptDetailLineDto
            {
                Id = d.Id,
                ProductVariantId = d.ProductVariantId,
                ProductName = d.ProductVariant?.Product?.Name ?? string.Empty,
                ReceivedWeight = d.ReceivedWeight,
                UsableWeight = d.UsableWeight,
                RejectWeight = d.RejectWeight,
                QCResult = d.QCResult.ToString()
            }).ToList();

            return dto;
        }

        public async Task<GoodsReceiptForApprovalDto> GetByIdForApprovalAsync(int id)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(id)
                ?? throw new NotFoundException("Phiếu nhập không tồn tại");

            var details = receipt.Details.Select(d =>
            {
                var lineTotal = d.UsableWeight * d.UnitPrice;
                return new GoodsReceiptDetailLineForApprovalDto
                {
                    Id = d.Id,
                    ProductVariantId = d.ProductVariantId,
                    ProductName = d.ProductVariant?.Product?.Name ?? string.Empty,
                    ReceivedWeight = d.ReceivedWeight,
                    UsableWeight = d.UsableWeight,
                    RejectWeight = d.RejectWeight,
                    QCResult = d.QCResult.ToString(),
                    UnitPrice = d.UnitPrice,
                    LineTotal = lineTotal
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
                SupplierName = receipt.Supplier?.Name ?? string.Empty,
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse?.Name ?? string.Empty,
                ReceivedDate = receipt.ReceivedDate,
                TotalReceivedWeight = receipt.TotalReceivedWeight,
                TotalUsableWeight = receipt.TotalUsableWeight,
                TotalAmount = details.Sum(x => x.LineTotal),
                Details = details
            };
        }
    }
}
