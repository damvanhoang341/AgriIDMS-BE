using AgriIDMS.Application.DTOs.PurchaseOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<int> CreateAsync(CreatePurchaseOrderRequest request, string userId);

        Task<PurchaseOrderResponse> GetByIdAsync(int id);
        Task ApprovePurchaseOrderAsync(int id, string userId);
    }
}
