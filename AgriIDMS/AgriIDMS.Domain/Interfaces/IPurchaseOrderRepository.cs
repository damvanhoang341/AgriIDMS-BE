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
        Task<PurchaseOrderDetail?> GetDetailByIdAsync(int purchaseOrderDetailId);
        Task UpdateAsync(PurchaseOrder purchaseOrder);
        Task<string> GenerateOrderCodeAsync();
    }
}
