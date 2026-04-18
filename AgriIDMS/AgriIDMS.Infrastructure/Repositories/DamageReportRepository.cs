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

        public Task<List<DamageReport>> GetListAsync(
            DamageReportStatus? status = null,
            int? warehouseId = null,
            string? reportedByUserId = null,
            DamageProcessingOutcome? requestedOutcome = null)
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

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrWhiteSpace(reportedByUserId))
                query = query.Where(x => x.ReportedByUserId == reportedByUserId);

            if (requestedOutcome.HasValue)
                query = query.Where(x => x.RequestedProcessingOutcome == requestedOutcome.Value);

            return query
                .OrderByDescending(x => x.ReportedAt)
                .ToListAsync();
        }

        public Task<bool> HasPendingForBoxAsync(int boxId)
        {
            return _db.DamageReports.AnyAsync(x =>
                !x.IsDeleted &&
                x.TargetType == DamageTargetType.Box &&
                x.TargetId == boxId &&
                x.Status == DamageReportStatus.Pending);
        }
    }
}

