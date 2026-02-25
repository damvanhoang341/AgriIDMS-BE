using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IGoodsReceiptRepository
    {
        Task<IEnumerable<GoodsReceipt>> GetAllGoodsReceiptsAsync();
        Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(string goodsReceiptId);
        Task AddGoodsReceiptAsync(GoodsReceipt goodsReceipt);
        Task UpdateGoodsReceiptAsync(GoodsReceipt goodsReceipt);
        Task DeleteGoodsReceiptAsync(string goodsReceiptId);
    }
}
