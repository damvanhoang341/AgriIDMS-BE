using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class BoxRepository : IBoxRepository
    {
        private readonly AppDbContext _context;
        public BoxRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task CreateAsync(Box box)
        {
            await _context.Boxes.AddAsync(box);
        }

        public async Task<Box?> GetByIdAsync(int id)
        {
            return await _context.Boxes.FindAsync(id);
        }
    }
}
