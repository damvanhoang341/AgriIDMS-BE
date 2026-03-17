using AgriIDMS.Application.DTOs.GoodsReceipt;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class GoodsReceiptDetailService : IGoodsReceiptDetailService
    {
        private readonly IGoodsReceiptRepository _receiptRepo;
        private readonly IGoodsReceiptDetailRepository _detailRepo;
        private readonly IPurchaseOrderRepository _purchaseOrderRepo;
        private readonly IUnitOfWork _unitOfWork;

        public GoodsReceiptDetailService(
            IGoodsReceiptRepository receiptRepo,
            IGoodsReceiptDetailRepository detailRepo,
            IPurchaseOrderRepository purchaseOrderRepo,
            IUnitOfWork unitOfWork)
        {
            _receiptRepo = receiptRepo;
            _detailRepo = detailRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
            _unitOfWork = unitOfWork;
        }

        // ADD DETAIL (3.1/3.2: không vượt RemainingWeight; 3.3: chi tiết phải thuộc đúng PO của phiếu; 3.4: chuyển Received sau khi thêm)
        public async Task AddGoodsReceiptDetailAsync(AddGoodsReceiptDetailRequest request)
        {
            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(request.GoodsReceiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.Status != GoodsReceiptStatus.Draft && receipt.Status != GoodsReceiptStatus.Received)
                throw new InvalidBusinessRuleException("Chỉ được thêm chi tiết khi phiếu nhập ở trạng thái Nháp (Draft) hoặc Đã nhập số liệu (Received)");

            var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(request.PurchaseOrderDetailId);
            if (poDetail == null)
                throw new NotFoundException("Chi tiết đơn mua không tồn tại");
            if (poDetail.PurchaseOrder.Status != PurchaseOrderStatus.Approved)
                throw new InvalidBusinessRuleException("Đơn mua chưa được duyệt, chỉ nhập hàng theo PO đã duyệt");
            if (receipt.SupplierId != poDetail.PurchaseOrder.SupplierId)
                throw new InvalidBusinessRuleException("Phiếu nhập phải cùng nhà cung cấp với đơn mua");
            if (request.ProductVariantId != poDetail.ProductVariantId)
                throw new InvalidBusinessRuleException("Sản phẩm không khớp với dòng đơn mua");

            // 3.3: Chi tiết phải thuộc đúng đơn mua của phiếu nhập
            if (receipt.PurchaseOrderId.HasValue && poDetail.PurchaseOrderId != receipt.PurchaseOrderId.Value)
                throw new InvalidBusinessRuleException("Chi tiết đơn mua phải thuộc đúng đơn mua của phiếu nhập");

            // 3.1 & 3.2: Tổng đã nhận (đã duyệt) + tổng đang chờ (Draft/Received/QCCompleted/PendingManagerApproval) + khối lượng mới không vượt OrderedWeight
            decimal totalPending = await _detailRepo.GetTotalReceivedWeightForPurchaseOrderDetailInDraftOrPendingAsync(poDetail.Id);
            if (poDetail.ReceivedWeight + totalPending + request.ReceivedWeight > poDetail.OrderedWeight)
                throw new InvalidBusinessRuleException(
                    $"Khối lượng nhận vượt quá số còn lại của dòng đơn mua. Đã nhận: {poDetail.ReceivedWeight}, đang chờ: {totalPending}, đặt hàng: {poDetail.OrderedWeight}.");

            // Check MinReceiptWeight của ProductVariant cho từng dòng: không đạt thì KHÔNG cho tạo detail (không đẩy cho Manager duyệt)
            var minReceiptWeight = poDetail.ProductVariant?.MinReceiptWeight;
            if (minReceiptWeight.HasValue && minReceiptWeight.Value > 0 && request.ReceivedWeight < minReceiptWeight.Value)
            {
                var productName = poDetail.ProductVariant?.Name ?? $"Id={poDetail.ProductVariantId}";
                throw new InvalidBusinessRuleException(
                    $"Dòng sản phẩm [{productName}] phải nhập tối thiểu {minReceiptWeight.Value:N2} kg. Đã nhập {request.ReceivedWeight:N2} kg.");
            }

            var detail = new Domain.Entities.GoodsReceiptDetail
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

            // 3.4: Chuyển trạng thái sang Received khi đã có chi tiết
            if (receipt.Status == GoodsReceiptStatus.Draft)
            {
                receipt.Status = GoodsReceiptStatus.Received;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // UPDATE / DELETE DETAIL (chỉ khi phiếu còn Draft/Received, chưa QC)
        public async Task UpdateGoodsReceiptDetailAsync(UpdateGoodsReceiptDetailRequest request)
        {
            var detail = await _detailRepo.GetByIdAsync(request.DetailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết phiếu nhập không tồn tại");

            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(detail.GoodsReceiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.Status != GoodsReceiptStatus.Draft && receipt.Status != GoodsReceiptStatus.Received && receipt.Status != GoodsReceiptStatus.PendingManagerApproval)
                throw new InvalidBusinessRuleException("Chỉ được sửa chi tiết khi phiếu nhập ở trạng thái Nháp (Draft) hoặc Đã nhập số liệu (Received)");

            if (detail.QCResult != QCResult.Pending)
                throw new InvalidBusinessRuleException("Chỉ được sửa chi tiết khi chưa QC (QCResult = Pending)");

            var poDetail = await _purchaseOrderRepo.GetDetailByIdAsync(detail.PurchaseOrderDetailId);
            if (poDetail == null)
                throw new NotFoundException("Chi tiết đơn mua không tồn tại");

            if (poDetail.PurchaseOrder.Status != PurchaseOrderStatus.Approved)
                throw new InvalidBusinessRuleException("Đơn mua chưa được duyệt, chỉ nhập hàng theo PO đã duyệt");
            if (receipt.SupplierId != poDetail.PurchaseOrder.SupplierId)
                throw new InvalidBusinessRuleException("Phiếu nhập phải cùng nhà cung cấp với đơn mua");
            if (detail.ProductVariantId != poDetail.ProductVariantId)
                throw new InvalidBusinessRuleException("Sản phẩm trên chi tiết phiếu nhập không khớp với dòng đơn mua");

            // Kiểm tra không vượt OrderedWeight:
            // totalPending hiện tại bao gồm cả detail này → trừ ReceivedWeight cũ, cộng ReceivedWeight mới.
            decimal totalPending = await _detailRepo.GetTotalReceivedWeightForPurchaseOrderDetailInDraftOrPendingAsync(poDetail.Id);
            decimal otherPending = totalPending - detail.ReceivedWeight;
            if (poDetail.ReceivedWeight + otherPending + request.ReceivedWeight > poDetail.OrderedWeight)
                throw new InvalidBusinessRuleException(
                    $"Khối lượng nhận vượt quá số còn lại của dòng đơn mua. Đã nhận: {poDetail.ReceivedWeight}, đang chờ (không tính dòng này): {otherPending}, khối lượng mới: {request.ReceivedWeight}, đặt hàng: {poDetail.OrderedWeight}.");

            // Check MinReceiptWeight sau khi sửa: không đạt thì không cho sửa
            var minReceiptWeight = poDetail.ProductVariant?.MinReceiptWeight;
            if (minReceiptWeight.HasValue && minReceiptWeight.Value > 0 && request.ReceivedWeight < minReceiptWeight.Value)
            {
                var productName = poDetail.ProductVariant?.Name ?? $"Id={poDetail.ProductVariantId}";
                throw new InvalidBusinessRuleException(
                    $"Dòng sản phẩm [{productName}] phải nhập tối thiểu {minReceiptWeight.Value:N2} kg. Đã nhập {request.ReceivedWeight:N2} kg.");
            }

            detail.ReceivedWeight = request.ReceivedWeight;
            // Trước khi QC, UsableWeight luôn = ReceivedWeight
            detail.UsableWeight = request.ReceivedWeight;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteGoodsReceiptDetailAsync(int detailId)
        {
            var detail = await _detailRepo.GetByIdAsync(detailId);
            if (detail == null)
                throw new NotFoundException("Chi tiết phiếu nhập không tồn tại");

            var receipt = await _receiptRepo.GetGoodsReceiptByIdAsync(detail.GoodsReceiptId);
            if (receipt == null)
                throw new NotFoundException("Phiếu nhập không tồn tại");
            if (receipt.Status != GoodsReceiptStatus.Draft && receipt.Status != GoodsReceiptStatus.Received)
                throw new InvalidBusinessRuleException("Chỉ được xóa chi tiết khi phiếu nhập ở trạng thái Nháp (Draft) hoặc Đã nhập số liệu (Received)");

            if (detail.QCResult != QCResult.Pending)
                throw new InvalidBusinessRuleException("Chỉ được xóa chi tiết khi chưa QC (QCResult = Pending)");

            _detailRepo.Remove(detail);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

