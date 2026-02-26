using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly AppDbContext _context;
        public SupplierRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Supplier> GetByIdAsync(int supplierId) => await _context.Suppliers.FindAsync(supplierId);
    }
}
