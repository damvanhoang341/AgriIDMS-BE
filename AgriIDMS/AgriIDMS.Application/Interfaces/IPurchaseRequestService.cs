using AgriIDMS.Application.DTOs.PurchaseRequest;

namespace AgriIDMS.Application.Interfaces
{
    public interface IPurchaseRequestService
    {
        Task<int> CreateAsync(CreatePurchaseRequestRequest request, string userId);
        Task<IEnumerable<PurchaseRequestResponse>> GetAllAsync();
        Task<PurchaseRequestResponse> GetByIdAsync(int id);
        Task<int> CreatePurchaseOrderAsync(int requestId, CreatePurchaseOrderFromRequestRequest request, string userId);
    }
}
