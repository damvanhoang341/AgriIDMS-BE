using AgriIDMS.Application.DTOs.Home;
using AgriIDMS.Application.DTOs.ProductVariant;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    /// <summary>Service dữ liệu trang chủ: hiển thị sản phẩm theo luồng Category → Product → ProductVariant.</summary>
    public interface IHomePageService
    {
        Task<ProductVariantResponseCustomerDto> GetDetailAsync(int idProductVariant);
        Task<IEnumerable<ProductVariantResponseCustomerHomeDto>> GetAllProductVariantAsync();
    }
}
