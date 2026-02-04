using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Auth
{
    public class RegisterEmployeeDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Role là bắt buộc")]
        public string Role { get; set; } = null!;
    }

    public class RegisterCustomerRequest
    {
        [Required(ErrorMessage = "User Name là bắt buộc")]
        [MinLength(3, ErrorMessage = "UserName tối thiểu 3 ký tự")]
        public string UserName { get; set; }=null!;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password tối thiểu 6 ký tự")]
        [RegularExpression(
        @"^(?=.*[A-Z]).*$",
        ErrorMessage = "Password phải chứa ít nhất 1 chữ in hoa")]
        public string Password { get; set; } = null!;
    }

    public class ConfirmEmailRequestDto
    {
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
    }

}
