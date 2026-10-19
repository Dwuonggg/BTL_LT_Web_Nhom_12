
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderController : Controller
    {
        private readonly WebDbContext _context;

        public OrderController(WebDbContext context)
        {
            _context = context;
        }

        private int? GetCustomerId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }

        // =====================================
        // DANH SÁCH ĐƠN HÀNG CUSTOMER
        // GET: /Order
        // =====================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Payment)
                .Where(o => o.CustomerId == customerId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .ToListAsync();

            return View(orders);
        }

        // =====================================
        // CHI TIẾT ĐƠN HÀNG
        // GET: /Order/Details/5
        // =====================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            if (id <= 0)
                return NotFound();

            var order = await _context.Orders
                .AsNoTracking()

                // Thông tin thanh toán
                .Include(o => o.Payment)

                // Danh sách món ăn
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Food)

                // Danh sách combo
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Combo)

                // Tránh truy vấn JOIN lặp dữ liệu
                .AsSplitQuery()

                // Chỉ lấy đơn của Customer đăng nhập
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.CustomerId == customerId.Value);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =====================================
        // CUSTOMER XÁC NHẬN ĐÃ NHẬN HÀNG
        // POST: /Order/ConfirmReceived
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int id)
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            if (id <= 0)
                return NotFound();

            // Chỉ cập nhật nếu:
            // - Đúng chủ đơn
            // - Đơn đang giao
            // - Đã thanh toán
            int updated = await _context.Orders
                .Where(o =>
                    o.OrderId == id &&
                    o.CustomerId == customerId.Value &&
                    o.Status == "Delivering" &&
                    o.Payment != null &&
                    o.Payment.PaymentStatus == "Paid")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        o => o.Status,
                        "Delivered"));

            if (updated == 1)
            {
                TempData["OrderSuccess"] =
                    "Bạn đã xác nhận nhận hàng thành công!";
            }
            else
            {
                TempData["OrderError"] =
                    "Không thể xác nhận đơn hàng. " +
                    "Đơn có thể chưa giao hoặc đã hoàn thành.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}
