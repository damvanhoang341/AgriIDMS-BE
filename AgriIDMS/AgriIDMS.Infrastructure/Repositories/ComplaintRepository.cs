using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly AppDbContext _context;

        public ComplaintRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Complaint complaint)
        {
            await _context.Complaints.AddAsync(complaint);
        }

        public async Task<Complaint?> GetByIdAsync(int id)
        {
            return await _context.Complaints.FindAsync(id);
        }

        public async Task<Complaint?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Complaints
                .AsSplitQuery()
                .Include(c => c.Order)
                .Include(c => c.Box)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(d => d.ProductVariant)
                                .ThenInclude(v => v.Product)
                .Include(c => c.VerifiedUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> HasPendingComplaintForOrderAndBoxAsync(int orderId, int boxId)
        {
            return await _context.Complaints.AnyAsync(c =>
                c.OrderId == orderId
                && c.BoxId == boxId
                && !c.IsDeleted
                && c.Status == ComplaintStatus.Pending);
        }

        public async Task<HashSet<int>> GetPendingComplaintBoxIdsForOrderAsync(int orderId)
        {
            var boxIds = await _context.Complaints
                .AsNoTracking()
                .Where(c =>
                    c.OrderId == orderId
                    && !c.IsDeleted
                    && c.Status == ComplaintStatus.Pending)
                .Select(c => c.BoxId)
                .Distinct()
                .ToListAsync();

            return boxIds.ToHashSet();
        }

        public async Task<List<Complaint>> ListByUserIdAsync(string userId)
        {
            return await _context.Complaints
                .AsNoTracking()
                .Include(c => c.Order)
                .Include(c => c.Box)
                .Where(c => c.Order.UserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Complaint>> ListAllAsync(int skip, int take)
        {
            return await _context.Complaints
                .AsNoTracking()
                .Include(c => c.Order)
                .Include(c => c.Box)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
