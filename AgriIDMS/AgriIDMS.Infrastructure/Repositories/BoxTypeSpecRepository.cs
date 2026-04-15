using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class BoxTypeSpecRepository : IBoxTypeSpecRepository
    {
        private readonly AppDbContext _context;

        public BoxTypeSpecRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<BoxTypeSpec>> GetAllActiveAsync()
        {
            return _context.BoxTypeSpecs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.BoxType)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public Task<List<BoxTypeSpec>> GetAllAsync()
        {
            return _context.BoxTypeSpecs.ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<BoxTypeSpec> entities)
        {
            await _context.BoxTypeSpecs.AddRangeAsync(entities);
        }

        public Task RemoveRangeAsync(IEnumerable<BoxTypeSpec> entities)
        {
            _context.BoxTypeSpecs.RemoveRange(entities);
            return Task.CompletedTask;
        }
    }
}

