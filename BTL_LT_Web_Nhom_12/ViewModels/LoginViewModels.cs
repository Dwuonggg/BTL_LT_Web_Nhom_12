using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.ViewModels
{
    public class LoginViewModels
    {
        [Required(ErrorMessage = "Vui lòng nhập tài khoản hoặc số điện thoại")]
        public string UsernameOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
