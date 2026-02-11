using AgriIDMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public bool Gender { get;  set; }
        public DateTime? Dob { get;  set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }

        public UserStatus Status { get; set; } = UserStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public UserType UserType { get; private set; }
        public RegisterMethod RegisterMethod { get; private set; }
        public ICollection<UserNotification> UserNotifications { get; private set; }= new List<UserNotification>();


        public void SetUserType(UserType type)
        {
            UserType = type;
        }

        public void SetRegisterMethod(RegisterMethod method)
        {
            RegisterMethod = method;
        }

        // Domain methods
        public void SetProfile(string fullName, DateTime? dob, Boolean gender, string? address)
        {
            FullName = fullName;
            Dob = dob;
            Gender = gender;
            Address = address;
        }

        public int? Age
        {
            get
            {
                if (Dob == null) return null;
                var today = DateTime.Today;
                var age = today.Year - Dob.Value.Year;
                if (Dob.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}
