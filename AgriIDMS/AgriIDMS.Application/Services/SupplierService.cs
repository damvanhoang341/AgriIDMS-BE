using AgriIDMS.Application.DTOs.Supplier;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;

namespace AgriIDMS.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SupplierService(
        ISupplierRepository supplierRepo,
        IUnitOfWork unitOfWork)
        {
            _supplierRepo = supplierRepo;
            _unitOfWork = unitOfWork;
        }

        // Lấy tất cả supplier
        public async Task<IEnumerable<SupplierResponse>> GetAllSuppliersAsync()
        {
            var suppliers = await _supplierRepo.GetAllAsync();

            return suppliers.Select(s => new SupplierResponse
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Phone = s.Phone
            });
        }

        // Lấy supplier theo id
        public async Task<SupplierResponse> GetSupplierByIdAsync(int id)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);

            if (supplier == null)
                throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại"  );

            return new SupplierResponse
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Address = supplier.Address,
                Phone = supplier.Phone
            };
        }

        // Tạo supplier
        public async Task CreateSupplierAsync(CreateSupplierRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidBusinessRuleException("Tên nhà cung cấp không được để trống"  );

            var supplier = new Supplier
            {
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone
            };

            await _supplierRepo.AddAsync(supplier);

            await _unitOfWork.SaveChangesAsync();
        }

        // Cập nhật supplier
        public async Task UpdateSupplierAsync(int id, UpdateSupplierRequest request)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);

            if (supplier == null)
                throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại"  );

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidBusinessRuleException("Tên nhà cung cấp không được để trống"  );

            supplier.Name = request.Name;
            supplier.Address = request.Address;
            supplier.Phone = request.Phone;

            _supplierRepo.Update(supplier);

            await _unitOfWork.SaveChangesAsync();
        }

        // Cập nhật supplier
        public async Task UpdateStatusSupplierAsync(int id, UpdateStatusSupplierRequest request)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);

            if (supplier == null)
                throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại");

            supplier.Status = request.status;

            _supplierRepo.Update(supplier);

            await _unitOfWork.SaveChangesAsync();
        }

        // Xóa supplier
        public async Task DeleteSupplierAsync(int id)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);

            if (supplier == null)
                throw new InvalidBusinessRuleException("Nhà cung cấp không tồn tại"  );

            _supplierRepo.Delete(supplier);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}