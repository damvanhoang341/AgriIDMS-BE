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
    public class RackService : IRackService
    {
        private readonly IRackRepository _rackRepository;
        private readonly IZoneRepository _zoneRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RackService(
            IRackRepository rackRepository,
            IZoneRepository zoneRepository,
            IUnitOfWork unitOfWork)
        {
            _rackRepository = rackRepository;
            _zoneRepository = zoneRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<RackDto>> GetByZoneAsync(int zoneId)
        {
            var racks = await _rackRepository.GetByZoneAsync(zoneId);

            return racks
                .Select(r => new RackDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    ZoneId = r.ZoneId,
                    LengthM = r.LengthM,
                    WidthM = r.WidthM,
                    FloorAreaM2 = r.FloorAreaM2
                })
                .ToList();
        }

        public async Task<int> CreateAsync(int zoneId, CreateRackRequest request)
        {
            var zone = await _zoneRepository.GetByIdAsync(zoneId);
            if (zone == null)
            {
                throw new NotFoundException("Zone không tồn tại");
            }

            var name = request.Name.Trim();
            if (await _rackRepository.ExistsByNameAsync(zoneId, name))
            {
                throw new InvalidBusinessRuleException("Rack đã tồn tại trong zone này");
            }
            if (!request.LengthM.HasValue || !request.WidthM.HasValue ||
                request.LengthM.Value <= 0 || request.WidthM.Value <= 0)
            {
                throw new InvalidBusinessRuleException("Rack phải có chiều dài và chiều rộng lớn hơn 0.");
            }
            var rackArea = request.FloorAreaM2 ??
                (request.LengthM.Value * request.WidthM.Value);
            if (zone.FloorAreaM2.HasValue)
            {
                var maxRacksArea = zone.FloorAreaM2.Value * 0.7m;
                var existingRacks = await _rackRepository.GetByZoneAsync(zoneId);
                var totalOtherRacksArea = existingRacks.Sum(r => r.FloorAreaM2 ?? (
                    r.LengthM.HasValue && r.WidthM.HasValue ? r.LengthM.Value * r.WidthM.Value : 0m));
                if (totalOtherRacksArea + rackArea > maxRacksArea)
                {
                    throw new InvalidBusinessRuleException("Tổng diện tích các rack không được lớn hơn 70% diện tích zone.");
                }
            }

            var rack = new Rack
            {
                Name = name,
                ZoneId = zoneId,
                LengthM = request.LengthM,
                WidthM = request.WidthM,
                FloorAreaM2 = rackArea
            };

            await _rackRepository.AddAsync(rack);
            await _unitOfWork.SaveChangesAsync();

            return rack.Id;
        }

        public async Task UpdateAsync(int id, CreateRackRequest request)
        {
            var rack = await _rackRepository.GetByIdAsync(id);
            if (rack == null)
            {
                throw new NotFoundException("Rack không tồn tại");
            }

            var name = request.Name.Trim();
            if (!string.Equals(rack.Name, name) &&
                await _rackRepository.ExistsByNameAsync(rack.ZoneId, name))
            {
                throw new InvalidBusinessRuleException("Rack đã tồn tại trong zone này");
            }
            if (!request.LengthM.HasValue || !request.WidthM.HasValue ||
                request.LengthM.Value <= 0 || request.WidthM.Value <= 0)
            {
                throw new InvalidBusinessRuleException("Rack phải có chiều dài và chiều rộng lớn hơn 0.");
            }
            var zone = await _zoneRepository.GetByIdAsync(rack.ZoneId);
            if (zone == null)
            {
                throw new NotFoundException("Zone không tồn tại");
            }
            var rackArea = request.FloorAreaM2 ??
                (request.LengthM.Value * request.WidthM.Value);
            if (zone.FloorAreaM2.HasValue)
            {
                var maxRacksArea = zone.FloorAreaM2.Value * 0.7m;
                var existingRacks = await _rackRepository.GetByZoneAsync(rack.ZoneId);
                var totalOtherRacksArea = existingRacks
                    .Where(r => r.Id != rack.Id)
                    .Sum(r => r.FloorAreaM2 ?? (
                        r.LengthM.HasValue && r.WidthM.HasValue ? r.LengthM.Value * r.WidthM.Value : 0m));
                if (totalOtherRacksArea + rackArea > maxRacksArea)
                {
                    throw new InvalidBusinessRuleException("Tổng diện tích các rack không được lớn hơn 70% diện tích zone.");
                }
            }

            rack.Name = name;
            rack.LengthM = request.LengthM;
            rack.WidthM = request.WidthM;
            rack.FloorAreaM2 = rackArea;

            await _rackRepository.UpdateAsync(rack);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var rack = await _rackRepository.GetByIdAsync(id);
            if (rack == null)
            {
                throw new NotFoundException("Rack không tồn tại");
            }

            await _rackRepository.DeleteAsync(rack);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

