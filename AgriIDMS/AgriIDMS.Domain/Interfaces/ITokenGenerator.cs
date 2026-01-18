using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(string userId, string userName, IList<string> roles);
        string GenerateRefreshToken();
    }
}
