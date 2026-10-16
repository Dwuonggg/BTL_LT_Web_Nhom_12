
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : Controller
    {
        private readonly WebDbContext _context;

        public NotificationController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Notification/Latest
        [HttpGet]
        public async Task<IActionResult> Latest()
        {
            // Chỉ lấy các giao dịch đã thanh toán thành công
            var notifications = await _context.Payments
                .AsNoTracking()
                .Where(p =>
                    p.PaymentStatus == "Paid" &&
                    p.PaidAt.HasValue)
                .OrderByDescending(p => p.PaidAt)
                .Take(20)
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.Amount,
                    p.PaidAt,

                    CustomerName = p.Order.ReceiverName,
                    CustomerPhone = p.Order.ReceiverPhone,
                    OrderNote = p.Order.Note,

                    Items = p.Order.OrderDetails
                        .Select(d => new
                        {
                            Name = d.ComboId.HasValue
                                ? d.Combo!.Name
                                : d.Food!.Name,

                            d.Quantity,
                            d.Note
                        })
                        .ToList()
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                notifications
            });
        }
    }
}
