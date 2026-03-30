using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Review review)
        {
            await _context.Set<Review>().AddAsync(review);
        }

        public async Task<Review?> GetByOrderDetailIdAsync(int orderDetailId)
        {
            return await _context.Set<Review>()
                .FirstOrDefaultAsync(x => x.OrderDetailId == orderDetailId);
        }

        public async Task<OrderDetail?> GetOrderDetailForReviewAsync(int orderDetailId)
        {
            return await _context.OrderDetails
                .Include(x => x.Order)
                    .ThenInclude(o => o.Payments)
                .Include(x => x.Review)
                .FirstOrDefaultAsync(x => x.Id == orderDetailId);
        }

        public async Task<bool> HasNonResolvedComplaintAsync(int orderId, int orderDetailId)
        {
            return await _context.Complaints
                .Where(c => !c.IsDeleted && c.OrderId == orderId)
                .Join(
                    _context.OrderAllocations.Where(a => a.OrderId == orderId && a.OrderDetailId == orderDetailId),
                    c => c.BoxId,
                    a => a.BoxId,
                    (c, _) => c)
                .AnyAsync(c => c.Status != ComplaintStatus.Verified);
        }

        public async Task<ComplaintStatus?> GetLatestComplaintStatusAsync(int orderId, int orderDetailId)
        {
            return await _context.Complaints
                .Where(c => !c.IsDeleted && c.OrderId == orderId)
                .Join(
                    _context.OrderAllocations.Where(a => a.OrderId == orderId && a.OrderDetailId == orderDetailId),
                    c => c.BoxId,
                    a => a.BoxId,
                    (c, _) => c)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => (ComplaintStatus?)c.Status)
                .FirstOrDefaultAsync();
        }
    }
}
