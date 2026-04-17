using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ProductVariantDiscountOverrideRepository : IProductVariantDiscountOverrideRepository
    {
        private readonly AppDbContext _db;

        public ProductVariantDiscountOverrideRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ProductVariantDiscountOverride?> GetActiveOverrideForVariantAsync(int productVariantId, DateTime asOfUtc)
        {
            return await _db.ProductVariantDiscountOverrides
                .AsNoTracking()
                .Where(x => x.ProductVariantId == productVariantId && x.IsActive)
                .Where(x => !x.StartAtUtc.HasValue || x.StartAtUtc.Value <= asOfUtc)
                .Where(x => !x.EndAtUtc.HasValue || x.EndAtUtc.Value >= asOfUtc)
                .OrderByDescending(x => x.StartAtUtc ?? DateTime.MinValue)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<int, ProductVariantDiscountOverride>> GetActiveOverridesByVariantIdsAsync(
            IEnumerable<int> productVariantIds,
            DateTime asOfUtc)
        {
            var ids = productVariantIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<int, ProductVariantDiscountOverride>();

            var list = await _db.ProductVariantDiscountOverrides
                .AsNoTracking()
                .Where(x => ids.Contains(x.ProductVariantId) && x.IsActive)
                .Where(x => !x.StartAtUtc.HasValue || x.StartAtUtc.Value <= asOfUtc)
                .Where(x => !x.EndAtUtc.HasValue || x.EndAtUtc.Value >= asOfUtc)
                .OrderByDescending(x => x.StartAtUtc ?? DateTime.MinValue)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return list
                .GroupBy(x => x.ProductVariantId)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public Task<List<ProductVariantDiscountOverride>> GetAllAsync()
        {
            return _db.ProductVariantDiscountOverrides
                .AsNoTracking()
                .OrderBy(x => x.ProductVariantId)
                .ThenByDescending(x => x.StartAtUtc ?? DateTime.MinValue)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task ReplaceAllAsync(IEnumerable<ProductVariantDiscountOverride> overrides)
        {
            var existing = await _db.ProductVariantDiscountOverrides.ToListAsync();
            _db.ProductVariantDiscountOverrides.RemoveRange(existing);
            await _db.SaveChangesAsync();

            await _db.ProductVariantDiscountOverrides.AddRangeAsync(overrides);
            await _db.SaveChangesAsync();
        }
    }
}
