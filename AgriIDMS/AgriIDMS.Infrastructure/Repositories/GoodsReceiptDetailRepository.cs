using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
