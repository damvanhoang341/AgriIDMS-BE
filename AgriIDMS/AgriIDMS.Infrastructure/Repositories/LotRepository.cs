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
    public class LotRepository : ILotRepository
    {
        private readonly AppDbContext _context;

        public LotRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Lot> lots)
        {
            await _context.Lots.AddRangeAsync(lots);
        }

        public async Task<Lot?> GetByIdAsync(int id)
        {
            return await _context.Lots.FindAsync(id);
        }

        public async Task<Lot?> GetByIdWithDetailAndReceiptAsync(int id)
        {
            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d!.GoodsReceipt)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<Lot>> GetByGoodsReceiptIdAsync(int goodsReceiptId)
        {
            return await _context.Lots
                .Where(l => l.GoodsReceiptDetail.GoodsReceiptId == goodsReceiptId)
                .ToListAsync();
        }

        public async Task<Lot?> GetByLotCodeAsync(string lotCode)
        {
            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.GoodsReceipt)
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(l => l.LotCode == lotCode);
        }

    }
}
