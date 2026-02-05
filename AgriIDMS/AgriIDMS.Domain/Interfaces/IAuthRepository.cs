using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task<IList<string>> GetRolesAsync(string userId);
        Task<string> GetUserNameAsync(string userId);
    }
}
