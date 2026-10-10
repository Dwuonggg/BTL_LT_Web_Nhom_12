using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.Models.Poster
{
    public class Banner
    {
        [Key]
        public int BannerId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? TargetUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
