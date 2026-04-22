using AgriIDMS.Domain.Entities;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IPurchaseRequestRepository
    {
        Task AddAsync(PurchaseRequest entity);
        Task<PurchaseRequest?> GetByIdAsync(int id);
        Task<IEnumerable<PurchaseRequest>> GetAllAsync();
        Task<string> GenerateRequestCodeAsync();
    }
}
