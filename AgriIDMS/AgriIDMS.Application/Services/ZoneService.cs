using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class ZoneService : IZoneService
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
                    WarehouseId = z.WarehouseId,
                    LengthM = z.LengthM,
                    WidthM = z.WidthM,
                    FloorAreaM2 = z.FloorAreaM2
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
            if (!request.LengthM.HasValue || !request.WidthM.HasValue ||
                request.LengthM.Value <= 0 || request.WidthM.Value <= 0)
            {
                throw new InvalidBusinessRuleException("Zone phải có chiều dài và chiều rộng lớn hơn 0.");
            }
            var zoneArea = request.FloorAreaM2 ??
                (request.LengthM.Value * request.WidthM.Value);
            if (warehouse.FloorAreaM2.HasValue)
            {
                var maxZonesArea = warehouse.FloorAreaM2.Value * 0.7m;
                var existingZones = await _zoneRepository.GetByWarehouseAsync(warehouseId);
                var totalOtherZonesArea = existingZones.Sum(z => z.FloorAreaM2 ?? (
                    z.LengthM.HasValue && z.WidthM.HasValue ? z.LengthM.Value * z.WidthM.Value : 0m));
                if (totalOtherZonesArea + zoneArea > maxZonesArea)
                {
                    throw new InvalidBusinessRuleException("Tổng diện tích các zone không được lớn hơn 70% diện tích kho.");
                }
            }

            var zone = new Zone
            {
                Name = name,
                WarehouseId = warehouseId,
                LengthM = request.LengthM,
                WidthM = request.WidthM,
                FloorAreaM2 = zoneArea
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
            if (!request.LengthM.HasValue || !request.WidthM.HasValue ||
                request.LengthM.Value <= 0 || request.WidthM.Value <= 0)
            {
                throw new InvalidBusinessRuleException("Zone phải có chiều dài và chiều rộng lớn hơn 0.");
            }
            var warehouse = await _warehouseRepository.GetWarehouseByIdAsync(zone.WarehouseId);
            if (warehouse == null)
            {
                throw new NotFoundException("Kho không tồn tại");
            }
            var zoneArea = request.FloorAreaM2 ??
                (request.LengthM.Value * request.WidthM.Value);
            if (warehouse.FloorAreaM2.HasValue)
            {
                var maxZonesArea = warehouse.FloorAreaM2.Value * 0.7m;
                var existingZones = await _zoneRepository.GetByWarehouseAsync(zone.WarehouseId);
                var totalOtherZonesArea = existingZones
                    .Where(z => z.Id != zone.Id)
                    .Sum(z => z.FloorAreaM2 ?? (
                        z.LengthM.HasValue && z.WidthM.HasValue ? z.LengthM.Value * z.WidthM.Value : 0m));
                if (totalOtherZonesArea + zoneArea > maxZonesArea)
                {
                    throw new InvalidBusinessRuleException("Tổng diện tích các zone không được lớn hơn 70% diện tích kho.");
                }
            }

            zone.Name = name;
            zone.LengthM = request.LengthM;
            zone.WidthM = request.WidthM;
            zone.FloorAreaM2 = zoneArea;

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

