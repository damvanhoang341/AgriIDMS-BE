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
    public class ZoneService
    {
        private readonly IZoneRepository _zoneRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ZoneService(
            IZoneRepository zoneRepository,
            IWarehouseRepository warehouseRepository,
            IUnitOfWork unitOfWork)
        {
            _zoneRepository = zoneRepository;
            _warehouseRepository = warehouseRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ZoneDto>> GetByWarehouseAsync(int warehouseId)
        {
            var zones = await _zoneRepository.GetByWarehouseAsync(warehouseId);

            return zones
                .Select(z => new ZoneDto
                {
                    Id = z.Id,
                    Name = z.Name,
                    WarehouseId = z.WarehouseId
                })
                .ToList();
        }

        public async Task<int> CreateAsync(int warehouseId, CreateZoneRequest request)
        {
            var warehouse = await _warehouseRepository.GetWarehouseByIdAsync(warehouseId);
            if (warehouse == null)
            {
                throw new NotFoundException("Kho không tồn tại");
            }

            var name = request.Name.Trim();
            if (await _zoneRepository.ExistsByNameAsync(warehouseId, name))
            {
                throw new InvalidBusinessRuleException("Zone đã tồn tại trong kho này");
            }

            var zone = new Zone
            {
                Name = name,
                WarehouseId = warehouseId
            };

            await _zoneRepository.AddAsync(zone);
            await _unitOfWork.SaveChangesAsync();

            return zone.Id;
        }

        public async Task UpdateAsync(int id, CreateZoneRequest request)
        {
            var zone = await _zoneRepository.GetByIdAsync(id);
            if (zone == null)
            {
                throw new NotFoundException("Zone không tồn tại");
            }

            var name = request.Name.Trim();
            if (!string.Equals(zone.Name, name) &&
                await _zoneRepository.ExistsByNameAsync(zone.WarehouseId, name))
            {
                throw new InvalidBusinessRuleException("Zone đã tồn tại trong kho này");
            }

            zone.Name = name;

            await _zoneRepository.UpdateAsync(zone);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var zone = await _zoneRepository.GetByIdAsync(id);
            if (zone == null)
            {
                throw new NotFoundException("Zone không tồn tại");
            }

            await _zoneRepository.DeleteAsync(zone);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

