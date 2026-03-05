using AgriIDMS.Application.DTOs.User;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Pagination;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task DeleteAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                throw new NotFoundException("User không tồn tại");

            if (user.Status == UserStatus.Deleted)
                throw new InvalidBusinessRuleException("User đã bị xóa trước đó");

            user.Status = UserStatus.Deleted;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new ApplicationException("Xóa user thất bại");
        }

        public async Task<PaginationResult<UserDto>> GetPagedAsync(
            PaginationRequest request)
        {
            var users = await _userManager.Users
                        .AsNoTracking()
                        .Where(x => x.Status != UserStatus.Deleted)
                        .OrderByDescending(x => x.CreatedAt)
                        .Skip((request.PageIndex - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    FullName = user.FullName ?? string.Empty,
                    //UserType = user.UserType,
                    Roles = roles.ToList()
                });
            }
            var totalCount = await _userManager.Users.CountAsync(x => x.Status != UserStatus.Deleted);

            return new PaginationResult<UserDto>
            {
                Items = userDtos,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }
    }
}
