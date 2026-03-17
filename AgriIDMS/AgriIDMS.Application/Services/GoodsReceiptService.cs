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
            IInventoryTransactionRepository inventoryTranRepo)
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
                        ProductVariantId = line.ProductVariantId,
                        ReceivedWeight = line.ReceivedWeight
                    };

                    await _detailService.AddGoodsReceiptDetailAsync(addDetailRequest);
                }
            }

            return receipt.Id;
        }


        // ===============================
        // QC INSPECTION (3.6: Failed => UsableWeight = 0; sau QC check dung sai + định mức, set QCCompleted hoặc PendingManagerApproval)
        // ===============================
        public async Task QCInspectionAsync(QCInspectionRequest request, string userId)
        {
            var detail = await _detailRepo.GetByIdAsync(request.DetailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết phiếu nhập không tồn tại");

            // Không cho QC khi phiếu đang chờ Manager duyệt (PendingManagerApproval / PendingManagerApprovalQc)
            var parentReceipt = await _receiptRepo.GetGoodsReceiptByIdAsync(detail.GoodsReceiptId);
            if (parentReceipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (parentReceipt.Status == GoodsReceiptStatus.PendingManagerApproval ||
                parentReceipt.Status == GoodsReceiptStatus.PendingManagerApprovalQc)
                throw new InvalidBusinessRuleException("Phiếu nhập đang chờ Manager duyệt, không được QC. Vui lòng đợi Manager xử lý.");

            if (request.UsableWeight > detail.ReceivedWeight)
                throw new InvalidBusinessRuleException("Khối lượng sử dụng được (UsableWeight) không được vượt quá khối lượng thực nhận (ReceivedWeight)");

            // Nếu client truyền RejectWeight thì validate = ReceivedWeight - UsableWeight (không âm)
            var expectedReject = Math.Max(0, detail.ReceivedWeight - request.UsableWeight);
            if (request.RejectWeight.HasValue && request.RejectWeight.Value != expectedReject)
                throw new InvalidBusinessRuleException($"Khối lượng loại bỏ (RejectWeight) phải bằng ReceivedWeight - UsableWeight = {expectedReject:N2} kg.");

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

            // Sau khi QC xong 1 dòng: nếu tất cả dòng đã QC, kiểm tra dung sai + định mức tối thiểu để quyết định QCCompleted hay PendingManagerApproval
            var receipt = await _receiptRepo.GetGoodsReceiptWithDetailsAsync(detail.GoodsReceiptId);
            if (receipt == null) return;

            // Nếu vẫn còn dòng chưa QC thì chưa kết luận
            if (receipt.Details.Any(d => d.QCResult == QCResult.Pending))
                return;

            bool toleranceExceeded = CheckToleranceExceeded(receipt);
            string? minReceiptWarning = TryGetMinReceiptWeightWarning(receipt);

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
            }
            else
            {
                // Tất cả dòng đã QC và nằm trong dung sai + đạt định mức tối thiểu
                receipt.Status = GoodsReceiptStatus.QCCompleted;
                receipt.PendingReason = null;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // ===============================
        // APPROVE RECEIPT (sau khi QC xong & đã xử lý dung sai/định mức; tạo Lots; không còn check dung sai ở đây)
        // ===============================
        public async Task ApproveGoodsReceiptAsync(int receiptId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var receipt = await _receiptRepo.GetGoodsReceiptForApproveAsync(receiptId);
                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");
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
        // MANAGER ALLOW QC (wrapper: luôn Approve ngoại lệ định mức tối thiểu, cho phép quay lại flow QC/Approve)
        // ===============================
        public async Task ManagerAllowQcAsync(int receiptId, string userId)
        {
            await ManagerReviewMinWeightAsync(receiptId, true, userId);
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
                // Nếu còn dòng QCResult = Pending → đưa về Received để QC tiếp; nếu không còn dòng Pending → đưa về QCCompleted.
                bool hasPendingQc = receipt.Details.Any(d => d.QCResult == QCResult.Pending);
                receipt.Status = hasPendingQc ? GoodsReceiptStatus.Received : GoodsReceiptStatus.QCCompleted;
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

        // ===============================
        // MANAGER REVIEW TOLERANCE (Approve / Reject khi status = PendingManagerApproval - vượt dung sai)
        // ===============================
        public async Task ManagerReviewToleranceAsync(int receiptId, bool isApproved, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
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

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
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
