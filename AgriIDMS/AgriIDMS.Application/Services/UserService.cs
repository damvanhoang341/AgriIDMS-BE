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

        public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
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
    }
}
