using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IBoxRepository
    {
        Task<Box?> GetByIdAsync(int id);
        Task<Box?> GetByIdWithLotAndReceiptAsync(int id);
        Task<Dictionary<int, Box>> GetByIdsAsync(IEnumerable<int> ids);
        Task CreateAsync(Box box);
        Task UpdateAsync(Box box);
        Task<List<Box>> GetAvailableBoxesForVariantAsync(int productVariantId);
        Task<Box?> GetByQrCodeAsync(string qrCode);
        Task<int> GetAvailableBoxCountByVariantIdAsync(int productVariantId);
        /// <summary>Lấy tổng hợp các loại box khả dụng (group theo IsPartial & Weight) cho 1 ProductVariant.</summary>
        Task<List<BoxTypeSummary>> GetAvailableBoxTypeSummaryByVariantIdAsync(int productVariantId);
    }
}
