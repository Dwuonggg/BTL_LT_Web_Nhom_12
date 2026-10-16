
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomerController : Controller
    {
        private readonly WebDbContext _context;

        public CustomerController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Customer
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .Include(c => c.Orders)
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(customers);
        }

        // GET: /Admin/Customer/Search?q=...
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            var query = _context.Customers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();

                query = query.Where(c =>
                    c.FullName.Contains(keyword) ||
                    c.Phone.Contains(keyword));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CustomerId,
                    c.FullName,
                    c.Phone,
                    c.IsActive,
                    c.CreatedAt,
                    TotalOrders = c.Orders.Count()
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                customers
            });
        }

        // GET: /Admin/Customer/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerId == id)
                .Select(c => new
                {
                    c.CustomerId,
                    c.FullName,
                    c.Phone,
                    c.IsActive,
                    c.CreatedAt,

                    TotalOrders = c.Orders.Count(),

                    Orders = c.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => new
                        {
                            o.OrderId,
                            o.OrderDate,
                            o.Status,
                            o.TotalAmount
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy khách hàng."
                });
            }

            return Json(new
            {
                success = true,
                customer
            });
        }

        // POST: /Admin/Customer/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var customer = await _context.Customers
                .FindAsync(id);

            if (customer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy khách hàng."
                });
            }

            customer.IsActive = !customer.IsActive;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = customer.IsActive,
                message = customer.IsActive
                    ? "Đã mở khóa tài khoản."
                    : "Đã khóa tài khoản."
            });
        }
    }
}
