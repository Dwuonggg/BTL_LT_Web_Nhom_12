
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Voucher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VoucherController : Controller
    {
        private readonly WebDbContext _context;

        public VoucherController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Voucher
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.VoucherUsages)
                .AsNoTracking()
                .OrderByDescending(v => v.StartDate)
                .ThenByDescending(v => v.VoucherId)
                .ToListAsync();

            return View(vouchers);
        }

        // POST: /Admin/Voucher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string code,
            string title,
            string? description,
            decimal discountPercent,
            decimal maxDiscountAmount,
            decimal minOrderAmount,
            DateTime startDate,
            DateTime endDate,
            bool isActive)
        {
            if (!ValidateVoucher(
                code, title, description, discountPercent,
                maxDiscountAmount, minOrderAmount,
                startDate, endDate))
            {
                return RedirectToAction(nameof(Index));
            }

            string normalizedCode = code.Trim().ToUpperInvariant();

            bool exists = await _context.Vouchers
                .AnyAsync(v => v.Code == normalizedCode);

            if (exists)
            {
                TempData["Error"] = "Mã Voucher đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var voucher = new Voucher
            {
                Code = normalizedCode,
                Title = title.Trim(),
                Description = description?.Trim(),
                DiscountPercent = discountPercent,
                MaxDiscountAmount = maxDiscountAmount,
                MinOrderAmount = minOrderAmount,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = isActive
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm Voucher thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Voucher/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string code,
            string title,
            string? description,
            decimal discountPercent,
            decimal maxDiscountAmount,
            decimal minOrderAmount,
            DateTime startDate,
            DateTime endDate,
            bool isActive)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
                return NotFound();

            if (!ValidateVoucher(
                code, title, description, discountPercent,
                maxDiscountAmount, minOrderAmount,
                startDate, endDate))
            {
                return RedirectToAction(nameof(Index));
            }

            string normalizedCode = code.Trim().ToUpperInvariant();

            bool exists = await _context.Vouchers
                .AnyAsync(v =>
                    v.Code == normalizedCode &&
                    v.VoucherId != id);

            if (exists)
            {
                TempData["Error"] = "Mã Voucher đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            voucher.Code = normalizedCode;
            voucher.Title = title.Trim();
            voucher.Description = description?.Trim();
            voucher.DiscountPercent = discountPercent;
            voucher.MaxDiscountAmount = maxDiscountAmount;
            voucher.MinOrderAmount = minOrderAmount;
            voucher.StartDate = startDate;
            voucher.EndDate = endDate;
            voucher.IsActive = isActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật Voucher thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Voucher/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
                return NotFound();

            bool hasUsage = await _context.VoucherUsages
                .AnyAsync(vu => vu.VoucherId == id);

            if (hasUsage)
            {
                TempData["Error"] =
                    "Voucher đã được sử dụng. Hãy chuyển sang Ngừng hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa Voucher thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Voucher/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);

            if (voucher == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy Voucher."
                });
            }

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = voucher.IsActive,
                message = voucher.IsActive
                    ? "Đã bật Voucher."
                    : "Đã tắt Voucher."
            });
        }

        private bool ValidateVoucher(
            string? code,
            string? title,
            string? description,
            decimal discountPercent,
            decimal maxDiscountAmount,
            decimal minOrderAmount,
            DateTime startDate,
            DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                code.Trim().Length > 50 ||
                string.IsNullOrWhiteSpace(title) ||
                title.Trim().Length > 150)
            {
                TempData["Error"] = "Mã hoặc tên Voucher không hợp lệ.";
                return false;
            }

            if (description?.Length > 500)
            {
                TempData["Error"] = "Mô tả không được quá 500 ký tự.";
                return false;
            }

            if (discountPercent <= 0 || discountPercent > 100 ||
                maxDiscountAmount <= 0 ||
                minOrderAmount < 0)
            {
                TempData["Error"] =
                    "Phần trăm hoặc giới hạn giảm giá không hợp lệ.";
                return false;
            }

            if (startDate == default ||
                endDate == default ||
                endDate < startDate)
            {
                TempData["Error"] =
                    "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.";
                return false;
            }

            return true;
        }
    }
}
