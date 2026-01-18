using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task<(bool ok, string userId, string userName)> ValidateUserAsync(string userNameOrEmail, string password);
        Task<IList<string>> GetRolesAsync(string userId);
        Task<string> GetUserNameAsync(string userId);
    }
}
