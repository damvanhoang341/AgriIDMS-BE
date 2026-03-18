using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class GoodsReceiptDetailRepository : IGoodsReceiptDetailRepository
    {
        private readonly AppDbContext _context;

        public GoodsReceiptDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddGoodsReceiptDetaiAsync(GoodsReceiptDetail entity)
        {
            await _context.GoodsReceiptDetails.AddAsync(entity);
        }

        public async Task<GoodsReceiptDetail?> GetByIdAsync(int id)
        {
            return await _context.GoodsReceiptDetails.FindAsync(id);
        }

        public async Task<decimal> GetTotalReceivedWeightForPurchaseOrderDetailInDraftOrPendingAsync(int purchaseOrderDetailId)
        {
            var sum = await _context.GoodsReceiptDetails
                .Where(d => d.PurchaseOrderDetailId == purchaseOrderDetailId
                    && (d.GoodsReceipt.Status == GoodsReceiptStatus.Draft
                        || d.GoodsReceipt.Status == GoodsReceiptStatus.Received
                        || d.GoodsReceipt.Status == GoodsReceiptStatus.QCCompleted
                        || d.GoodsReceipt.Status == GoodsReceiptStatus.PendingManagerApprovalQc
                        || d.GoodsReceipt.Status == GoodsReceiptStatus.PendingManagerApproval))
                .SumAsync(d => d.ReceivedWeight);
            return sum;
        }

        public void Remove(GoodsReceiptDetail entity)
        {
            _context.GoodsReceiptDetails.Remove(entity);
        }
    }
}
