using AgriIDMS.Application.DTOs.User;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Pagination;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork,ILogger<UserService> logger, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager )
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task ChangeUserRoleAsync(string userId, string roleName)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            var roleExists = await _roleManager.RoleExistsAsync(roleName);

            if (!roleExists)
                throw new Exception("Role does not exist");

            var currentRoles = await _userRepository.GetRolesAsync(user);

            // xóa role cũ
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // thêm role mới
            await _userManager.AddToRoleAsync(user, roleName);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId.ToString());

            if (user == null)
                throw new NotFoundException("User không tồn tại");

            if (user.Status == UserStatus.Deleted)
                throw new InvalidBusinessRuleException("User đã bị xóa trước đó");

            user.Status = UserStatus.Deleted;

            _userRepository.UpdateUser(user);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginationResult<UserDto>> GetPagedAsync(PaginationRequest request)
        {
            var query = _userRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status != UserStatus.Deleted);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userRepository.GetRolesAsync(user);

                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Status = user.Status,
                    Roles = roles.ToList()
                });
            }

            return new PaginationResult<UserDto>
            {
                Items = userDtos,
                TotalPages = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                return null;

            return new UserDetailDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Dob = user.Dob,
                Age = user.Age,
                Address = user.Address,
                Status = user.Status.ToString(),
                UserType = user.UserType.ToString(),
                CreatedAt = user.CreatedAt
            };
        }

        public async Task UpdateProfileAsync(string userId, UpdateUserProfileDto dto)
        {
            _logger.LogInformation("Updating profile for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new NotFoundException("User không tồn tại");

            if (dto.FullName != null)
                user.FullName = dto.FullName;

            if (dto.PhoneNumber != null)
                user.PhoneNumber = dto.PhoneNumber;

            if (dto.Gender.HasValue)
                user.Gender = dto.Gender.Value;

            if (dto.Dob.HasValue)
                user.Dob = dto.Dob.Value;

            if (dto.Address != null)
                user.Address = dto.Address;

            _userRepository.UpdateUser(user);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Profile updated for user {UserId}", userId);
        }

        public async Task ChangeStatus(string userId, ChangeStatusDto dto)
        {
            _logger.LogInformation("Change status for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new NotFoundException("User không tồn tại");

            user.Status = (UserStatus)dto.status;

            _userRepository.UpdateUser(user);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Change status for user {UserId}", userId);
        }

        public async Task<List<UserStatusDto>> GetByStatusAsync(string status)
        {
            var statusEnum = Enum.Parse<UserStatus>(status);

            var query = _userRepository.GetAll()
                .AsNoTracking()
                .Where(u => u.Status == statusEnum);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var result = new List<UserStatusDto>();

            foreach (var user in users)
            {
                var roles = await _userRepository.GetRolesAsync(user);
                result.Add(new UserStatusDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Roles = roles.ToList()
                });
            }

            return result;
        }
    }
}
