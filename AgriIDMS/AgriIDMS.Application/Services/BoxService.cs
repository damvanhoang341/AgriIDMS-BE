using AgriIDMS.Application.DTOs.Box;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class BoxService : IBoxService
    {
        private readonly IBoxRepository _boxRepo;
        private readonly ISlotRepository _slotRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly IUnitOfWork _unitOfWork;

        public BoxService(
            IBoxRepository boxRepo,
            ISlotRepository slotRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            IUnitOfWork unitOfWork)
        {
            _boxRepo = boxRepo;
            _slotRepo = slotRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _unitOfWork = unitOfWork;
        }

        /// <summary>Gán box vào slot. Kiểm tra cùng warehouse, dung lượng slot; cập nhật CurrentCapacity; nếu kho lạnh thì set PlacedInColdAt.</summary>
        public async Task AssignBoxToSlotAsync(AssignBoxToSlotRequest request)
        {
            var box = await _boxRepo.GetByIdWithLotAndReceiptAsync(request.BoxId);
            if (box == null)
                throw new NotFoundException("Box không tồn tại");

            var slot = await _slotRepo.GetByIdWithWarehouseAsync(request.SlotId);
            if (slot == null)
                throw new NotFoundException("Slot không tồn tại");

            int? boxWarehouseId = box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId;
            if (!boxWarehouseId.HasValue)
                throw new InvalidBusinessRuleException("Box không thuộc phiếu nhập hợp lệ, không xác định được kho");

            int slotWarehouseId = slot.Rack?.Zone?.Warehouse?.Id ?? 0;
            if (slotWarehouseId == 0)
                throw new InvalidBusinessRuleException("Slot không thuộc kho hợp lệ");

            if (boxWarehouseId.Value != slotWarehouseId)
                throw new InvalidBusinessRuleException("Box và slot phải thuộc cùng một kho");

            // Rule: 1 slot chỉ chứa 1 loại sản phẩm (ProductVariant) dựa theo Lot -> GoodsReceiptDetail -> ProductVariantId
            var incomingVariantId = box.Lot?.GoodsReceiptDetail?.ProductVariantId;
            if (!incomingVariantId.HasValue || incomingVariantId.Value <= 0)
                throw new InvalidBusinessRuleException("Không xác định được loại sản phẩm của box");

            var existingVariantId = slot.Boxes
                .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariantId)
                .FirstOrDefault(v => v.HasValue && v.Value > 0);

            if (existingVariantId.HasValue && existingVariantId.Value != incomingVariantId.Value)
                throw new InvalidBusinessRuleException(
                    "Slot này đang chứa sản phẩm khác loại. Mỗi slot chỉ được chứa 1 loại sản phẩm.");

            bool isNewSlot = box.SlotId != request.SlotId;
            if (isNewSlot && slot.CurrentCapacity + box.Weight > slot.Capacity)
                throw new InvalidBusinessRuleException(
                    $"Slot không đủ dung lượng: còn trống {slot.Capacity - slot.CurrentCapacity}, box nặng {box.Weight}.");

            if (isNewSlot && box.SlotId.HasValue)
            {
                var oldSlot = await _slotRepo.GetByIdAsync(box.SlotId.Value);
                if (oldSlot != null)
                {
                    oldSlot.CurrentCapacity = Math.Max(0, oldSlot.CurrentCapacity - box.Weight);
                    await _slotRepo.UpdateAsync(oldSlot);
                }
            }

            box.SlotId = request.SlotId;
            if (slot.Rack?.Zone?.Warehouse != null &&
                slot.Rack.Zone.Warehouse.TitleWarehouse == TitleWarehouse.Cold &&
                !box.PlacedInColdAt.HasValue)
            {
                box.PlacedInColdAt = DateTime.UtcNow;
            }

            if (isNewSlot)
            {
                slot.CurrentCapacity += box.Weight;
                await _slotRepo.UpdateAsync(slot);
            }
            await _boxRepo.UpdateAsync(box);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>Gán nhiều box vào cùng một slot trong một lần. Box và slot phải cùng kho; tổng dung lượng không vượt Capacity.</summary>
        public async Task AssignBoxesToSlotAsync(AssignBoxesToSlotRequest request)
        {
            if (request.BoxIds == null || request.BoxIds.Count == 0)
                throw new InvalidBusinessRuleException("Danh sách BoxId không được để trống");

            var slot = await _slotRepo.GetByIdWithWarehouseAsync(request.SlotId);
            if (slot == null)
                throw new NotFoundException("Slot không tồn tại");

            int slotWarehouseId = slot.Rack?.Zone?.Warehouse?.Id ?? 0;
            if (slotWarehouseId == 0)
                throw new InvalidBusinessRuleException("Slot không thuộc kho hợp lệ");

            var boxes = await _boxRepo.GetByIdsWithLotAndReceiptAsync(request.BoxIds);
            if (boxes.Count != request.BoxIds.Distinct().Count())
                throw new NotFoundException("Một hoặc nhiều Box không tồn tại");

            foreach (var box in boxes)
            {
                int? boxWarehouseId = box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId;
                if (!boxWarehouseId.HasValue)
                    throw new InvalidBusinessRuleException($"Box Id={box.Id} không thuộc phiếu nhập hợp lệ");
                if (boxWarehouseId.Value != slotWarehouseId)
                    throw new InvalidBusinessRuleException($"Box Id={box.Id} và slot phải thuộc cùng một kho");
            }

            // Rule: 1 slot chỉ chứa 1 loại sản phẩm (ProductVariant)
            var incomingVariantId = boxes
                .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariantId)
                .FirstOrDefault(v => v > 0);
            if (incomingVariantId <= 0)
                throw new InvalidBusinessRuleException("Không xác định được loại sản phẩm của box");

            // Tất cả box trong batch phải cùng loại
            bool mixed = boxes.Any(b => (b.Lot?.GoodsReceiptDetail?.ProductVariantId ?? 0) != incomingVariantId);
            if (mixed)
                throw new InvalidBusinessRuleException("Batch box có nhiều loại sản phẩm khác nhau, không thể xếp chung 1 slot.");

            var existingVariantId = slot.Boxes
                .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariantId)
                .FirstOrDefault(v => v.HasValue && v.Value > 0);

            if (existingVariantId.HasValue && existingVariantId.Value != incomingVariantId)
                throw new InvalidBusinessRuleException(
                    "Slot này đang chứa sản phẩm khác loại. Mỗi slot chỉ được chứa 1 loại sản phẩm.");

            // Chỉ tính box đang chuyển sang slot này (chưa ở slot này)
            var boxesToAssign = boxes.Where(b => b.SlotId != request.SlotId).ToList();
            decimal totalWeightToAdd = boxesToAssign.Sum(b => b.Weight);

            if (slot.CurrentCapacity + totalWeightToAdd > slot.Capacity)
                throw new InvalidBusinessRuleException(
                    $"Slot không đủ dung lượng: còn trống {slot.Capacity - slot.CurrentCapacity:N2}, tổng khối lượng {totalWeightToAdd:N2}.");

            // Trừ dung lượng ở các slot cũ (group theo SlotId)
            foreach (var group in boxesToAssign.Where(b => b.SlotId.HasValue).GroupBy(b => b.SlotId!.Value))
            {
                var oldSlot = group.First().Slot;
                if (oldSlot != null)
                {
                    decimal subtract = group.Sum(b => b.Weight);
                    oldSlot.CurrentCapacity = Math.Max(0, oldSlot.CurrentCapacity - subtract);
                    await _slotRepo.UpdateAsync(oldSlot);
                }
            }

            bool isColdWarehouse = slot.Rack?.Zone?.Warehouse != null &&
                slot.Rack.Zone.Warehouse.TitleWarehouse == TitleWarehouse.Cold;

            foreach (var box in boxes)
            {
                box.SlotId = request.SlotId;
                if (isColdWarehouse && !box.PlacedInColdAt.HasValue)
                    box.PlacedInColdAt = DateTime.UtcNow;
                await _boxRepo.UpdateAsync(box);
            }

            slot.CurrentCapacity += totalWeightToAdd;
            await _slotRepo.UpdateAsync(slot);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Chuyển 1 box đã xếp từ slot hiện tại sang slot khác (cùng kho) và ghi InventoryTransactionType.Transfer.
        /// </summary>
        public async Task TransferBoxToSlotAsync(TransferBoxToSlotRequest request, string userId)
        {
            var box = await _boxRepo.GetByIdWithLotAndReceiptAsync(request.BoxId);
            if (box == null)
                throw new NotFoundException("Box không tồn tại");

            if (!box.SlotId.HasValue || box.SlotId.Value <= 0)
                throw new InvalidBusinessRuleException("Box chưa được xếp vào slot nào, không thể chuyển");

            if (box.SlotId.Value == request.ToSlotId)
                throw new InvalidBusinessRuleException("Box đang ở slot này, không cần chuyển");

            var fromSlot = await _slotRepo.GetByIdAsync(box.SlotId.Value);
            if (fromSlot == null)
                throw new NotFoundException("Slot hiện tại của box không tồn tại");

            var toSlot = await _slotRepo.GetByIdWithWarehouseAsync(request.ToSlotId);
            if (toSlot == null)
                throw new NotFoundException("Slot đích không tồn tại");

            int? boxWarehouseId = box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId;
            if (!boxWarehouseId.HasValue)
                throw new InvalidBusinessRuleException("Box không thuộc phiếu nhập hợp lệ, không xác định được kho");

            int toSlotWarehouseId = toSlot.Rack?.Zone?.Warehouse?.Id ?? 0;
            if (toSlotWarehouseId == 0)
                throw new InvalidBusinessRuleException("Slot đích không thuộc kho hợp lệ");

            if (boxWarehouseId.Value != toSlotWarehouseId)
                throw new InvalidBusinessRuleException("Chỉ được chuyển box trong cùng một kho");

            // Rule: 1 slot chỉ chứa 1 loại sản phẩm (ProductVariant)
            var incomingVariantId = box.Lot?.GoodsReceiptDetail?.ProductVariantId;
            if (!incomingVariantId.HasValue || incomingVariantId.Value <= 0)
                throw new InvalidBusinessRuleException("Không xác định được loại sản phẩm của box");

            var existingVariantId = toSlot.Boxes
                .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariantId)
                .FirstOrDefault(v => v.HasValue && v.Value > 0);

            if (existingVariantId.HasValue && existingVariantId.Value != incomingVariantId.Value)
                throw new InvalidBusinessRuleException(
                    "Slot này đang chứa sản phẩm khác loại. Mỗi slot chỉ được chứa 1 loại sản phẩm.");

            if (toSlot.CurrentCapacity + box.Weight > toSlot.Capacity)
                throw new InvalidBusinessRuleException(
                    $"Slot không đủ dung lượng: còn trống {toSlot.Capacity - toSlot.CurrentCapacity:N2}, box nặng {box.Weight:N2}.");

            // Update capacities
            fromSlot.CurrentCapacity = Math.Max(0, fromSlot.CurrentCapacity - box.Weight);
            toSlot.CurrentCapacity += box.Weight;
            await _slotRepo.UpdateAsync(fromSlot);
            await _slotRepo.UpdateAsync(toSlot);

            // Move box
            var fromSlotId = box.SlotId.Value;
            box.SlotId = request.ToSlotId;

            if (toSlot.Rack?.Zone?.Warehouse != null &&
                toSlot.Rack.Zone.Warehouse.TitleWarehouse == TitleWarehouse.Cold &&
                !box.PlacedInColdAt.HasValue)
            {
                box.PlacedInColdAt = DateTime.UtcNow;
            }

            await _boxRepo.UpdateAsync(box);

            // Inventory transaction
            await _inventoryTranRepo.CreateAsync(new InventoryTransaction
            {
                BoxId = box.Id,
                TransactionType = InventoryTransactionType.Transfer,
                FromSlotId = fromSlotId,
                ToSlotId = request.ToSlotId,
                Quantity = box.Weight,
                CreatedBy = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<object?> GetByQrCodeAsync(string qrCode)
        {
            var box = await _boxRepo.GetByQrCodeAsync(qrCode);
            if (box == null) return null;

            return new
            {
                box.Id,
                box.BoxCode,
                box.QRCode,
                box.QrImageUrl,
                box.Weight,
                box.Status,
                box.SlotId,
                WarehouseId = box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId,
                WarehouseName =
                    box.Slot?.Rack?.Zone?.Warehouse?.Name ??
                    box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.Warehouse?.Name,
                SlotCode = box.Slot?.Code,
                LotCode = box.Lot?.LotCode,
                LotId = box.LotId,
                ProductVariantId = box.Lot?.GoodsReceiptDetail?.ProductVariantId,
                ProductVariantName = box.Lot?.GoodsReceiptDetail?.ProductVariant?.Name,
                ProductName = box.Lot?.GoodsReceiptDetail?.ProductVariant?.Product?.Name,
                box.PlacedInColdAt
            };
        }

        public async Task<List<UnassignedBoxDto>> GetUnassignedBoxesByWarehouseAsync(int warehouseId)
        {
            var boxes = await _boxRepo.GetUnassignedBoxesByWarehouseIdAsync(warehouseId);

            return boxes.Select(b => new UnassignedBoxDto
            {
                Id = b.Id,
                BoxCode = b.BoxCode,
                QrCode = b.QRCode,
                QrImageUrl = b.QrImageUrl,
                Weight = b.Weight,
                Status = b.Status.ToString(),
                SlotId = b.SlotId,
                WarehouseId = b.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId,
                WarehouseName = b.Lot?.GoodsReceiptDetail?.GoodsReceipt?.Warehouse?.Name,
                LotId = b.LotId,
                LotCode = b.Lot?.LotCode,
                SlotCode = b.Slot?.Code,
                ProductVariantId = b.Lot?.GoodsReceiptDetail?.ProductVariantId,
                ProductVariantName = b.Lot?.GoodsReceiptDetail?.ProductVariant?.Name,
                ProductName = b.Lot?.GoodsReceiptDetail?.ProductVariant?.Product?.Name,
                PlacedInColdAt = b.PlacedInColdAt
            }).ToList();
        }

        public async Task<List<UnassignedBoxDto>> GetBoxesByGoodsReceiptAsync(int goodsReceiptId)
        {
            var boxes = await _boxRepo.GetByGoodsReceiptIdAsync(goodsReceiptId);
            return boxes.Select(b => new UnassignedBoxDto
            {
                Id = b.Id,
                BoxCode = b.BoxCode,
                QrCode = b.QRCode,
                QrImageUrl = b.QrImageUrl,
                Weight = b.Weight,
                Status = b.Status.ToString(),
                SlotId = b.SlotId,
                WarehouseId = b.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId,
                WarehouseName = b.Lot?.GoodsReceiptDetail?.GoodsReceipt?.Warehouse?.Name,
                LotId = b.LotId,
                LotCode = b.Lot?.LotCode,
                SlotCode = b.Slot?.Code,
                ProductVariantId = b.Lot?.GoodsReceiptDetail?.ProductVariantId,
                ProductVariantName = b.Lot?.GoodsReceiptDetail?.ProductVariant?.Name,
                ProductName = b.Lot?.GoodsReceiptDetail?.ProductVariant?.Product?.Name,
                PlacedInColdAt = b.PlacedInColdAt
            }).ToList();
        }

        public async Task UpdateQrCodeAsync(int boxId, string? qrCode)
        {
            var box = await _boxRepo.GetByIdAsync(boxId);
            if (box == null)
                throw new NotFoundException("Box không tồn tại");

            box.QRCode = string.IsNullOrWhiteSpace(qrCode) ? null : qrCode.Trim();

            await _boxRepo.UpdateAsync(box);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateQrImageUrlAsync(int boxId, string qrImageUrl)
        {
            var box = await _boxRepo.GetByIdAsync(boxId);
            if (box == null)
                throw new NotFoundException("Box không tồn tại");

            box.QrImageUrl = qrImageUrl.Trim();
            await _boxRepo.UpdateAsync(box);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

