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
    public class RackService
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
                    ZoneId = r.ZoneId
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

            var rack = new Rack
            {
                Name = name,
                ZoneId = zoneId
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

            rack.Name = name;

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

