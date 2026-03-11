using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByTransactionCodeAsync(string transactionCode);
        Task<List<Payment>> GetByOrderIdAsync(int orderId);
        Task<Payment?> GetLatestByOrderIdAsync(int orderId);
        Task<bool> HasSuccessPaymentAsync(int orderId);
    }
}
