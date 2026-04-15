using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IProductVariantDiscountOverrideRepository
    {
        Task<ProductVariantDiscountOverride?> GetActiveOverrideForVariantAsync(int productVariantId, DateTime asOfUtc);
        Task<Dictionary<int, ProductVariantDiscountOverride>> GetActiveOverridesByVariantIdsAsync(
            IEnumerable<int> productVariantIds,
            DateTime asOfUtc);
        Task<List<ProductVariantDiscountOverride>> GetAllAsync();
        Task ReplaceAllAsync(IEnumerable<ProductVariantDiscountOverride> overrides);
    }
}
