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
        private readonly IUnitOfWork _unitOfWork;

        public BoxService(
            IBoxRepository boxRepo,
            ISlotRepository slotRepo,
            IUnitOfWork unitOfWork)
        {
            _boxRepo = boxRepo;
            _slotRepo = slotRepo;
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

        public async Task<object?> GetByQrCodeAsync(string qrCode)
        {
            var box = await _boxRepo.GetByQrCodeAsync(qrCode);
            if (box == null) return null;

            return new
            {
                box.Id,
                box.BoxCode,
                box.QRCode,
                box.Weight,
                box.Status,
                box.SlotId,
                WarehouseId = box.Lot?.GoodsReceiptDetail?.GoodsReceipt?.WarehouseId,
                LotId = box.LotId,
                box.PlacedInColdAt
            };
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
    }
}
