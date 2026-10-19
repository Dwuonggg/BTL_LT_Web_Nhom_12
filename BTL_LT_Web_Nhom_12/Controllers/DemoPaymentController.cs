using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Voucher;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

using QRCoder;
using System.Data;
using System.Security.Claims;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    [Authorize(Roles = "Customer")]
    public class DemoPaymentController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DemoPaymentController(
            WebDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int? GetCustomerId()
        {
            string? claimId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(claimId, out int id)
                ? id
                : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            // Chỉ chạy trong Development
            if (!_environment.IsDevelopment())
                return NotFound();

            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";

            Response.Headers["Pragma"] = "no-cache";

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.CustomerId == customerId.Value);

            if (order == null)
                return NotFound();

            // Đơn đã thanh toán thì xem kết quả
            if (order.Payment?.PaymentStatus == "Paid")
            {
                return RedirectToAction(
                    "PaymentPending",
                    "Checkout",
                    new { id });
            }

            DateTime expiresAt =
                order.OrderDate.AddMinutes(10);

            bool canPay =
                order.Status == "Pending" &&
                order.Payment?.PaymentStatus == "Pending" &&
                DateTime.Now < expiresAt;

            ViewBag.CanPay = canPay;

            ViewBag.RemainingSeconds = canPay
                ? Math.Max(0, (int)Math.Ceiling(
                    (expiresAt - DateTime.Now).TotalSeconds))
                : 0;

            if (canPay)
            {
                // QR chứa số tiền SAU giảm Voucher
                string qrContent =
                    $"LUA MOC DEMO\n" +
                    $"ORDER: {order.OrderId}\n" +
                    $"AMOUNT: {order.TotalAmount:0} VND\n" +
                    "NOT A REAL PAYMENT";

                using var generator = new QRCodeGenerator();

                using var qrData = generator.CreateQrCode(
                    qrContent,
                    QRCodeGenerator.ECCLevel.Q);

                using var qrCode = new PngByteQRCode(qrData);

                ViewBag.QrBase64 =
                    Convert.ToBase64String(
                        qrCode.GetGraphic(10));
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            DateTime now = DateTime.Now;

            // Payment và VoucherUsage phải được lưu
            // cùng nhau, tránh thanh toán mà mất lượt Voucher.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.CustomerId == customerId.Value);

            if (order == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            // Kiểm tra trạng thái và thời hạn
            if (order.Status != "Pending" ||
                order.Payment?.PaymentStatus != "Pending" ||
                order.OrderDate <= now.AddMinutes(-10))
            {
                await transaction.RollbackAsync();

                TempData["DemoPaymentError"] =
                    "Đơn hàng đã thanh toán, " +
                    "bị hủy hoặc quá 10 phút.";

                return RedirectToAction(
                    nameof(Index),
                    new { id });
            }

            // Nếu đơn sử dụng Voucher
            if (order.VoucherId.HasValue)
            {
                bool alreadyUsed =
                    await _context.VoucherUsages
                        .AnyAsync(u =>
                            u.VoucherId ==
                                order.VoucherId.Value &&
                            u.CustomerId ==
                                customerId.Value);

                if (alreadyUsed)
                {
                    await transaction.RollbackAsync();

                    TempData["DemoPaymentError"] =
                        "Voucher đã được sử dụng ở đơn khác. " +
                        "Vui lòng tạo đơn mới với mã khác.";

                    return RedirectToAction(
                        nameof(Index),
                        new { id });
                }

                // Chỉ lúc này mới ghi lượt sử dụng
                var usage = new VoucherUsage
                {
                    VoucherId = order.VoucherId.Value,
                    CustomerId = customerId.Value,
                    OrderId = order.OrderId,
                    DiscountAmount = order.DiscountAmount,
                    UsedAt = now
                };

                _context.VoucherUsages.Add(usage);
            }

            // Cập nhật Payment thành Paid
            order.Payment.PaymentStatus = "Paid";
            order.Payment.PaidAt = now;

            order.Payment.TransactionCode =
                $"DEMO-{id}-{Guid.NewGuid():N}";

            try
            {
                // Lưu Payment và VoucherUsage
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["DemoPaymentError"] =
                    "Không thể ghi nhận thanh toán. " +
                    "Voucher có thể đã được dùng ở đơn khác.";

                return RedirectToAction(
                    nameof(Index),
                    new { id });
            }

            TempData["DemoPaymentSuccess"] =
                "Thanh toán mô phỏng thành công!";

            return RedirectToAction(
                "PaymentPending",
                "Checkout",
                new { id });
        }
    }
}