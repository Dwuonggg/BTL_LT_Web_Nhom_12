using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Voucher
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0.01, 100)]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal DiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxDiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<VoucherUsage> VoucherUsages
        { get; set; } = new List<VoucherUsage>();
    }
}
