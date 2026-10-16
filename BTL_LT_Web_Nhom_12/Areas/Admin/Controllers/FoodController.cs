
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FoodController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public FoodController(
            WebDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            string? status,
            int page = 1)
        {
            var query = _context.Foods
                .Include(f => f.Category)
                .AsNoTracking()
                .AsQueryable();

            // 1. Tìm kiếm theo tên món
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(f => f.Name.Contains(search.Trim()));
            }

            // 2. Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo trạng thái thực tế
            if (status == "Available")
            {
                query = query.Where(f =>
                    f.Status == "Available" && f.Category.IsActive);
            }
            else if (status == "Unavailable")
            {
                query = query.Where(f =>
                    f.Status != "Available" || !f.Category.IsActive);
            }

            const int pageSize = 10;

            int totalFoods = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalFoods / (double)pageSize
            );

            page = Math.Max(1, page);

            if (totalPages > 0)
                page = Math.Min(page, totalPages);

            var foods = await query
                .OrderBy(f => f.FoodId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;

            return View(foods);
        }

        // ================= CREATE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int categoryId,
            string name,
            decimal price,
            string? description,
            IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.Trim().Length > 150 ||
                price <= 0)
            {
                TempData["Error"] =
                    "Tên món hoặc giá bán không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            bool categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryId == categoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                TempData["Error"] =
                    "Danh mục không tồn tại hoặc đã ngừng hoạt động.";

                return RedirectToAction(nameof(Index));
            }

            if (!ValidateImage(imageFile))
            {
                return RedirectToAction(nameof(Index));
            }

            string? imageUrl = null;

            if (imageFile != null)
            {
                imageUrl = await SaveImageAsync(imageFile);
            }

            var food = new Food
            {
                CategoryId = categoryId,
                Name = name.Trim(),
                Description = description,
                Price = price,
                ImageUrl = imageUrl,
                Status = "Available",
                IsFeatured = false
            };

            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm món ăn thành công!";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var food = await _context.Foods
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FoodId == id);

            if (food == null)
            {
                return NotFound();
            }

            return Json(new
            {
                foodId = food.FoodId,
                categoryId = food.CategoryId,
                name = food.Name,
                description = food.Description,
                price = food.Price,
                imageUrl = food.ImageUrl,
                status = food.Status,
                isFeatured = food.IsFeatured
            });
        }

        // POST: /Admin/Food/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int categoryId,
            string name,
            decimal price,
            string? description,
            string status,
            bool isFeatured,
            IFormFile? imageFile)
        {
            var food = await _context.Foods.FindAsync(id);

            if (food == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(name) ||
                name.Trim().Length > 150 ||
                price <= 0)
            {
                TempData["Error"] =
                    "Tên món hoặc giá bán không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            bool categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryId == categoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                TempData["Error"] =
                    "Danh mục không tồn tại hoặc đã ngừng hoạt động.";

                return RedirectToAction(nameof(Index));
            }

            if (status != "Available" &&
                status != "Unavailable")
            {
                TempData["Error"] = "Trạng thái món không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (!ValidateImage(imageFile))
            {
                return RedirectToAction(nameof(Index));
            }

            // Thay ảnh mới nếu người dùng có chọn
            if (imageFile != null)
            {
                food.ImageUrl = await SaveImageAsync(imageFile);
            }

            // Không chọn ảnh mới thì giữ ảnh cũ
            food.CategoryId = categoryId;
            food.Name = name.Trim();
            food.Description = description;
            food.Price = price;
            food.Status = status;
            food.IsFeatured = isFeatured;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật món ăn thành công!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var food = await _context.Foods.FindAsync(id);

            if (food == null)
            {
                return NotFound();
            }

            // Không xóa món đã thuộc combo
            bool inCombo = await _context.Set<ComboDetail>()
                .AnyAsync(cd => cd.FoodId == id);

            if (inCombo)
            {
                TempData["Error"] =
                    "Không thể xóa món đang thuộc combo.";

                return RedirectToAction(nameof(Index));
            }

            // Các ràng buộc liên quan đơn hàng/lựa chọn món
            // vẫn phải được kiểm tra trước khi xóa thật.
            _context.Foods.Remove(food);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa món ăn thành công!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa món đã có dữ liệu liên quan. " +
                    "Bạn nên chuyển trạng thái sang ngừng bán.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ValidateImage(IFormFile? imageFile)
        {
            if (imageFile == null)
                return true;

            const long maxSize = 5 * 1024 * 1024;

            if (imageFile.Length == 0 ||
                imageFile.Length > maxSize)
            {
                TempData["Error"] =
                    "Ảnh phải có dung lượng từ 1 byte đến 5MB.";

                return false;
            }

            string extension = Path.GetExtension(
                imageFile.FileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            string[] allowedContentTypes =
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedExtensions.Contains(extension) ||
                !allowedContentTypes.Contains(
                    imageFile.ContentType.ToLowerInvariant()))
            {
                TempData["Error"] =
                    "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.";

                return false;
            }

            return true;
        }

        // ================= SAVE IMAGE =================

        private async Task<string> SaveImageAsync(
            IFormFile imageFile)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            string uploadFolder = Path.Combine(
                webRoot, "images", "foods");

            Directory.CreateDirectory(uploadFolder);

            string extension = Path.GetExtension(
                imageFile.FileName).ToLowerInvariant();

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(
                uploadFolder, fileName);

            using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/images/foods/" + fileName;
        }
    }
}
