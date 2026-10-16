using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ComboController : Controller
    {
        private readonly WebDbContext _context;

        public ComboController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Combo
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var combos = await _context.Set<Combo>()
            .Include(c => c.ComboDetails)
                .ThenInclude(d => d.Food)
                    .ThenInclude(f => f.Category)
            .AsNoTracking()
            .OrderBy(c => c.ComboId)
            .ToListAsync();

            return View(combos);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Foods = await _context.Foods
                .Include(f => f.Category)
                .Where(f => f.Status == "Available" &&
                            f.Category.IsActive)
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            decimal price,
            string? description,
            bool isActive,
            List<int> foodIds,
            List<int> quantities,
            IFormFile? imageFile)
        {
            // Kiểm tra thông tin Combo
            if (string.IsNullOrWhiteSpace(name) ||
                name.Trim().Length > 150 ||
                price <= 0)
            {
                TempData["Error"] = "Tên hoặc giá Combo không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra danh sách món
            if (foodIds == null ||
                quantities == null ||
                foodIds.Count == 0 ||
                foodIds.Count != quantities.Count ||
                foodIds.Distinct().Count() != foodIds.Count ||
                quantities.Any(q => q < 1))
            {
                TempData["Error"] = "Vui lòng chọn món và số lượng hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            // Chỉ chấp nhận món đang bán, danh mục hoạt động
            int validFoodCount = await _context.Foods
                .CountAsync(f =>
                    foodIds.Contains(f.FoodId) &&
                    f.Status == "Available" &&
                    f.Category.IsActive);

            if (validFoodCount != foodIds.Count)
            {
                TempData["Error"] = "Có món ăn không tồn tại hoặc đã ngừng bán.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra ảnh
            string? imageUrl = null;
            if (imageFile == null)
            {
                TempData["Error"] = "Form chưa gửi ảnh lên Controller.";
                return RedirectToAction(nameof(Index));
            }
            if (imageFile != null)
            {
                string extension = Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (imageFile.Length == 0 ||
                    imageFile.Length > 5 * 1024 * 1024 ||
                    !allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Ảnh phải là JPG, PNG hoặc WEBP, tối đa 5MB.";
                    return RedirectToAction(nameof(Index));
                }

                string webRoot = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot");

                string folder = Path.Combine(webRoot, "images", "combos");
                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString("N") + extension;
                string filePath = Path.Combine(folder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.CreateNew))
                {
                    await imageFile.CopyToAsync(stream);
                }

                imageUrl = "/images/combos/" + fileName;
            }

            // Tạo Combo cùng các dòng ComboDetail
            var combo = new Combo
            {
                Name = name.Trim(),
                Price = price,
                Description = description,
                ImageUrl = imageUrl,
                IsActive = isActive,
                ComboDetails = foodIds.Select((id, index) => new ComboDetail
                {
                    FoodId = id,
                    Quantity = quantities[index]
                }).ToList()
            };

            _context.Set<Combo>().Add(combo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm Combo thành công!";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var combo = await _context.Set<Combo>()
                .Include(c => c.ComboDetails)
                .ThenInclude(d => d.Food)
                .ThenInclude(f => f.Category).AsNoTracking()
                .FirstOrDefaultAsync(c => c.ComboId == id);

            if (combo == null)
            {
                return NotFound();
            }

            ViewBag.Foods = await _context.Foods
                .Include(f => f.Category)
                .Where(f =>
                    (f.Status == "Available" && f.Category.IsActive) ||
                    f.ComboDetails.Any(d => d.ComboId == id))
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string name,
            decimal price,
            string? description,
            bool isActive,
            List<int> foodIds,
            List<int> quantities,
            IFormFile? imageFile)
        {
            // 1. Tìm Combo cần sửa, kèm danh sách món
            var combo = await _context.Set<Combo>()
                .Include(c => c.ComboDetails)
                .FirstOrDefaultAsync(c => c.ComboId == id);

            if (combo == null)
            {
                return NotFound();
            }
            // 2. Kiểm tra thông tin Combo
            if (string.IsNullOrWhiteSpace(name) ||
                name.Trim().Length > 150 ||
                price <= 0)
            {
                TempData["Error"] = "Tên hoặc giá Combo không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }
            // 3. Kiểm tra danh sách món và số lượng
            if (foodIds == null ||
                quantities == null ||
                foodIds.Count == 0 ||
                foodIds.Count != quantities.Count ||
                foodIds.Distinct().Count() != foodIds.Count ||
                quantities.Any(q => q < 1))
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một món và số lượng hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            // 4. Kiểm tra món ăn được chọn
            var oldFoodIds = combo.ComboDetails
                .Select(d => d.FoodId)
                .ToHashSet();

            var validFoodIds = await _context.Foods
                .Where(f => foodIds.Contains(f.FoodId) &&
                    (oldFoodIds.Contains(f.FoodId) ||
                     (f.Status == "Available" && f.Category.IsActive)))
                .Select(f => f.FoodId)
                .ToListAsync();

            if (validFoodIds.Count != foodIds.Count)
            {
                TempData["Error"] =
                    "Có món không hợp lệ hoặc đã ngừng bán.";
                return RedirectToAction(nameof(Index));
            }

            // 5. Kiểm tra ảnh mới nếu có
            if (imageFile != null)
            {
                string extension = Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();

                string[] allowedExtensions =
                {
            ".jpg", ".jpeg", ".png", ".webp"
        };

                string[] allowedContentTypes =
                {
            "image/jpeg", "image/png", "image/webp"
        };

                if (imageFile.Length == 0 ||
                    imageFile.Length > 5 * 1024 * 1024 ||
                    !allowedExtensions.Contains(extension) ||
                    !allowedContentTypes.Contains(
                        imageFile.ContentType.ToLowerInvariant()))
                {
                    TempData["Error"] =
                        "Ảnh phải là JPG, PNG hoặc WEBP, tối đa 5MB.";

                    return RedirectToAction(nameof(Index));
                }

                string folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot", "images", "combos");

                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString("N") + extension;
                string filePath = Path.Combine(folder, fileName);

                await using (var stream = new FileStream(
                    filePath, FileMode.CreateNew))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Chỉ thay ảnh khi Admin chọn ảnh mới
                combo.ImageUrl = "/images/combos/" + fileName;
            }

            // 6. Cập nhật thông tin Combo
            combo.Name = name.Trim();
            combo.Price = price;
            combo.Description = description;
            combo.IsActive = isActive;

            // 7. Cập nhật danh sách món trong Combo

            // Xóa những dòng chi tiết không còn được chọn
            var removedDetails = combo.ComboDetails
                .Where(d => !foodIds.Contains(d.FoodId))
                .ToList();

            _context.Set<ComboDetail>().RemoveRange(removedDetails);

            foreach (var (foodId, index) in foodIds.Select((value, i) => (value, i)))
            {
                var existingDetail = combo.ComboDetails
                    .FirstOrDefault(d => d.FoodId == foodId);

                if (existingDetail != null)
                {
                    // Món đã có: cập nhật số lượng
                    existingDetail.Quantity = quantities[index];
                }
                else
                {
                    // Món mới: thêm vào Combo
                    combo.ComboDetails.Add(new ComboDetail
                    {
                        FoodId = foodId,
                        Quantity = quantities[index]
                    });
                }
            }

            // 8. Lưu xuống SQL Server
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật Combo thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await _context.Set<Combo>()
                .Include(c => c.ComboDetails)
                .FirstOrDefaultAsync(c => c.ComboId == id);

            if (combo == null)
            {
                TempData["Error"] = "Không tìm thấy Combo.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa các món thuộc Combo trước
            _context.Set<ComboDetail>()
                .RemoveRange(combo.ComboDetails);

            // Xóa Combo
            _context.Set<Combo>().Remove(combo);

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa Combo thành công!";
            }
            catch (DbUpdateException)
            {
                // Xóa trạng thái thay đổi đang được EF Core theo dõi
                // Không cần thao tác thêm vì request sẽ kết thúc
                TempData["Error"] =
                    "Không thể xóa Combo đã có dữ liệu liên quan. " +
                    "Bạn nên chuyển Combo sang Ngừng bán.";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}