using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class DisposalRequestRepository : IDisposalRequestRepository
    {
        private readonly AppDbContext _db;

        public DisposalRequestRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task CreateAsync(DisposalRequest request)
        {
            _db.DisposalRequests.Add(request);
            return Task.CompletedTask;
        }

        public Task<DisposalRequest?> GetByIdWithItemsAsync(int id)
        {
            return _db.DisposalRequests
                .Include(r => r.Warehouse)
                .Include(r => r.RequestedUser)
                .Include(r => r.ReviewedUser)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Box)
                        .ThenInclude(b => b.Slot)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Box)
                        .ThenInclude(b => b.Lot)
                            .ThenInclude(l => l.GoodsReceiptDetail)
                                .ThenInclude(d => d.ProductVariant)
                                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public Task<List<DisposalRequest>> GetListAsync(DisposalRequestStatus? status, int? warehouseId)
        {
            var q = _db.DisposalRequests
                .Include(r => r.Warehouse)
                .Include(r => r.RequestedUser)
                .Include(r => r.ReviewedUser)
                .Include(r => r.Items)
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
                q = q.Where(r => r.Status == status.Value);
            if (warehouseId.HasValue && warehouseId.Value > 0)
                q = q.Where(r => r.WarehouseId == warehouseId.Value);

            return q
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }
    }
}

