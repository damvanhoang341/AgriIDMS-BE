using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class GoodsReceiptService
    {
        private readonly IGoodsReceiptRepository _receiptRepo;
        private readonly IGoodsReceiptDetailRepository _detailRepo;
        private readonly ILotRepository _lotRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IProductVariantRepository _productVariantRepo;

        public GoodsReceiptService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            ILotRepository lotRepo,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepo,
            IProductVariantRepository ProductVariantRepository)
        {
            _receiptRepo = receiptRepo;
            _detailRepo = detailRepo;
            _lotRepo = lotRepo;
            _unitOfWork = unitOfWork;
            _supplierRepo = supplierRepository;
            _warehouseRepo = warehouseRepo;
            _productVariantRepo = ProductVariantRepository;
        }

        private string GenerateLotCode(int productVariantId, DateTime receivedDate)
        {
            return $"LOT-{productVariantId}-{receivedDate:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6)}";
        }

        public async Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request,string currentUserId)
        {
                await _unitOfWork.BeginTransactionAsync();

                if (request == null)
                    throw new InvalidBusinessRuleException("Dữ liệu không được để trống");

                if (request.Details == null || !request.Details.Any())
                    throw new InvalidBusinessRuleException("Phiếu nhập phải có ít nhất một sản phẩm.");

            var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId);
            if (supplier == null)
                throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại");

            var warehouse = await _warehouseRepo.GetWarehouseByIdAsync(request.WarehouseId);
            if (warehouse == null)
                throw new InvalidBusinessRuleException("Kho không tồn tại");

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
                    CreatedBy = currentUserId
                };

                await _receiptRepo.AddGoodsReceiptAsync(receipt);
                await _unitOfWork.SaveChangesAsync();

                decimal totalActual = 0;
                decimal totalEstimated = 0;

                foreach (var d in request.Details)
                {
                    if (d.ActualQuantity <= 0)
                        throw new InvalidBusinessRuleException("Số lượng thực tế phải lớn hơn 0");
                var productVariant = await _productVariantRepo.GetProductVariantByIdAsync(d.ProductVariantId);
                if (productVariant == null)
                    throw new InvalidBusinessRuleException("Mặt hàng nông sản không tồn tại");
                var detail = new GoodsReceiptDetail
                    {
                        GoodsReceiptId = receipt.Id,
                        ProductVariantId = d.ProductVariantId,
                        EstimatedQuantity = d.EstimatedQuantity,
                        ActualQuantity = d.ActualQuantity,
                        UnitPrice = d.UnitPrice
                    };

                    await _detailRepo.AddGoodsReceiptDetaiAsync(detail);
                    await _unitOfWork.SaveChangesAsync();

                    totalActual += d.ActualQuantity;
                    totalEstimated += d.EstimatedQuantity;

                    if (d.Lots == null || !d.Lots.Any())
                        throw new InvalidBusinessRuleException("Mỗi sản phẩm phải có ít nhất một lô");

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
                        throw new InvalidBusinessRuleException(
                            $"Tổng số lượng lô không khớp.");

                    await _lotRepo.AddRangeAsync(lots);
                }

                receipt.TotalActualQuantity = totalActual;
                receipt.TotalEstimatedQuantity = totalEstimated;

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                return receipt.Id;
        }

    }
}
