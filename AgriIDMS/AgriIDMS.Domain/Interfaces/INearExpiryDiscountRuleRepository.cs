using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IDiscountRuleRepository
    {
        Task<List<DiscountRule>> GetActiveRulesAsync(DiscountRuleType? ruleType = null);
        Task<List<DiscountRule>> GetAllRulesAsync(DiscountRuleType? ruleType = null);
        Task ReplaceAllRulesAsync(IEnumerable<DiscountRule> rules, DiscountRuleType? ruleType = null);
    }
}

