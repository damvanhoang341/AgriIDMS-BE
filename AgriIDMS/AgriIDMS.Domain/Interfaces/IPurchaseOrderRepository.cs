using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IPurchaseOrderRepository
    {
        Task AddAsync(PurchaseOrder order);

        Task<PurchaseOrder?> GetByIdAsync(int id);
        Task<PurchaseOrder?> GetByIdWithGoodsReceiptsAsync(int id);
        Task<PurchaseOrderDetail?> GetDetailByIdAsync(int purchaseOrderDetailId);
        Task UpdateAsync(PurchaseOrder purchaseOrder);
        Task DeleteAsync(PurchaseOrder purchaseOrder);
        void RemoveDetails(IEnumerable<PurchaseOrderDetail> details);
        Task<string> GenerateOrderCodeAsync();
    }
}
