using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class DiscountRuleRepository : IDiscountRuleRepository
    {
        private readonly AppDbContext _db;

        public DiscountRuleRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<List<DiscountRule>> GetActiveRulesAsync(DiscountRuleType? ruleType = null)
        {
            var query = _db.DiscountRules
                .AsNoTracking()
                .Where(r => r.IsActive);

            if (ruleType.HasValue)
            {
                query = query.Where(r => r.RuleType == ruleType.Value);
            }

            return query
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public Task<List<DiscountRule>> GetAllRulesAsync(DiscountRuleType? ruleType = null)
        {
            var query = _db.DiscountRules.AsNoTracking();

            if (ruleType.HasValue)
            {
                query = query.Where(r => r.RuleType == ruleType.Value);
            }

            return query
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.MaxDaysLeft)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public async Task ReplaceAllRulesAsync(IEnumerable<DiscountRule> rules, DiscountRuleType? ruleType = null)
        {
            var existingQuery = _db.DiscountRules.AsQueryable();
            if (ruleType.HasValue)
            {
                existingQuery = existingQuery.Where(r => r.RuleType == ruleType.Value);
            }

            var existing = await existingQuery.ToListAsync();
            _db.DiscountRules.RemoveRange(existing);
            await _db.SaveChangesAsync();

            await _db.DiscountRules.AddRangeAsync(rules);
            await _db.SaveChangesAsync();
        }
    }
}

