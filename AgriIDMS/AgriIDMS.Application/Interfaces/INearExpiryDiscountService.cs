using AgriIDMS.Application.DTOs.Discount;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface INearExpiryDiscountService
    {
        Task<NearExpiryDiscountResultDto> CalculateForVariantAsync(
            int productVariantId,
            decimal baseUnitPrice,
            bool includeOfflineOnly = false);
    }
}
