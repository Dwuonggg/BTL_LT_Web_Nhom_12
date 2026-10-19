
using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.ViewModels
{
    public class FoodApiRequest
    {
        [Required(ErrorMessage = "Tên món không được để trống.")]
        [StringLength(150)]
        [RegularExpression(@".*\S.*",
            ErrorMessage = "Tên món không được chỉ chứa khoảng trắng.")]
        public string Name { get; set; } = "";

        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
