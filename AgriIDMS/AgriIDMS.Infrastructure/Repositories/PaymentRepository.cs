using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetByTransactionCodeAsync(string transactionCode)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.TransactionCode == transactionCode);
        }

        public async Task<List<Payment>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetLatestByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Payment>> GetPaymentsByMethodAndStatusAsync(
            PaymentMethod paymentMethod,
            PaymentStatus paymentStatus,
            int? orderId,
            string? customerUserId,
            int skip,
            int take)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .Where(p => p.PaymentMethod == paymentMethod && p.PaymentStatus == paymentStatus);

            if (orderId.HasValue)
                query = query.Where(p => p.OrderId == orderId.Value);

            if (!string.IsNullOrWhiteSpace(customerUserId))
            {
                var customerUserIdTrimmed = customerUserId.Trim();
                query = query.Where(p => p.Order.UserId == customerUserIdTrimmed);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> HasSuccessPaymentAsync(int orderId)
        {
            return await _context.Payments
                .AnyAsync(p => p.OrderId == orderId
                            && p.PaymentStatus == Domain.Enums.PaymentStatus.Success);
        }
    }
}
