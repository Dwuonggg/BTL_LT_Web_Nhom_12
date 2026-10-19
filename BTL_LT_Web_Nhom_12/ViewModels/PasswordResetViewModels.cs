
using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.ViewModels
{
    // Nhập số điện thoại
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(
            @"^0\d{9}$",
            ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0")]
        public string Phone { get; set; } = string.Empty;
    }

    // Xác minh OTP
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "Mã OTP phải gồm 6 chữ số")]
        public string Code { get; set; } = string.Empty;
    }

    // Đặt mật khẩu mới
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(
            8,
            ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(
            "Password",
            ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
