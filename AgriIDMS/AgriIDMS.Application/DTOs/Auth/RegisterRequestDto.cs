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

        [Required(ErrorMessage = "Role là bắt buộc")]
        public string Role { get; set; } = null!;
    }

    public class RegisterCustomerRequest
    {
        [Required(ErrorMessage = "User Name là bắt buộc")]
        [MinLength(3, ErrorMessage = "UserName tối thiểu 3 ký tự")]
        public string UserName { get; set; }=null!;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password tối thiểu 8 ký tự")]
        [RegularExpression(
        @"^(?=.*[A-Z]).*$",
        ErrorMessage = "Password phải chứa ít nhất 1 chữ in hoa")]
        public string Password { get; set; } = null!;


        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        public bool Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Dob { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = null!;

        [MaxLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string? Address { get; set; }

        // ===== Optional =====

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [MaxLength(150, ErrorMessage = "Email tối đa 150 ký tự")]
        public string? Email { get; set; }
    }

    public class ConfirmEmailRequestDto
    {
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = null!;
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password tối thiểu 8 ký tự")]
        [RegularExpression(
        @"^(?=.*[A-Z]).*$",
        ErrorMessage = "Password phải chứa ít nhất 1 chữ in hoa")]
        public string CurrentPassword { get; set; } = null!;
        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password tối thiểu 8 ký tự")]
        [RegularExpression(
        @"^(?=.*[A-Z]).*$",
        ErrorMessage = "Password phải chứa ít nhất 1 chữ in hoa")]
        public string NewPassword { get; set; } = null!;
    }

}
