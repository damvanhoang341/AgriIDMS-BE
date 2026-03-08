using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IProductVariantRepository
    {
        Task<ProductVariant> GetProductVariantByIdAsync(int productVariantId);
        Task<IReadOnlyDictionary<int, ProductVariant>> GetByIdsAsync(IEnumerable<int> ids);
        Task<IEnumerable<ProductVariant>> GetAllAsync();

        Task AddAsync(ProductVariant variant);

        void Update(ProductVariant variant);

        void Delete(ProductVariant variant);
    }
}
