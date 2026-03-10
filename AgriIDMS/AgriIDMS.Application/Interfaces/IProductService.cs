using AgriIDMS.Application.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IProductService
    {
        Task<int> CreateAsync(CreateProductRequest request);

        Task<IEnumerable<object>> GetAllProducts();

        Task<object> GetByIdAsync(int id);

        Task UpdateAsync(int id, UpdateProductRequest request);

        Task DeleteAsync(int id);
        Task UpdateStatusAsync(int id, UpdateProductStatusRequest request);
    }
}
