
using System;
using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.Models.Account
{
    public class PasswordResetOtp
    {
        [Key]
        public int Id { get; set; }

        public int CustomerId { get; set; }

        [Required]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public int FailedAttempts { get; set; } = 0;

        public bool IsUsed { get; set; } = false;
    }
}
