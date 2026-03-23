using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IList<Order>> GetByUserIdWithDetailsAndPaymentsAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<Order?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Details)
                .ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetByIdWithPaymentsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetByIdWithDetailsAndPaymentsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IList<Order>> GetOverdueBackordersAsync(System.DateTime nowUtc)
        {
            return await _context.Orders
                .Include(o => o.Details)
                .Include(o => o.Allocations)
                .Where(o =>
                    o.Status == OrderStatus.BackorderWaiting
                    && o.Allocations.Any(a =>
                        a.Status == AllocationStatus.Reserved
                        && a.ExpiredAt.HasValue
                        && a.ExpiredAt.Value <= nowUtc))
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IList<Order>> GetPendingSaleConfirmationOrdersAsync(string? customerUserId, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Where(o => o.Status == OrderStatus.PendingSaleConfirmation);

            if (!string.IsNullOrWhiteSpace(customerUserId))
                query = query.Where(o => o.UserId == customerUserId.Trim());

            return await query
                .OrderBy(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IList<Order>> GetPendingAllocationOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Where(o => o.Status == OrderStatus.AwaitingAllocation);

            if (!string.IsNullOrWhiteSpace(customerUserId))
                query = query.Where(o => o.UserId == customerUserId.Trim());

            if (source.HasValue)
                query = query.Where(o => o.Source == source.Value);

            return await query
                .OrderBy(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}

