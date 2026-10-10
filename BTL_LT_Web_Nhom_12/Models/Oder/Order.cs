using BTL_LT_Web_Nhom_12.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Order
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        [Required, StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required, StringLength(15)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string DeliveryAddress { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime EstimatedCompletionTime { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public ICollection<OrderDetail> OrderDetails { get; set; }
            = new List<OrderDetail>();

        public BTL_LT_Web_Nhom_12.Models.Payment.Payment? Payment { get; set; }
    }
}
