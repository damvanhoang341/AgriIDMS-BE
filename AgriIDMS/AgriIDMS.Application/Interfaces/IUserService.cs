using AgriIDMS.Application.DTOs.User;
using AgriIDMS.Application.Pagination;
using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDetailDto?> GetUserByIdAsync(string id);
        Task<PaginationResult<UserDto>> GetPagedAsync(PaginationRequest request);
        Task DeleteAsync(string userId);
        Task UpdateProfileAsync(string userId, UpdateUserProfileDto dto);
        Task ChangeStatus(string userId, ChangeStatusDto dto);
        Task ChangeUserRoleAsync(string userId, string roleName);
        Task<List<UserDto>> GetByStatusAsync(UserStatus status);
    }
}
