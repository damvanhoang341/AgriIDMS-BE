using AgriIDMS.Domain.Entities;

namespace AgriIDMS.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id);

        Task<IEnumerable<Category>> GetAllAsync();

        Task<bool> ExistsByNameAsync(string name);

        Task AddAsync(Category category);

        void Update(Category category);
    }
}