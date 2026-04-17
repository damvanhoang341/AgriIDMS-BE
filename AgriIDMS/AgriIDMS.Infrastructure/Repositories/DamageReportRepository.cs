using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class DamageReportRepository : IDamageReportRepository
    {
        private readonly AppDbContext _db;

        public DamageReportRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task AddAsync(DamageReport item)
        {
            return _db.DamageReports.AddAsync(item).AsTask();
        }

        public Task<DamageReport?> GetByIdAsync(int id)
        {
            return _db.DamageReports
                .Include(x => x.ReportedByUser)
                .Include(x => x.ReviewedByUser)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public Task<List<DamageReport>> GetListAsync(DamageReportStatus? status = null)
        {
            var query = _db.DamageReports
                .AsNoTracking()
                .Include(x => x.ReportedByUser)
                .Include(x => x.ReviewedByUser)
                .Where(x => !x.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return query
                .OrderByDescending(x => x.ReportedAt)
                .ToListAsync();
        }
    }
}

