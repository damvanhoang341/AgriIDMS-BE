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
        private const int DefaultLotExpiryDays = 30;
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
        // CREATE GOODS RECEIPT
        // ===============================
        public async Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string userId)
        {
            _logger.LogInformation("User {UserId} tạo phiếu nhập kho", userId);

            var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId);
            if (supplier == null)
                throw new NotFoundException("Nhà cung cấp không tồn tại");

            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
            if (warehouse == null)
                throw new NotFoundException("Kho không tồn tại");

            var receipt = new GoodsReceipt
            {
                ReceiptCode = await _receiptRepo.GenerateReceiptCodeAsync(),
                PurchaseOrderId = request.PurchaseOrderId,
                SupplierId = request.SupplierId,
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
            return receipt.Id;
        }

        // ===============================
        // ADD DETAIL (Warehouse: only ReceivedWeight; UnitPrice from PO; ExpectedWeight = PO.OrderedWeight)
        // ===============================
        public async Task AddGoodsReceiptDetailAsync(AddGoodsReceiptDetailRequest request)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(request.GoodsReceiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.Status != GoodsReceiptStatus.Draft)
                throw new InvalidBusinessRuleException("Chỉ được thêm chi tiết khi phiếu nhập ở trạng thái Nháp (Draft)");

            var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(request.PurchaseOrderDetailId);
            if (poDetail == null)
                throw new NotFoundException("Chi tiết đơn mua không tồn tại");
            if (poDetail.PurchaseOrder.Status != PurchaseOrderStatus.Approved)
                throw new InvalidBusinessRuleException("Đơn mua chưa được duyệt, chỉ nhập hàng theo PO đã duyệt");
            if (receipt.SupplierId != poDetail.PurchaseOrder.SupplierId)
                throw new InvalidBusinessRuleException("Phiếu nhập phải cùng nhà cung cấp với đơn mua");
            if (request.ProductVariantId != poDetail.ProductVariantId)
                throw new InvalidBusinessRuleException("Sản phẩm không khớp với dòng đơn mua");

            var detail = new GoodsReceiptDetail
            {
                GoodsReceiptId = request.GoodsReceiptId,
                PurchaseOrderDetailId = request.PurchaseOrderDetailId,
                ProductVariantId = poDetail.ProductVariantId,
                ReceivedWeight = request.ReceivedWeight,
                UsableWeight = request.ReceivedWeight,
                UnitPrice = poDetail.UnitPrice
            };

            await _detailRepo.AddGoodsReceiptDetaiAsync(detail);
            await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // UPDATE TRUCK WEIGHT (GrossWeight > TareWeight)
        // ===============================
        public async Task UpdateTruckWeightAsync(UpdateTruckWeightRequest request)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(request.GoodsReceiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (request.GrossWeight <= request.TareWeight)
                throw new InvalidBusinessRuleException("Cân xe đầy (GrossWeight) phải lớn hơn cân xe rỗng (TareWeight)");

            receipt.GrossWeight = request.GrossWeight;
            receipt.TareWeight = request.TareWeight;
            await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // QC INSPECTION (no Lot creation here; Lot created at Approve)
        // ===============================
        public async Task QCInspectionAsync(QCInspectionRequest request, string userId)
        {
            var detail = await _detailRepo.GetByIdAsync(request.DetailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết phiếu nhập không tồn tại");
            if (request.UsableWeight > detail.ReceivedWeight)
                throw new InvalidBusinessRuleException("Khối lượng sử dụng được (UsableWeight) không được vượt quá khối lượng thực nhận (ReceivedWeight)");

            detail.UsableWeight = request.UsableWeight;
            detail.QCResult = Enum.TryParse<QCResult>(request.QCResult, out var result) ? result
                : throw new InvalidBusinessRuleException("QCResult không hợp lệ");
            detail.QCNote = request.QCNote;
            detail.InspectedBy = userId;
            detail.InspectedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
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
                if (receipt.Status != GoodsReceiptStatus.Draft)
                    throw new InvalidBusinessRuleException("Chỉ được duyệt phiếu nhập ở trạng thái Nháp (Draft)");

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
                    receipt.ApprovedBy = userId;
                    receipt.ApprovedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Receipt {ReceiptId} vượt dung sai, chuyển PendingManagerApproval", receiptId);
                    return;
                }

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
                if (detail.UsableWeight <= 0) continue;

                var lot = new Lot
                {
                    LotCode = $"LOT-{DateTime.UtcNow.Ticks}-{detail.Id}",
                    GoodsReceiptDetailId = detail.Id,
                    TotalQuantity = detail.UsableWeight,
                    RemainingQuantity = detail.UsableWeight,
                    ReceivedDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(DefaultLotExpiryDays)
                };
                await _lotRepo.AddRangeAsync(new List<Lot> { lot });

                // Chỉ cập nhật PO.ReceivedWeight khi phiếu nhập được duyệt (Approved)
                var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId);
                if (poDetail != null)
                {
                    poDetail.ReceivedWeight += detail.UsableWeight;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            receipt.Status = GoodsReceiptStatus.Approved;
            receipt.ApprovedBy = userId;
            receipt.ApprovedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
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
                    BoxCode = $"{baseCode}-{i + 1}"
                });
            }
            if (remainder > 0)
            {
                boxesToCreate.Add(new Box
                {
                    LotId = lot.Id,
                    Weight = remainder,
                    Status = BoxStatus.Stored,
                    BoxCode = $"{baseCode}-{fullCount + 1}"
                });
            }

            foreach (var box in boxesToCreate)
            {
                await _boxRepo.CreateAsync(box);
            }
            await _unitOfWork.SaveChangesAsync();

            foreach (var box in boxesToCreate)
            {
                await _inventoryTranRepo.CreadAsyn(new InventoryTransaction
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

            var lot = new Lot
            {
                LotCode = $"LOT-{DateTime.UtcNow.Ticks}",
                GoodsReceiptDetailId = goodsReceiptDetailId,
                TotalQuantity = usable,
                RemainingQuantity = usable,
                ReceivedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(DefaultLotExpiryDays)
            };
            await _lotRepo.AddRangeAsync(new List<Lot> { lot });
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
