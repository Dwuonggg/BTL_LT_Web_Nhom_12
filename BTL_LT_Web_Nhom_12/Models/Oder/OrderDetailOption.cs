using BTL_LT_Web_Nhom_12.Models.Menu;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Order
{
    public class OrderDetailOption
    {
        [Key]
        public int OrderDetailOptionId { get; set; }

        public int OrderDetailId { get; set; }

        public int FoodOptionId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; }

        [ForeignKey(nameof(OrderDetailId))]
        public OrderDetail OrderDetail { get; set; } = null!;

        [ForeignKey(nameof(FoodOptionId))]
        public FoodOption FoodOption { get; set; } = null!;
    }
}
