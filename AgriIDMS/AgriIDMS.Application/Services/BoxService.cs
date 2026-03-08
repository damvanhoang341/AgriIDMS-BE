using AgriIDMS.Application.DTOs.Box;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System;
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
    }
}
