using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<ApplicationUser?> GetByIdAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ApplicationUser?> GetByPhoneAsync(string phoneNumber)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

        public void UpdateUser(ApplicationUser user)
        {
            _context.Users.Update(user);
        }

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            var roles = await (from ur in _context.UserRoles
                               join r in _context.Roles
                               on ur.RoleId equals r.Id
                               where ur.UserId == user.Id
                               select r.Name)
                               .ToListAsync();

            return roles;
        }

        public IQueryable<ApplicationUser> GetAll()
        {
            return _context.Users;
        }

        public async Task<List<string>> GetUserIdsInRolesAsync(params string[] roles)
        {
            if (roles == null || roles.Length == 0)
                return new List<string>();

            var roleNames = roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
            if (roleNames.Count == 0)
                return new List<string>();

            return await (from ur in _context.UserRoles
                          join r in _context.Roles on ur.RoleId equals r.Id
                          where roleNames.Contains(r.Name!)
                          select ur.UserId)
                .Distinct()
                .ToListAsync();
        }
    }
}
