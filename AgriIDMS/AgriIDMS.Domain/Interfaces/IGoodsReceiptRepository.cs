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
        Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(int goodsReceiptId);
        Task AddGoodsReceiptAsync(GoodsReceipt goodsReceipt);
        Task UpdateGoodsReceiptAsync(GoodsReceipt goodsReceipt);
        Task DeleteGoodsReceiptAsync(int goodsReceiptId);
    }
}
