using AgriIDMS.Domain.Entities;

namespace AgriIDMS.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id);

        Task<IEnumerable<Category>> GetAllAsync();

        /// <summary>Danh mục Active kèm Product (IsActive) và Variant (IsActive) để hiển thị trang chủ.</summary>
        Task<IEnumerable<Category>> GetActiveWithProductsAndVariantsForDisplayAsync();

        Task<bool> ExistsByNameAsync(string name);

        Task AddAsync(Category category);

        void Update(Category category);
    }
}