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

        public async Task DeleteGoodsReceiptAsync(string goodsReceiptId)
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
            return await _context.GoodsReceipts.AsNoTracking().ToListAsync();
        }

        public async Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(int goodsReceiptId)
        {
            return await _context.GoodsReceipts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == goodsReceiptId);
        }

        public Task UpdateGoodsReceiptAsync(GoodsReceipt goodsReceipt)
        {
            _context.GoodsReceipts.Update(goodsReceipt);
            return Task.CompletedTask;
        }
    }
}
