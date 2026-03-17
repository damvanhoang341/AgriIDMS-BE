using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseService(
            IWarehouseRepository warehouseRepository,
            IUnitOfWork unitOfWork)
        {
            _warehouseRepository = warehouseRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateAsync(CreateWarehouseRequest request)
        {
            if (request == null)
            {
                throw new InvalidBusinessRuleException("Dữ liệu kho không được để trống");
            }

            var normalizedName = request.Name.Trim();

            if (await _warehouseRepository.ExistsByNameAsync(normalizedName))
            {
                throw new InvalidBusinessRuleException("Tên kho đã tồn tại");
            }

            // ✅ Validate loại kho
            if (request.TitleWarehouse == TitleWarehouse.Normal
                && request.MinColdStorageHours.HasValue)
            {
                throw new InvalidBusinessRuleException(
                    "Kho thường không được có thời gian bảo quản lạnh"
                );
            }

            if (request.TitleWarehouse == TitleWarehouse.Cold)
            {
                if (!request.MinColdStorageHours.HasValue || request.MinColdStorageHours <= 0)
                {
                    throw new InvalidBusinessRuleException(
                        "Kho lạnh phải có thời gian bảo quản tối thiểu > 0"
                    );
                }
            }

            var warehouse = new Warehouse
            {
                Name = normalizedName,
                Location = request.Location.Trim(),
                TitleWarehouse = request.TitleWarehouse,
                MinColdStorageHours = request.TitleWarehouse == TitleWarehouse.Cold
                    ? (request.MinColdStorageHours ?? 48)
                    : null,
                MinReceiptWeight = request.MinReceiptWeight
            };

            await _warehouseRepository.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            return warehouse.Id;
        }

        public async Task<List<WarehouseDto>> GetAllAsync()
        {
            var warehouses = await _warehouseRepository.GetAllAsync();

            return warehouses
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Location = w.Location,
                    TitleWarehouse = w.TitleWarehouse,
                    MinColdStorageHours = w.MinColdStorageHours,
                    MinReceiptWeight = w.MinReceiptWeight
                })
                .ToList();
        }

        public async Task<WarehouseDto> GetByIdAsync(int id)
        {
            var warehouse = await _warehouseRepository.GetWarehouseByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Kho không tồn tại");
            }

            return new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Location = warehouse.Location,
                TitleWarehouse = warehouse.TitleWarehouse,
                MinColdStorageHours = warehouse.MinColdStorageHours,
                MinReceiptWeight = warehouse.MinReceiptWeight
            };
        }

        public async Task UpdateAsync(int id, CreateWarehouseRequest request)
        {
            var warehouse = await _warehouseRepository.GetWarehouseByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Kho không tồn tại");
            }

            var normalizedName = request.Name.Trim();

            if (!string.Equals(warehouse.Name, normalizedName) &&
                await _warehouseRepository.ExistsByNameAsync(normalizedName))
            {
                throw new InvalidBusinessRuleException("Tên kho đã tồn tại");
            }

            warehouse.Name = normalizedName;
            warehouse.Location = request.Location.Trim();
            warehouse.TitleWarehouse = request.TitleWarehouse;
            warehouse.MinColdStorageHours = request.TitleWarehouse == TitleWarehouse.Cold
                ? (request.MinColdStorageHours ?? 48)
                : null;
            warehouse.MinReceiptWeight = request.MinReceiptWeight;

            await _warehouseRepository.UpdateAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var warehouse = await _warehouseRepository.GetWarehouseByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Kho không tồn tại");
            }

            await _warehouseRepository.DeleteAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

