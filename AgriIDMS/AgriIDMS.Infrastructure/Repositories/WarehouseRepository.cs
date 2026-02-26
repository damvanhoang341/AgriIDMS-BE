using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly AppDbContext _context;
        public WarehouseRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId) => _context.Warehouses.FindAsync(warehouseId).AsTask();

    }
}
