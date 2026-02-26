using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
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
    }
}
