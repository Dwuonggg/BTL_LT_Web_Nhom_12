using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_LT_Web_Nhom_12.Models.Menu
{
    public class Food
    {
        [Key]
        public int FoodId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available";

        public bool IsFeatured { get; set; } = false;

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;
        public ICollection<FoodOption> FoodOptions { get; set; } = new List<FoodOption>();

        public ICollection<ComboDetail> ComboDetails { get; set; } = new List<ComboDetail>();
    }
}
