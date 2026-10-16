
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Poster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BannerController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BannerController(
            WebDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Admin/Banner
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .AsNoTracking()
                .OrderBy(b => b.DisplayOrder)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(banners);
        }

        // POST: /Admin/Banner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string title,
            string? targetUrl,
            int displayOrder,
            bool isActive,
            IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(title) ||
                title.Trim().Length > 150 ||
                displayOrder < 0 ||
                !IsValidTargetUrl(targetUrl))
            {
                TempData["Error"] = "Thông tin Banner không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (imageFile == null || !ValidateImage(imageFile))
            {
                TempData["Error"] =
                    "Vui lòng chọn ảnh JPG, PNG hoặc WEBP tối đa 5MB.";
                return RedirectToAction(nameof(Index));
            }

            string imageUrl = await SaveImageAsync(imageFile);

            var banner = new Banner
            {
                Title = title.Trim(),
                ImageUrl = imageUrl,
                TargetUrl = string.IsNullOrWhiteSpace(targetUrl)
                    ? null : targetUrl.Trim(),
                DisplayOrder = displayOrder,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm Banner thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Banner/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string title,
            string? targetUrl,
            int displayOrder,
            bool isActive,
            IFormFile? imageFile)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.BannerId == id);

            if (banner == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(title) ||
                title.Trim().Length > 150 ||
                displayOrder < 0 ||
                !IsValidTargetUrl(targetUrl))
            {
                TempData["Error"] = "Thông tin Banner không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (imageFile != null)
            {
                if (!ValidateImage(imageFile))
                {
                    TempData["Error"] =
                        "Ảnh phải là JPG, PNG hoặc WEBP tối đa 5MB.";
                    return RedirectToAction(nameof(Index));
                }

                banner.ImageUrl = await SaveImageAsync(imageFile);
            }

            banner.Title = title.Trim();
            banner.TargetUrl = string.IsNullOrWhiteSpace(targetUrl)
                ? null : targetUrl.Trim();
            banner.DisplayOrder = displayOrder;
            banner.IsActive = isActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật Banner thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Banner/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);

            if (banner == null)
                return NotFound();

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa Banner thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Banner/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var banner = await _context.Banners.FindAsync(id);

            if (banner == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy Banner."
                });
            }

            banner.IsActive = !banner.IsActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = banner.IsActive,
                message = banner.IsActive
                    ? "Đã bật Banner."
                    : "Đã ẩn Banner."
            });
        }

        // Kiểm tra ảnh upload
        private static bool ValidateImage(IFormFile file)
        {
            const long maxSize = 5 * 1024 * 1024;

            string extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            string[] allowedContentTypes =
            {
                "image/jpeg", "image/png", "image/webp"
            };

            return file.Length > 0 &&
                   file.Length <= maxSize &&
                   allowedExtensions.Contains(extension) &&
                   allowedContentTypes.Contains(
                       file.ContentType.ToLowerInvariant());
        }

        // Chỉ cho phép URL nội bộ hoặc http/https
        private static bool IsValidTargetUrl(string? targetUrl)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
                return true;

            string url = targetUrl.Trim();

            if (url.StartsWith("/") && !url.StartsWith("//")
                && !url.StartsWith("/\\"))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps);
        }

        // Lưu ảnh vào wwwroot/images/banners
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath, "wwwroot");

            string folder = Path.Combine(
                webRoot, "images", "banners");

            Directory.CreateDirectory(folder);

            string extension = Path.GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/images/banners/" + fileName;
        }
    }
}
