
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Setting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OpeningHourController : Controller
    {
        private readonly WebDbContext _context;

        public OpeningHourController(WebDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var openingHours = await _context.OpeningHours
                .AsNoTracking()
                .OrderBy(o => o.DayOfWeek)
                .ToListAsync();

            return View(openingHours);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            int dayOfWeek,
            TimeSpan openTime,
            TimeSpan closeTime,
            bool isClosed)
        {
            // Kiểm tra ngày trong tuần
            if (dayOfWeek < 0 || dayOfWeek > 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ngày trong tuần không hợp lệ."
                });
            }

            // Kiểm tra giờ hoạt động
            if (!isClosed && openTime >= closeTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Giờ đóng cửa phải muộn hơn giờ mở cửa."
                });
            }

            // Tìm dữ liệu của ngày đang chỉnh sửa
            var openingHour = await _context.OpeningHours
                .FirstOrDefaultAsync(o => o.DayOfWeek == dayOfWeek);

            if (openingHour == null)
            {
                openingHour = new OpeningHour
                {
                    DayOfWeek = dayOfWeek,
                    OpenTime = openTime,
                    CloseTime = closeTime,
                    IsClosed = isClosed
                };

                _context.OpeningHours.Add(openingHour);
            }
            else
            {
                openingHour.OpenTime = openTime;
                openingHour.CloseTime = closeTime;
                openingHour.IsClosed = isClosed;
            }

            await _context.SaveChangesAsync();

            // không tải lại trang
            return Json(new
            {
                success = true,
                message = "Lưu giờ hoạt động thành công!"
            });
        }

    }
}
