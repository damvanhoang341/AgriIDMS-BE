using AgriIDMS.Application.DTOs.GoodsReceipt;
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
using System.Text;
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

        public GoodsReceiptService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            ILotRepository lotRepo,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepo,
            IProductVariantRepository ProductVariantRepository,
            ILogger<GoodsReceiptService> logger)
        {
            _receiptRepo = receiptRepo;
            _detailRepo = detailRepo;
            _lotRepo = lotRepo;
            _unitOfWork = unitOfWork;
            _supplierRepo = supplierRepository;
            _warehouseRepo = warehouseRepo;
            _productVariantRepo = ProductVariantRepository;
            _logger = logger;
        }

        private string GenerateLotCode(int productVariantId, DateTime receivedDate)
        {
            return $"LOT-{productVariantId}-{receivedDate:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6)}";
        }


        // Tạo phiếu nhập Draft
        public async Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string currentUserId)
        {
            _logger.LogInformation("Start creating GoodsReceipt by user {UserId}", currentUserId);

            await _unitOfWork.BeginTransactionAsync();

            if (request == null)
            {
                _logger.LogWarning("CreateGoodsReceipt failed: request is null");
                throw new InvalidBusinessRuleException("Dữ liệu không được để trống");
            }

            if (request.Details == null || !request.Details.Any())
            {
                _logger.LogWarning("CreateGoodsReceipt failed: no product details");
                throw new InvalidBusinessRuleException("Phiếu nhập phải có ít nhất một sản phẩm");
            }

            var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier {SupplierId} not found", request.SupplierId);
                throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại");
            }

            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
            if (warehouse == null)
            {
                _logger.LogWarning("Warehouse {WarehouseId} not found", request.WarehouseId);
                throw new InvalidBusinessRuleException("Kho không tồn tại");
            }

            var receipt = new GoodsReceipt
            {
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                VehicleNumber = request.VehicleNumber,
                DriverName = request.DriverName,
                TransportCompany = request.TransportCompany,
                GrossWeight = request.GrossWeight,
                TareWeight = request.TareWeight,
                ReceivedDate = request.ReceivedDate,
                Status = GoodsReceiptStatus.Draft,
                CreatedBy = currentUserId,
                TolerancePercent = request.TolerancePercent
            };

            await _receiptRepo.AddGoodsReceiptAsync(receipt);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created GoodsReceipt draft with Id {ReceiptId}", receipt.Id);

            foreach (var d in request.Details)
            {
                if (d.ActualQuantity <= 0)
                {
                    _logger.LogWarning("Invalid actual quantity for productVariant {ProductVariantId}", d.ProductVariantId);
                    throw new InvalidBusinessRuleException("Số lượng thực tế phải lớn hơn 0");
                }

                var productVariant = await _productVariantRepo.GetProductVariantByIdAsync(d.ProductVariantId);
                if (productVariant == null)
                {
                    _logger.LogWarning("ProductVariant {ProductVariantId} not found", d.ProductVariantId);
                    throw new InvalidBusinessRuleException("Mặt hàng nông sản không tồn tại");
                }

                var detail = new GoodsReceiptDetail
                {
                    GoodsReceiptId = receipt.Id,
                    ProductVariantId = d.ProductVariantId,
                    OrderedWeight = d.EstimatedQuantity,
                    UsableWeight = d.ActualQuantity,
                    UnitPrice = d.UnitPrice
                };

                detail.CalculateRejectWeight();

                await _detailRepo.AddGoodsReceiptDetaiAsync(detail);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created GoodsReceiptDetail for productVariant {ProductVariantId}", d.ProductVariantId);

                if (d.Lots == null || !d.Lots.Any())
                {
                    _logger.LogWarning("Lots missing for productVariant {ProductVariantId}", d.ProductVariantId);
                    throw new InvalidBusinessRuleException("Mỗi sản phẩm phải có ít nhất một lô");
                }

                var lots = d.Lots.Select(l => new Lot
                {
                    LotCode = GenerateLotCode(d.ProductVariantId, request.ReceivedDate),
                    GoodsReceiptDetailId = detail.Id,
                    TotalQuantity = l.Quantity,
                    RemainingQuantity = l.Quantity,
                    ExpiryDate = l.ExpiryDate,
                    ReceivedDate = request.ReceivedDate,
                    Status = LotStatus.Active
                }).ToList();

                if (lots.Sum(x => x.TotalQuantity) != d.ActualQuantity)
                {
                    _logger.LogWarning("Lot quantity mismatch for productVariant {ProductVariantId}", d.ProductVariantId);
                    throw new InvalidBusinessRuleException("Tổng số lượng các lô phải bằng số lượng thực tế");
                }

                await _lotRepo.AddRangeAsync(lots);

                _logger.LogInformation("Created {LotCount} lots for productVariant {ProductVariantId}", lots.Count, d.ProductVariantId);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("GoodsReceipt {ReceiptId} created successfully", receipt.Id);

            return receipt.Id;
        }

        // Duyệt phiếu nhập
        public async Task ApproveGoodsReceiptAsync(int receiptId, string approvedBy)
        {
            _logger.LogInformation("User {UserId} approving GoodsReceipt {ReceiptId}", approvedBy, receiptId);

            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(receiptId);

            if (receipt == null)
            {
                _logger.LogWarning("GoodsReceipt {ReceiptId} not found", receiptId);
                throw new InvalidBusinessRuleException("Phiếu nhập không tồn tại");
            }

            if (receipt.Status != GoodsReceiptStatus.Draft)
            {
                _logger.LogWarning("GoodsReceipt {ReceiptId} is not in Draft status", receiptId);
                throw new InvalidBusinessRuleException("Chỉ có thể duyệt phiếu ở trạng thái Draft");
            }

            foreach (var detail in receipt.Details)
            {
                detail.CalculateRejectWeight();
            }

            receipt.CalculateTotalLossWeight();

            if (receipt.TotalLossWeight > receipt.AllowedLossWeight)
            {
                _logger.LogWarning(
                    "Loss exceeded tolerance for receipt {ReceiptId}. Loss={Loss}, Allowed={Allowed}",
                    receiptId,
                    receipt.TotalLossWeight,
                    receipt.AllowedLossWeight
                );

                throw new InvalidBusinessRuleException(
                    $"Hao hụt {receipt.TotalLossWeight}kg vượt dung sai {receipt.AllowedLossWeight}kg"
                );
            }

            receipt.Status = GoodsReceiptStatus.Approved;
            receipt.ApprovedBy = approvedBy;
            receipt.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("GoodsReceipt {ReceiptId} approved successfully by {UserId}", receiptId, approvedBy);
        }
    }
}
