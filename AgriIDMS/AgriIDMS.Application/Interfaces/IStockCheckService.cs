using AgriIDMS.Application.DTOs.StockCheck;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IStockCheckService
    {
        Task<int> CreateAsync(CreateStockCheckRequest request, string userId);
        Task StartCheckAsync(int stockCheckId);
        Task UpdateCountedWeightAsync(UpdateCountedWeightRequest request, string userId);
        Task CompleteCountAsync(int stockCheckId);
        Task ApproveAsync(int stockCheckId, string userId);
        Task RejectAsync(int stockCheckId, string userId);

        Task<StockCheckWarehouseDashboardDto> GetWarehouseDashboardAsync(int? warehouseId);
        Task<StockCheckManagerDashboardDto> GetManagerDashboardAsync(int? warehouseId);
        Task<StockCheckDetailsResponseDto> GetDetailsAsync(int stockCheckId);
    }
}
