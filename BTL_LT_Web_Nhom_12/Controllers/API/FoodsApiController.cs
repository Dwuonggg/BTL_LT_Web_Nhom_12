
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_LT_Web_Nhom_12.Models.Menu;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using BTL_LT_Web_Nhom_12.ViewModels;
using Microsoft.AspNetCore.Antiforgery;
namespace BTL_LT_Web_Nhom_12.Controllers.Api
{
    [ApiController]
    [Route("api/foods")]
    public class FoodsApiController : ControllerBase
    {
        private readonly WebDbContext _context;

        public FoodsApiController(WebDbContext context)
        {
            _context = context;
        }

        [HttpGet("/api/antiforgery/token")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAntiForgeryToken(
            [FromServices] IAntiforgery antiforgery)
        {
            var tokens = antiforgery.GetAndStoreTokens(
                HttpContext);

            Response.Headers.CacheControl = "no-store";

            return Ok(new
            {
                token = tokens.RequestToken
            });
        }

        // GET: /api/foods
        // GET: /api/foods?categoryId=1
        [HttpGet]
        public async Task<IActionResult> GetFoods(
            int? categoryId)
        {
            var query = _context.Foods
                .AsNoTracking()
                .Where(f => f.Status == "Available"
                         && f.Category.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(f =>
                    f.CategoryId == categoryId.Value);
            }

            var foods = await query
                .OrderBy(f => f.FoodId)
                .Select(f => new
                {
                    f.FoodId,
                    f.Name,
                    f.Price,
                    f.Description,
                    f.ImageUrl,
                    f.CategoryId
                })
                .ToListAsync();

            return Ok(foods);
        }

        // GET: /api/foods/17
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFood(int id)
        {
            var food = await _context.Foods
                .AsNoTracking()
                .Where(f => f.FoodId == id
                         && f.Status == "Available"
                         && f.Category.IsActive)
                .Select(f => new
                {
                    f.FoodId,
                    f.Name,
                    f.Price,
                    f.Description,
                    f.ImageUrl,
                    f.CategoryId
                })
                .FirstOrDefaultAsync();

            if (food == null)
                return NotFound(new
                {
                    message = "Không tìm thấy món ăn."
                });

            return Ok(food);
        }

        [HttpGet("/api/categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST: /api/foods
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFood(
            [FromBody] FoodApiRequest request)
        {
            // Kiểm tra danh mục có tồn tại và đang hoạt động
            bool categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryId == request.CategoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "Danh mục không hợp lệ."
                });
            }

            // Tạo món ăn mới
            var food = new Food
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                CategoryId = request.CategoryId,
                Description = request.Description?.Trim(),
                Status = "Available",
                IsFeatured = false
            };

            // Thêm món vào DbContext
            _context.Foods.Add(food);

            // Lưu xuống SQL Server
            await _context.SaveChangesAsync();

            // HTTP 201 Created
            return CreatedAtAction(
                nameof(GetFood),
                new { id = food.FoodId },
                new
                {
                    food.FoodId,
                    food.Name,
                    food.Price,
                    food.CategoryId
                }
            );
        }

        // PUT: /api/foods
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFood(
            int id,
            [FromBody] FoodApiRequest request)
        {
            // Tìm món cần sửa
            var food = await _context.Foods
                .FirstOrDefaultAsync(f => f.FoodId == id);

            if (food == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy món ăn."
                });
            }

            // Kiểm tra danh mục
            bool categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryId == request.CategoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "Danh mục không hợp lệ."
                });
            }

            // Cập nhật thông tin
            food.Name = request.Name.Trim();
            food.Price = request.Price;
            food.Description = request.Description?.Trim();
            food.CategoryId = request.CategoryId;

            // Giữ nguyên ảnh, trạng thái và IsFeatured
            await _context.SaveChangesAsync();

            // HTTP 204 No Content
            return NoContent();
        }

        // DELETE: /api/foods
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFood(int id)
        {
            var food = await _context.Foods
                .FirstOrDefaultAsync(f => f.FoodId == id);

            if (food == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy món ăn."
                });
            }

            // Không xóa món đã nằm trong đơn hàng
            bool usedInOrders = await _context.OrderDetails
                .AnyAsync(od => od.FoodId == id);

            // Không xóa món thuộc Combo
            bool usedInCombos = await _context.ComboDetails
                .AnyAsync(cd => cd.FoodId == id);

            if (usedInOrders || usedInCombos)
            {
                return Conflict(new
                {
                    message = "Món đã có dữ liệu liên quan. " +
                              "Vui lòng chuyển sang ngừng bán."
                });
            }

            _context.Foods.Remove(food);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể xóa món do dữ liệu liên quan."
                });
            }

            return NoContent();
        }

    }
}
