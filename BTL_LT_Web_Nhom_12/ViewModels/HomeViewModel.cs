using BTL_LT_Web_Nhom_12.Models.Menu;
using BTL_LT_Web_Nhom_12.Models.Poster;

namespace BTL_LT_Web_Nhom_12.ViewModels
{
    public class HomeViewModel
    {
        public List<Banner> Banners { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Food> FeaturedFoods { get; set; } = new();
        public List<Combo> Combos { get; set; } = new();
    }
}