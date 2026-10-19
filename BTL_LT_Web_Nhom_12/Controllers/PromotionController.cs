
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class PromotionController : Controller
    {
        private readonly WebDbContext _context;

        public PromotionController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Promotion
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            DateTime now = DateTime.Now;

            var vouchers = await _context.Vouchers
                .AsNoTracking()
                .Where(v =>
                    v.IsActive &&
                    v.StartDate <= now &&
                    v.EndDate >= now)
                .OrderBy(v => v.EndDate)
                .ToListAsync();

            return View(vouchers);
        }
    }
}
