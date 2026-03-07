using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
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
        private readonly IInventoryTransactionRepository _InventoryTranRepo;

        public GoodsReceiptService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            ILotRepository lotRepo,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepo,
            IProductVariantRepository ProductVariantRepository,
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
            _productVariantRepo = ProductVariantRepository;
            _logger = logger;
            _purchaseOrderRepo = purchaseOrderRepo;
            _boxRepo = boxRepo;
            _InventoryTranRepo = inventoryTranRepo;
        }

        private string GenerateLotCode(int productVariantId, DateTime receivedDate)
        {
            return $"LOT-{productVariantId}-{receivedDate:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6)}";
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
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                VehicleNumber = request.VehicleNumber,
                DriverName = request.DriverName,
                TransportCompany = request.TransportCompany,
                TolerancePercent = request.TolerancePercent,
                CreatedBy = userId,
                ReceivedDate = DateTime.UtcNow,
                Status = GoodsReceiptStatus.Draft
            };

            await _receiptRepo.AddGoodsReceiptAsync(receipt);
            await _unitOfWork.SaveChangesAsync();
            //try
            //{
            //    await _unitOfWork.SaveChangesAsync();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.InnerException?.Message);
            //    throw;
            //}

            return receipt.Id;
        }

        // ===============================
        // ADD DETAIL
        // ===============================
        public async Task AddGoodsReceiptDetailAsync(AddGoodsReceiptDetailRequest request)
        {
            // Lấy đúng dòng đơn mua theo PurchaseOrderDetailId (không nhầm với PO Id)
            var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(request.PurchaseOrderDetailId);

            if (poDetail == null)
                throw new NotFoundException("Chi tiết đơn mua không tồn tại");

            if (poDetail.PurchaseOrder.Status != PurchaseOrderStatus.Approved)
                throw new InvalidBusinessRuleException("Đơn mua chưa được duyệt, chỉ nhập hàng theo PO đã duyệt");

            // Phiếu nhập phải cùng NCC với đơn mua
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(request.GoodsReceiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.SupplierId != poDetail.PurchaseOrder.SupplierId)
                throw new InvalidBusinessRuleException("Phiếu nhập phải cùng nhà cung cấp với đơn mua");

            // Dòng nhập phải khớp sản phẩm và đơn giá của dòng PO
            if (request.ProductVariantId != poDetail.ProductVariantId)
                throw new InvalidBusinessRuleException("Sản phẩm không khớp với dòng đơn mua");

            var detail = new GoodsReceiptDetail
            {
                GoodsReceiptId = request.GoodsReceiptId,
                PurchaseOrderDetailId = request.PurchaseOrderDetailId,
                ProductVariantId = poDetail.ProductVariantId,
                OrderedWeight = request.OrderedWeight,
                UnitPrice = poDetail.UnitPrice
            };

            await _detailRepo.AddGoodsReceiptDetaiAsync(detail);

            await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // UPDATE TRUCK WEIGHT
        // ===============================
        public async Task UpdateTruckWeightAsync(UpdateTruckWeightRequest request)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(request.GoodsReceiptId);

            if (receipt == null)
                throw new Exception("Phiếu nhập không tồn tại");

            receipt.GrossWeight = request.GrossWeight;
            receipt.TareWeight = request.TareWeight;

            await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // QC INSPECTION
        // ===============================
        public async Task QCInspectionAsync(QCInspectionRequest request, string userId)
        {
            var detail = await _detailRepo.GetByIdAsync(request.DetailId);

            if (detail == null)
                throw new Exception("Chi tiết phiếu nhập không tồn tại");

            detail.UsableWeight = request.UsableWeight;
            detail.QCResult = Enum.TryParse<QCResult>(request.QCResult, out var result) ? result
                                                 : throw new Exception("Invalid QCResult");
            detail.QCNote = request.QCNote;
            detail.InspectedBy = userId;
            detail.InspectedAt = DateTime.UtcNow;

            detail.CalculateRejectWeight();
            
            await _unitOfWork.SaveChangesAsync();

            await GenerateLotAsync(detail.Id);
        }

        // ===============================
        // GENERATE LOT (System)
        // ===============================
        public async Task GenerateLotAsync(int goodsReceiptDetailId)
        {
            var detail = await _detailRepo.GetByIdAsync(goodsReceiptDetailId);

            if (detail == null)
                throw new Exception("Không tìm thấy chi tiết phiếu nhập");

            var lot = new Lot
            {
                LotCode = $"LOT-{DateTime.UtcNow.Ticks}",
                GoodsReceiptDetailId = goodsReceiptDetailId,
                TotalQuantity = detail.UsableWeight??0,
                RemainingQuantity = detail.UsableWeight ?? 0,
                ReceivedDate = DateTime.UtcNow,
                // ExpiryDate is required by DB mapping; default to 30 days if not provided by business flow yet.
                ExpiryDate = DateTime.UtcNow.AddDays(DefaultLotExpiryDays)
            };

            await _lotRepo.AddRangeAsync(new List<Lot> { lot });

            await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // GENERATE BOX (System)
        // ===============================
        public async Task GenerateBoxesAsync(CreateBoxesRequest request)
        {
            var lot = await _lotRepo.GetByIdAsync(request.LotId);

            if (lot == null)
                throw new Exception("Lot không tồn tại");

            int boxCount = (int)(lot.TotalQuantity / request.BoxSize);

            for (int i = 0; i < boxCount; i++)
            {
                var boxCode = $"BOX-{DateTime.Now:yyyyMMddHHmmss}-{i + 1}";
                var box = new Box
                {
                    LotId = lot.Id,
                    Weight = request.BoxSize,
                    Status = BoxStatus.Stored,
                    BoxCode = boxCode
                };

                await _boxRepo.CreateAsync(box);
            }
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }
            //await _unitOfWork.SaveChangesAsync();
        }

        // ===============================
        // APPROVE RECEIPT
        // ===============================

        public async Task ApproveGoodsReceiptAsync(int receiptId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var receipt = await _receiptRepo.GetGoodsReceiptForApproveAsync(receiptId);

                if (receipt == null)
                    throw new NotFoundException("Phiếu nhập không tồn tại");

                if (receipt.Status == GoodsReceiptStatus.Approved)
                    throw new InvalidBusinessRuleException("Phiếu nhập đã được duyệt");

                if (!receipt.Details.Any())
                    throw new InvalidBusinessRuleException("Phiếu nhập chưa có chi tiết");

                foreach (var detail in receipt.Details)
                {
                    if (detail.QCResult == QCResult.Pending)
                        throw new InvalidBusinessRuleException("Có sản phẩm chưa QC");
                }

                // tạo inventory transaction
                foreach (var detail in receipt.Details)
                {
                    foreach (var lot in detail.Lots)
                    {
                        foreach (var box in lot.Boxes)
                        {
                            var inventoryTransaction = new InventoryTransaction
                            {
                                BoxId = box.Id,
                                TransactionType = InventoryTransactionType.Import,
                                Quantity = box.Weight,
                                CreatedBy = userId,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _InventoryTranRepo.CreadAsyn(inventoryTransaction);
                        }
                    }
                }

                receipt.Status = GoodsReceiptStatus.Approved;
                receipt.ApprovedBy = userId;
                receipt.ApprovedAt = DateTime.UtcNow;

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Receipt {ReceiptId} đã được approve bởi {UserId}", receiptId, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();

                _logger.LogError(ex, "Approve receipt lỗi");

                throw;
            }
        }
    }
}
