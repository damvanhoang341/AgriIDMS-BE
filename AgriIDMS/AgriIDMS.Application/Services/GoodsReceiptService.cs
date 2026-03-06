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
        private readonly IPurchaseOrderRepository _purchaseOrderRepo;

        public GoodsReceiptService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            ILotRepository lotRepo,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepo,
            IProductVariantRepository ProductVariantRepository,
            ILogger<GoodsReceiptService> logger,
            IPurchaseOrderRepository purchaseOrderRepo)
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
        }

        private string GenerateLotCode(int productVariantId, DateTime receivedDate)
        {
            return $"LOT-{productVariantId}-{receivedDate:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6)}";
        }


        // Tạo phiếu nhập Draft
        public async Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string userId)
        {
            _logger.LogInformation("Bắt đầu tạo phiếu nhập kho bởi người dùng {UserId}", userId);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (request == null)
                    throw new InvalidBusinessRuleException("Dữ liệu phiếu nhập không hợp lệ");

                if (request.Details == null || !request.Details.Any())
                    throw new InvalidBusinessRuleException("Phiếu nhập phải có ít nhất một sản phẩm");

                var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId);
                if (supplier == null)
                {
                    _logger.LogWarning("Không tìm thấy nhà cung cấp {SupplierId}", request.SupplierId);
                    throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại");
                }

                var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
                if (warehouse == null)
                {
                    _logger.LogWarning("Không tìm thấy kho {WarehouseId}", request.WarehouseId);
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
                    TolerancePercent = request.TolerancePercent,
                    CreatedBy = userId,
                    ReceivedDate = DateTime.UtcNow,
                    Status = GoodsReceiptStatus.Draft
                };

                await _receiptRepo.AddGoodsReceiptAsync(receipt);

                foreach (var item in request.Details)
                {
                    var poDetail = await _purchaseOrderRepo.GetByIdAsync(item.PurchaseOrderDetailId);

                    if (poDetail == null)
                    {
                        _logger.LogWarning("Không tìm thấy chi tiết đơn mua {PurchaseOrderDetailId}", item.PurchaseOrderDetailId);
                        throw new InvalidBusinessRuleException("Chi tiết đơn mua không tồn tại");
                    }

                    if (poDetail.Status != PurchaseOrderStatus.Approved)
                    {
                        _logger.LogWarning("Đơn mua {PurchaseOrderId} chưa được duyệt", poDetail.Id);
                        throw new InvalidBusinessRuleException("Đơn mua chưa được duyệt, không thể nhập kho");
                    }

                    var productVariant = await _productVariantRepo.GetProductVariantByIdAsync(item.ProductVariantId);

                    if (productVariant == null)
                    {
                        _logger.LogWarning("Không tìm thấy sản phẩm {ProductVariantId}", item.ProductVariantId);
                        throw new InvalidBusinessRuleException("Sản phẩm không tồn tại");
                    }

                    if (item.OrderedWeight <= 0)
                        throw new InvalidBusinessRuleException("Số lượng nhập phải lớn hơn 0");

                    var detail = new GoodsReceiptDetail
                    {
                        GoodsReceiptId = receipt.Id,
                        PurchaseOrderDetailId = item.PurchaseOrderDetailId,
                        ProductVariantId = item.ProductVariantId,
                        OrderedWeight = item.OrderedWeight,
                        UnitPrice = item.UnitPrice
                    };

                    await _detailRepo.AddGoodsReceiptDetaiAsync(detail);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Tạo phiếu nhập kho {ReceiptId} thành công", receipt.Id);

                return receipt.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phiếu nhập kho");

                await _unitOfWork.RollbackAsync();

                throw;
            }
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
