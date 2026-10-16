using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoryController(
            WebDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Admin/Category
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Foods)
                .AsNoTracking()
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,IsActive")] Category model,
            IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra ảnh trước khi lưu
            if (!ValidateImage(imageFile))
            {
                return View(model);
            }

            // Kiểm tra tên danh mục trùng
            string name = model.Name.Trim();

            bool exists = await _context.Categories
                .AnyAsync(c => c.Name == name);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên danh mục đã tồn tại.");

                return View(model);
            }

            // Upload ảnh nếu có
            string? imageUrl = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                imageUrl = await SaveImageAsync(imageFile);
            }

            var category = new Category
            {
                Name = name,
                Description = model.Description,
                ImageUrl = imageUrl,
                IsActive = model.IsActive
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: /Admin/Category/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("CategoryId,Name,Description,IsActive")] Category model,
            IFormFile? imageFile)
        {
            if (id != model.CategoryId)
            {
                return BadRequest();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            // Giữ ảnh cũ khi phải hiển thị lại form
            model.ImageUrl = category.ImageUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!ValidateImage(imageFile))
            {
                return View(model);
            }

            string name = model.Name.Trim();

            bool exists = await _context.Categories
                .AnyAsync(c =>
                    c.Name == name &&
                    c.CategoryId != id);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên danh mục đã tồn tại.");

                return View(model);
            }

            // Nếu có ảnh mới thì thay ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                string newImageUrl =
                    await SaveImageAsync(imageFile);

                category.ImageUrl = newImageUrl;
            }

            // Nếu không chọn ảnh mới,
            // ImageUrl cũ được giữ nguyên

            category.Name = name;
            category.Description = model.Description;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            // Không xóa danh mục đang có món
            bool hasFoods = await _context.Foods
                .AnyAsync(f => f.CategoryId == id);

            if (hasFoods)
            {
                TempData["Error"] =
                    "Không thể xóa danh mục đang có món ăn.";

                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Xóa danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }

        private bool ValidateImage(IFormFile? imageFile)
        {
            // Không bắt buộc chọn ảnh
            if (imageFile == null)
            {
                return true;
            }

            const long maxSize = 5 * 1024 * 1024;

            if (imageFile.Length == 0 ||
                imageFile.Length > maxSize)
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Ảnh phải có dung lượng từ 1 byte đến 5MB.");

                return false;
            }

            string extension = Path
                .GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            string[] allowedContentTypes =
            {
                "image/jpeg", "image/png", "image/webp"
            };

            if (!allowedExtensions.Contains(extension) ||
                !allowedContentTypes.Contains(
                    imageFile.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.");

                return false;
            }

            return true;
        }

        private async Task<string> SaveImageAsync(
            IFormFile imageFile)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            string uploadFolder = Path.Combine(
                webRoot,
                "images",
                "categories");

            Directory.CreateDirectory(uploadFolder);

            string extension = Path
                .GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(
                uploadFolder,
                fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/images/categories/" + fileName;
        }
    }
}
