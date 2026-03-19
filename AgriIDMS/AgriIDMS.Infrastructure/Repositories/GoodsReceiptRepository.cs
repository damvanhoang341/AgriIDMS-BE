using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class GoodsReceiptRepository : IGoodsReceiptRepository
    {
        private readonly AppDbContext _context;
        public GoodsReceiptRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddGoodsReceiptAsync(GoodsReceipt goodsReceipt)
        {
            await _context.GoodsReceipts.AddAsync(goodsReceipt);
        }

        public async Task DeleteGoodsReceiptAsync(int goodsReceiptId)
        {
            var goodsReceipt = await _context.GoodsReceipts.FindAsync(goodsReceiptId);
            if (goodsReceipt != null)
            {
                _context.GoodsReceipts.Remove(goodsReceipt);
            }
            else return;
        }

        public async Task<IEnumerable<GoodsReceipt>> GetAllGoodsReceiptsAsync()
        {
            return await _context.GoodsReceipts
                .AsNoTracking()
                .Include(r => r.CreatedUser)
                .Include(r => r.Supplier)
                .Include(r => r.Warehouse)
                .Include(r => r.PurchaseOrder)
                .ToListAsync();
        }

        public async Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(int goodsReceiptId)
        {
            // Tracking entity so services can update fields (truck weight, status, approve metadata, ...)
            return await _context.GoodsReceipts.FirstOrDefaultAsync(x => x.Id == goodsReceiptId);
        }

        public async Task<GoodsReceipt?> GetGoodsReceiptWithDetailsAsync(int goodsReceiptId)
        {
            return await _context.GoodsReceipts
                .Include(r => r.CreatedUser)
                .Include(r => r.Supplier)
                .Include(r => r.Warehouse)
                .Include(r => r.PurchaseOrder)
                .Include(r => r.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(r => r.Details)
                    .ThenInclude(d => d.PurchaseOrderDetail)
                .FirstOrDefaultAsync(r => r.Id == goodsReceiptId);
        }

        public async Task<GoodsReceipt?> GetGoodsReceiptForApproveAsync(int goodsReceiptId)
        {
            return await _context.GoodsReceipts
                .Include(r => r.CreatedUser)
                .Include(r => r.Warehouse)
                .Include(r => r.Details)
                    .ThenInclude(d => d.PurchaseOrderDetail)
                .Include(r => r.Details)
                    .ThenInclude(d => d.ProductVariant)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Lots)
                        .ThenInclude(l => l.Boxes)
                .FirstOrDefaultAsync(r => r.Id == goodsReceiptId);
        }

        public Task UpdateGoodsReceiptAsync(GoodsReceipt goodsReceipt)
        {
            _context.GoodsReceipts.Update(goodsReceipt);
            return Task.CompletedTask;
        }

        public async Task<string> GenerateReceiptCodeAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _context.GoodsReceipts.CountAsync(x => x.CreatedAt.Year == year) + 1;
            return $"GR-{year}-{count:D5}";
        }
    }
}
