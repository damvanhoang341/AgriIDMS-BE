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
    public class NearExpiryDiscountRuleRepository : INearExpiryDiscountRuleRepository
    {
        private readonly AppDbContext _db;

        public NearExpiryDiscountRuleRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<List<NearExpiryDiscountRule>> GetActiveRulesAsync(DateTime asOfUtc)
        {
            return _db.NearExpiryDiscountRules
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Where(r => !r.StartAtUtc.HasValue || r.StartAtUtc.Value <= asOfUtc)
                .Where(r => !r.EndAtUtc.HasValue || r.EndAtUtc.Value >= asOfUtc)
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.DiscountPercent)
                .ThenBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public Task<List<NearExpiryDiscountRule>> GetAllRulesAsync()
        {
            return _db.NearExpiryDiscountRules
                .AsNoTracking()
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public async Task ReplaceAllRulesAsync(IEnumerable<NearExpiryDiscountRule> rules)
        {
            var existing = await _db.NearExpiryDiscountRules.ToListAsync();
            _db.NearExpiryDiscountRules.RemoveRange(existing);
            await _db.SaveChangesAsync();

            await _db.NearExpiryDiscountRules.AddRangeAsync(rules);
            await _db.SaveChangesAsync();
        }
    }
}

