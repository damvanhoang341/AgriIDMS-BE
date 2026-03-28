using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface ILotRepository
    {
        Task AddRangeAsync(IEnumerable<Lot> lots);
        Task<Lot?> GetByIdAsync(int id);
        Task<Lot?> GetByIdWithContextAndBoxesAsync(int id);
        Task<Lot?> GetByIdWithDetailAndReceiptAsync(int id);
        Task<List<Lot>> GetByGoodsReceiptIdAsync(int goodsReceiptId);
        Task<Lot?> GetByLotCodeAsync(string lotCode);
        Task<List<Lot>> GetAllWithContextAsync();
        Task<IEnumerable<Lot>> GetAllExpiryDateAsync();
        Task<List<Lot>> GetNearExpiryLotsAsync(int days, int? warehouseId = null);
    }
}
