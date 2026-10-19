using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Menu;
using BTL_LT_Web_Nhom_12.Models.Poster;
using BTL_LT_Web_Nhom_12.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class HomeController : Controller
    {
        private readonly WebDbContext _context;

        public HomeController(WebDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                Banners = await _context.Set<Banner>()
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.DisplayOrder)
                    .ToListAsync(),

                Categories = await _context.Set<Category>()
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),

                FeaturedFoods = await _context.Set<Food>()
                    .AsNoTracking()
                    .Where(f => f.IsFeatured
                             && f.Status == "Available"
                             && f.Category.IsActive)
                    .Take(8)
                    .ToListAsync(),

                Combos = await _context.Set<Combo>()
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Take(4)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}