
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly WebDbContext _context;

        public OrderController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Order
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status)
        {
            var query = _context.Orders
                .Include(o => o.Payment)
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm theo tên, số điện thoại hoặc mã đơn
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                query = query.Where(o =>
                    o.ReceiverName.Contains(keyword) ||
                    o.ReceiverPhone.Contains(keyword) ||
                    o.OrderId.ToString() == keyword);
            }

            // Lọc theo trạng thái đơn
            string[] validStatuses =
            {
                "Pending",
                "Preparing",
                "Delivering",
                "Delivered",
                "Cancelled"
            };

            if (!string.IsNullOrWhiteSpace(status) &&
                validStatuses.Contains(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(orders);
        }

        // GET: /Admin/Order/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Food)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Combo)
                .Include(o => o.Payment)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: /Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string newStatus)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy đơn hàng."
                });
            }

            // Chỉ xử lý đơn đã thanh toán Online
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == id);

            if (payment == null ||
                payment.PaymentStatus != "Paid")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng chưa thanh toán thành công."
                });
            }

            string? allowedNextStatus = order.Status switch
            {
                "Preparing" => "Delivering",
                "Delivering" => "Delivered",
                _ => null
            };

            if (newStatus != allowedNextStatus)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không thể chuyển sang trạng thái này."
                });
            }

            order.Status = newStatus;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                status = order.Status,
                message = "Cập nhật trạng thái đơn hàng thành công!"
            });
        }
    }
}
