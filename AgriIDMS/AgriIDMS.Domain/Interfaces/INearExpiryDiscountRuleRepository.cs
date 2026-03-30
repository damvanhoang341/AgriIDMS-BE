using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface INearExpiryDiscountRuleRepository
    {
        Task<List<NearExpiryDiscountRule>> GetActiveRulesAsync();
        Task<List<NearExpiryDiscountRule>> GetAllRulesAsync();
        Task ReplaceAllRulesAsync(IEnumerable<NearExpiryDiscountRule> rules);
    }
}

