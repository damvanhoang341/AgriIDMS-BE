using AgriIDMS.Application.DTOs.GoodsReceipt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IGoodsReceiptService
    {
        Task ApproveGoodsReceiptAsync(int receiptId, string approvedBy);
        Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string currentUserId);
    }
}
