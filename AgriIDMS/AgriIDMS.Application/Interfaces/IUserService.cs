using AgriIDMS.Application.DTOs.User;
using AgriIDMS.Application.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDetailDto?> GetUserByIdAsync(string id);
        Task<PaginationResult<UserDto>> GetPagedAsync(PaginationRequest request);
        Task DeleteAsync(string userId);
    }
}
