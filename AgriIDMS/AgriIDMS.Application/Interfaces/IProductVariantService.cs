using AgriIDMS.Application.DTOs.ProductVariant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IProductVariantService
    {
        Task<int> CreateAsync(CreateProductVariantDto dto);

        Task<IEnumerable<ProductVariantResponseDto>> GetAllAsync();

        Task<ProductVariantResponseDto> GetByIdAsync(int id);

        Task UpdateAsync(int id, UpdateProductVariantDto dto);

        Task DeleteAsync(int id);
        Task UpdateStatusAsync(int id, UpdateProductVariantStatusDto dto);

        /// <summary>Manager đặt % giảm giá gần hết hạn cho biến thể (ghi đè config chung).</summary>
        Task SetManualNearExpiryDiscountAsync(int productVariantId, decimal? discountPercent);
    }
}
