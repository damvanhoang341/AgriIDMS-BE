using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class SlotService
    {
        private readonly ISlotRepository _slotRepository;
        private readonly IRackRepository _rackRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SlotService(
            ISlotRepository slotRepository,
            IRackRepository rackRepository,
            IUnitOfWork unitOfWork)
        {
            _slotRepository = slotRepository;
            _rackRepository = rackRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<SlotDto>> GetByRackAsync(int rackId)
        {
            var slots = await _slotRepository.GetByRackAsync(rackId);

            return slots
                .Select(s => new SlotDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    QrCode = s.QrCode,
                    Capacity = s.Capacity,
                    CurrentCapacity = s.CurrentCapacity,
                    RackId = s.RackId
                })
                .ToList();
        }

        public async Task<int> CreateAsync(int rackId, CreateSlotRequest request)
        {
            var rack = await _rackRepository.GetByIdAsync(rackId);
            if (rack == null)
            {
                throw new NotFoundException("Rack không tồn tại");
            }

            var slot = new Slot
            {
                Code = request.Code.Trim(),
                Capacity = request.Capacity,
                CurrentCapacity = 0,
                RackId = rackId
            };

            await _slotRepository.AddAsync(slot);
            await _unitOfWork.SaveChangesAsync();

            return slot.Id;
        }

        public async Task UpdateAsync(int id, CreateSlotRequest request)
        {
            var slot = await _slotRepository.GetByIdAsync(id);
            if (slot == null)
            {
                throw new NotFoundException("Slot không tồn tại");
            }

            slot.Code = request.Code.Trim();
            slot.Capacity = request.Capacity;

            await _slotRepository.UpdateAsync(slot);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var slot = await _slotRepository.GetByIdAsync(id);
            if (slot == null)
            {
                throw new NotFoundException("Slot không tồn tại");
            }

            await _slotRepository.DeleteAsync(slot);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

