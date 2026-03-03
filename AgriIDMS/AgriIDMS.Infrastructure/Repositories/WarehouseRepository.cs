using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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

        public Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId) =>
            _context.Warehouses.FindAsync(warehouseId).AsTask();

        public Task<bool> ExistsByNameAsync(string name)
        {
            return _context.Warehouses
                .AnyAsync(w => w.Name == name);
        }

        public async Task AddAsync(Warehouse warehouse)
        {
            await _context.Warehouses.AddAsync(warehouse);
        }

        public Task<List<Warehouse>> GetAllAsync()
        {
            return _context.Warehouses
                .AsNoTracking()
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public Task UpdateAsync(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Warehouse warehouse)
        {
            _context.Warehouses.Remove(warehouse);
            return Task.CompletedTask;
        }
    }
}
