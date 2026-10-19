
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class ComboController : Controller
    {
        private readonly WebDbContext _context;

        public ComboController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Combo
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var combos = await _context.Combos
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.ComboId)
                .ToListAsync();

            return View(combos);
        }

        // GET: /Combo/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();

            var combo = await _context.Combos
                .AsNoTracking()
                .Include(c => c.ComboDetails)
                    .ThenInclude(cd => cd.Food)
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync(c => c.ComboId == id);

            if (combo == null)
                return NotFound();

            return View(combo);
        }
    }
}
