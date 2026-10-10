using BTL_LT_Web_Nhom_12.Models.Menu;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Order
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        public int? FoodId { get; set; }

        public int? ComboId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [ForeignKey(nameof(FoodId))]
        public Food? Food { get; set; }

        [ForeignKey(nameof(ComboId))]
        public Combo? Combo { get; set; }

        public ICollection<OrderDetailOption> OrderDetailOptions
        { get; set; } = new List<OrderDetailOption>();
    }
}
