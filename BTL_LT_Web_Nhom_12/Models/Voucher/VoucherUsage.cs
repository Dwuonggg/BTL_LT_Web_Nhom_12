using BTL_LT_Web_Nhom_12.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Voucher
{
    using OrderModel = BTL_LT_Web_Nhom_12.Models.Order.Order;
    public class VoucherUsage
    {
        [Key]
        public int VoucherUsageId { get; set; }

        public int VoucherId { get; set; }

        public int CustomerId { get; set; }

        public int OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(VoucherId))]
        public Voucher Voucher { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        [ForeignKey(nameof(OrderId))]
        public OrderModel Order { get; set; } = null!;
    }
}
