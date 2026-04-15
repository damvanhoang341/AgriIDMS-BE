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
        private readonly IBoxRepository _boxRepository;
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseService(
            IWarehouseRepository warehouseRepository,
            IBoxRepository boxRepository,
            IUnitOfWork unitOfWork)
        {
            _warehouseRepository = warehouseRepository;
            _boxRepository = boxRepository;
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
                if (!request.MinColdStorageHours.HasValue || request.MinColdStorageHours < 0)
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
                LengthM = request.LengthM,
                WidthM = request.WidthM,
                FloorAreaM2 = request.FloorAreaM2 ?? (
                    request.LengthM.HasValue && request.WidthM.HasValue
                        ? request.LengthM.Value * request.WidthM.Value
                        : null),
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
            var result = new List<WarehouseDto>(warehouses.Count);

            foreach (var w in warehouses)
            {
                var totalCapacity = await _warehouseRepository.GetTotalCapacityByWarehouseIdAsync(w.Id);
                var storedInSlots = await _boxRepository.GetAssignedStockVolumeByWarehouseIdAsync(w.Id);
                var unassignedWeight = await _boxRepository.GetUnassignedStockVolumeByWarehouseIdAsync(w.Id);
                var totalStock = storedInSlots + unassignedWeight;

                result.Add(new WarehouseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Location = w.Location,
                    TitleWarehouse = w.TitleWarehouse,
                    LengthM = w.LengthM,
                    WidthM = w.WidthM,
                    FloorAreaM2 = w.FloorAreaM2,
                    MinColdStorageHours = w.MinColdStorageHours,
                    MinReceiptWeight = w.MinReceiptWeight,
                    TotalCapacity = totalCapacity,
                    StoredInSlotsWeight = storedInSlots,
                    UnassignedStockWeight = unassignedWeight,
                    TotalStockWeight = totalStock
                });
            }

            return result;
        }

        public async Task<WarehouseDto> GetByIdAsync(int id)
        {
            var warehouse = await _warehouseRepository.GetWarehouseByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Kho không tồn tại");
            }

            var totalCapacity = await _warehouseRepository.GetTotalCapacityByWarehouseIdAsync(warehouse.Id);
            var storedInSlots = await _boxRepository.GetAssignedStockVolumeByWarehouseIdAsync(warehouse.Id);
            var unassignedWeight = await _boxRepository.GetUnassignedStockVolumeByWarehouseIdAsync(warehouse.Id);
            var totalStock = storedInSlots + unassignedWeight;

            return new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Location = warehouse.Location,
                TitleWarehouse = warehouse.TitleWarehouse,
                LengthM = warehouse.LengthM,
                WidthM = warehouse.WidthM,
                FloorAreaM2 = warehouse.FloorAreaM2,
                MinColdStorageHours = warehouse.MinColdStorageHours,
                MinReceiptWeight = warehouse.MinReceiptWeight,
                TotalCapacity = totalCapacity,
                StoredInSlotsWeight = storedInSlots,
                UnassignedStockWeight = unassignedWeight,
                TotalStockWeight = totalStock
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

            // Chỉ cho phép cập nhật thông tin kho khi kho không còn hàng.
            var assignedVolume = await _boxRepository.GetAssignedStockVolumeByWarehouseIdAsync(id);
            var unassignedVolume = await _boxRepository.GetUnassignedStockVolumeByWarehouseIdAsync(id);
            var assignedWeight = await _boxRepository.GetAssignedStockWeightByWarehouseIdAsync(id);
            var unassignedWeight = await _boxRepository.GetUnassignedStockWeightByWarehouseIdAsync(id);
            var hasAnyStock = (assignedVolume + unassignedVolume) > 0m || (assignedWeight + unassignedWeight) > 0m;
            if (hasAnyStock)
            {
                throw new InvalidBusinessRuleException("Chỉ được cập nhật kho khi trong kho không có sản phẩm.");
            }

            warehouse.Name = normalizedName;
            warehouse.Location = request.Location.Trim();
            warehouse.TitleWarehouse = request.TitleWarehouse;
            warehouse.LengthM = request.LengthM;
            warehouse.WidthM = request.WidthM;
            warehouse.FloorAreaM2 = request.FloorAreaM2 ?? (
                request.LengthM.HasValue && request.WidthM.HasValue
                    ? request.LengthM.Value * request.WidthM.Value
                    : null);
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

