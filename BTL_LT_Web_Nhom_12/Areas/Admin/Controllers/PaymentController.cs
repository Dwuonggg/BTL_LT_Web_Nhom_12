
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentController : Controller
    {
        private readonly WebDbContext _context;

        public PaymentController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Payment
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? method,
            string? status)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .AsNoTracking()
                .AsQueryable();

            // Tìm theo mã đơn hoặc mã giao dịch
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                if (int.TryParse(keyword.TrimStart('#'), out int orderId))
                {
                    query = query.Where(p =>
                        p.OrderId == orderId ||
                        (p.TransactionCode != null &&
                         p.TransactionCode.Contains(keyword)));
                }
                else
                {
                    query = query.Where(p =>
                        p.TransactionCode != null &&
                        p.TransactionCode.Contains(keyword));
                }
            }

            // Lọc phương thức thanh toán
            if (!string.IsNullOrWhiteSpace(method))
            {
                query = query.Where(p =>
                    p.PaymentMethod == method);
            }

            // Lọc trạng thái thanh toán
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p =>
                    p.PaymentStatus == status);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentId)
                .ToListAsync();

            // Lấy các giá trị thật trong DB cho bộ lọc
            ViewBag.Methods = await _context.Payments
                .AsNoTracking()
                .Select(p => p.PaymentMethod)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            ViewBag.Statuses = await _context.Payments
                .AsNoTracking()
                .Select(p => p.PaymentStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Search = search ?? "";
            ViewBag.Method = method ?? "";
            ViewBag.Status = status ?? "";

            return View(payments);
        }
    }
}
