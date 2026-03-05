using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.User
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;

        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public UserType UserType { get; set; }

        public List<string> Roles { get; set; } = new();
    }

    public class UserDetailDto
    {
        public string Id { get; set; } = null!;
        public string? FullName { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool Gender { get; set; }
        public DateTime? Dob { get; set; }
        public int? Age { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = null!;
        public string UserType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

}
