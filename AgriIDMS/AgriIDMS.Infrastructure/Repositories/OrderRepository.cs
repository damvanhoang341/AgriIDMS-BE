using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task<IList<Order>> GetPendingWarehouseConfirmOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Include(o => o.Allocations)
                .Where(o =>
                    o.Status == OrderStatus.PendingWarehouseConfirm
                    && o.Allocations.Any(a => a.Status == AllocationStatus.Proposed));

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

        public async Task<IList<Order>> GetPendingCustomerDecisionOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Where(o => o.Status == OrderStatus.PartiallyAllocated);

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

        public async Task<IList<Order>> GetBackorderWaitingOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Include(o => o.Allocations)
                .Where(o => o.Status == OrderStatus.BackorderWaiting);

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

        public async Task<IList<Order>> GetConfirmedAllocationOrdersAsync(string? customerUserId, OrderSource? source, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Include(o => o.Allocations)
                .Where(o =>
                    o.Status == OrderStatus.Confirmed
                    && o.Allocations.Any(a => a.Status == AllocationStatus.Reserved));

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

        public async Task<IList<Order>> GetPaidPendingExportOrdersAsync(int? orderId, OrderSource? source, int skip, int take, string? sort)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Payments)
                .Include(o => o.Allocations)
                .Include(o => o.ExportReceipts)
                .Where(o => o.Allocations.Any(a => a.Status == AllocationStatus.Reserved))
                .Where(o =>
                    o.Status == OrderStatus.Paid
                    || (o.Status == OrderStatus.Confirmed
                        && o.Payments.Any(p =>
                            p.PaymentMethod == PaymentMethod.COD
                            && p.PaymentStatus == PaymentStatus.Pending)));

            if (orderId.HasValue)
                query = query.Where(o => o.Id == orderId.Value);

            if (source.HasValue)
                query = query.Where(o => o.Source == source.Value);

            var sortKey = sort?.Trim();
            if (string.Equals(sortKey, "createdAtDesc", StringComparison.OrdinalIgnoreCase))
                query = query.OrderByDescending(o => o.CreatedAt);
            else if (string.Equals(sortKey, "createdAtAsc", StringComparison.OrdinalIgnoreCase))
                query = query.OrderBy(o => o.CreatedAt);
            else if (string.Equals(sortKey, "paidAtAsc", StringComparison.OrdinalIgnoreCase))
                query = query.OrderBy(o => o.Payments.Max(p => (DateTime?)p.PaidAt) ?? o.CreatedAt);
            else
                query = query.OrderByDescending(o => o.Payments.Max(p => (DateTime?)p.PaidAt) ?? o.CreatedAt);

            return await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IList<Order>> GetCustomerOrdersForComplaintAsync(string userId, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Allocations)
                .Where(o =>
                    o.UserId == userId
                    && (o.Status == OrderStatus.Shipping || o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed));

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}

