using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BTL_LT_Web_Nhom_12.Models.Menu
{
    public class ComboDetail
    {
        [Key]
        public int ComboDetailId { get; set; }

        public int ComboId { get; set; }

        public int FoodId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [ForeignKey(nameof(ComboId))]
        public Combo Combo { get; set; } = null!;

        [ForeignKey(nameof(FoodId))]
        public Food Food { get; set; } = null!;
    }
}
