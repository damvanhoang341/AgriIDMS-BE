using AgriIDMS.Application.DTOs.Supplier;
using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface ISupplierService
    {
        Task<IEnumerable<SupplierResponse>> GetAllSuppliersAsync();

        Task<SupplierResponse> GetSupplierByIdAsync(int id);

        Task CreateSupplierAsync(CreateSupplierRequest request);

        Task UpdateSupplierAsync(int id, UpdateSupplierRequest request);

        Task DeleteSupplierAsync(int id);
        Task UpdateStatusSupplierAsync(int id, UpdateStatusSupplierRequest request);
    }
}
