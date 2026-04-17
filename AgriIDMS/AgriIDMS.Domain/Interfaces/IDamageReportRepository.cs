using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IDamageReportRepository
    {
        Task AddAsync(DamageReport item);
        Task<DamageReport?> GetByIdAsync(int id);
        Task<List<DamageReport>> GetListAsync(DamageReportStatus? status = null);
    }
}

