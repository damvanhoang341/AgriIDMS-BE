using AgriIDMS.Application.DTOs.Lot;
using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface ILotService
    {
        Task<List<Lot>> GetLotsByGoodsReceiptIdAsync(int goodsReceiptId);
        Task<IEnumerable<NearExpiryLotDto>> GetNearExpiryLotsAsync();
        Task<NearExpiryDashboardDto> GetNearExpiryDashboardAsync(int days);
    }
}
