
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Menu;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class MenuController : Controller
    {
        private readonly WebDbContext _context;

        public MenuController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Menu
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId)
        {
            var query = _context.Set<Food>()
                .AsNoTracking()
                .Include(f => f.Category)
                .Where(f => f.Status == "Available"
                         && f.Category.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(f =>
                    f.Name.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f =>
                    f.CategoryId == categoryId.Value);
            }

            var foods = await query
                .OrderBy(f => f.Name)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;

            ViewBag.Categories = await _context.Set<Category>()
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(foods);
        }

        // GET: /Menu/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var food = await _context.Set<Food>()
                .AsNoTracking()
                .Include(f => f.Category)
                .FirstOrDefaultAsync(f =>
                    f.FoodId == id
                    && f.Status == "Available"
                    && f.Category.IsActive);

            if (food == null)
            {
                return NotFound();
            }

            return View(food);
        }
    }
}
