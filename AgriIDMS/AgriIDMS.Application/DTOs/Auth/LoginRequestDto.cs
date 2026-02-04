using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Username or Email is required")]
        [StringLength(256,MinimumLength = 3, ErrorMessage = "Username or Email must be between 3 and 256 characters")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password tối thiểu 8 ký tự")]
        [RegularExpression(
        @"^(?=.*[A-Z]).*$",
        ErrorMessage = "Password phải chứa ít nhất 1 chữ in hoa")]
        public string Password { get; set; } = string.Empty;

    }

    public class LogoutRequestDto
    {
        [Required(ErrorMessage = "Refresh Token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshRequestDto
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = default!;
    }

    public class AuthResponseDto
    {
        [Required(ErrorMessage = "AccessToken is required.")]
        public string AccessToken { get; set; } = default!;

        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = default!;

        [Required(ErrorMessage = "UserId is required.")]
        public string UserId { get; set; } = default!;

        [Required(ErrorMessage = "UserName is required.")]
        public string UserName { get; set; } = default!;

        [Required(ErrorMessage = "Roles is required.")]
        [MinLength(1, ErrorMessage = "Roles must contain at least 1 role.")]
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
