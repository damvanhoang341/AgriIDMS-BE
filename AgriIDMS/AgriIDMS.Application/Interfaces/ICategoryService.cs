using AgriIDMS.Application.DTOs.Category;

namespace AgriIDMS.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<int> CreateAsync(CreateCategoryRequest request);

        Task UpdateAsync(int id, UpdateCategoryRequest request);

        Task DeleteAsync(int id);

        Task<CategoryDto?> GetByIdAsync(int id);

        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task UpdateStatusAsync(int id, UpdateStatusCategoryRequest request);
    }
}