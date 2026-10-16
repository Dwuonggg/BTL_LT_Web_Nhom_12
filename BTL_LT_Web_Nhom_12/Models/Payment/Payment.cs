using BTL_LT_Web_Nhom_12.Models.Order;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Payment
{
    using OrderModel = BTL_LT_Web_Nhom_12.Models.Order.Order;
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, StringLength(30)]
        public string PaymentMethod { get; set; } = "BankTransfer";

        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(100)]
        public string? TransactionCode { get; set; }

        public DateTime? PaidAt { get; set; }

        [ForeignKey(nameof(OrderId))]
        public OrderModel Order { get; set; } = null!;
    }
}
