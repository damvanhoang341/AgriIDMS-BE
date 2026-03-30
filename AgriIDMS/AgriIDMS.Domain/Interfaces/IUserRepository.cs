using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        Task<ApplicationUser?> GetByPhoneAsync(string phoneNumber);
        void UpdateUser(ApplicationUser user);
        IQueryable<ApplicationUser> GetAll();
        Task<IList<string>> GetRolesAsync(ApplicationUser user);

        Task<List<string>> GetUserIdsInRolesAsync(params string[] roles);
    }
}
