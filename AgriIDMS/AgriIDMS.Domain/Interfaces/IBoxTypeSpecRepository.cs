using AgriIDMS.Domain.Entities;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IBoxTypeSpecRepository
    {
        Task<List<BoxTypeSpec>> GetAllActiveAsync();
        Task<List<BoxTypeSpec>> GetAllAsync();
        Task AddRangeAsync(IEnumerable<BoxTypeSpec> entities);
        Task RemoveRangeAsync(IEnumerable<BoxTypeSpec> entities);
    }
}

