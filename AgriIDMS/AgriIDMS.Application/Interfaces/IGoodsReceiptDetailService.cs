using AgriIDMS.Application.DTOs.GoodsReceipt;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IGoodsReceiptDetailService
    {
        Task AddGoodsReceiptDetailAsync(AddGoodsReceiptDetailRequest request);
        Task UpdateGoodsReceiptDetailAsync(UpdateGoodsReceiptDetailRequest request);
        Task DeleteGoodsReceiptDetailAsync(int detailId);
    }
}

