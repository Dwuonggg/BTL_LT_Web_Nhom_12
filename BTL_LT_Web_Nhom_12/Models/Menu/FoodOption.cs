using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Menu
{
    public class FoodOption
    {
        [Key]
        public int FoodOptionId { get; set; }

        public int FoodId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal AdditionalPrice { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(FoodId))]
        public Food Food { get; set; } = null!;
    }
}
