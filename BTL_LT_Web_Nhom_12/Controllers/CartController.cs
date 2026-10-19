
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.ViewModels;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class CartController : Controller
    {
        private readonly WebDbContext _context;

        private const string CartSessionKey = "LuaMocCart";
        private const int MaxQuantity = 50;
        private const int MaxDistinctItems = 50;

        public CartController(WebDbContext context)
        {
            _context = context;
        }
        private List<CartItemViewModel> GetCart()
        {
            string? json =
                HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrEmpty(json))
            {
                return new List<CartItemViewModel>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItemViewModel>>(
                    json
                ) ?? new List<CartItemViewModel>();
            }
            catch (JsonException)
            {
                return new List<CartItemViewModel>();
            }
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            string json = JsonSerializer.Serialize(cart);

            HttpContext.Session.SetString(
                CartSessionKey,
                json
            );
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new CartViewModel
            {
                Items = GetCart()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            string type,
            int id,
            int quantity = 1,
            string? note = null)
        {
            if (string.IsNullOrWhiteSpace(type) ||
                id <= 0 ||
                quantity < 1 ||
                quantity > MaxQuantity)
            {
                TempData["CartError"] =
                    "Thông tin sản phẩm hoặc số lượng không hợp lệ.";

                return RedirectToAction(nameof(Index));
            }

            type = type.Trim();

            CartItemViewModel? product = null;

            // Lấy món ăn từ Database
            if (type.Equals(
                "Food",
                StringComparison.OrdinalIgnoreCase))
            {
                type = "Food";

                product = await _context.Foods
                    .AsNoTracking()
                    .Where(f =>
                        f.FoodId == id &&
                        f.Status == "Available" &&
                        f.Category.IsActive)
                    .Select(f => new CartItemViewModel
                    {
                        Type = "Food",
                        ProductId = f.FoodId,
                        Name = f.Name,
                        ImageUrl = f.ImageUrl,
                        UnitPrice = f.Price,
                        Quantity = quantity
                    })
                    .FirstOrDefaultAsync();
            }

            // Lấy combo từ Database
            else if (type.Equals(
                "Combo",
                StringComparison.OrdinalIgnoreCase))
            {
                type = "Combo";

                product = await _context.Combos
                    .AsNoTracking()
                    .Where(c =>
                        c.ComboId == id &&
                        c.IsActive)
                    .Select(c => new CartItemViewModel
                    {
                        Type = "Combo",
                        ProductId = c.ComboId,
                        Name = c.Name,
                        ImageUrl = c.ImageUrl,
                        UnitPrice = c.Price,
                        Quantity = quantity
                    })
                    .FirstOrDefaultAsync();
            }

            if (product == null)
            {
                TempData["CartError"] =
                    "Sản phẩm không tồn tại hoặc hiện không thể đặt.";

                return RedirectToAction(nameof(Index));
            }
            note = (note ?? string.Empty).Trim();

            if (note.Length > 250)
            {
                TempData["CartError"] = "Ghi chú không được quá 250 ký tự.";
                return RedirectToAction(nameof(Index));
            }

            product.Note = note;
            var cart = GetCart();

            var existing = cart.FirstOrDefault(x =>
                x.Type == type &&
                x.ProductId == id);

            if (existing != null)
            {
                if (existing.Quantity + quantity > MaxQuantity)
                {
                    TempData["CartError"] =
                        $"Mỗi sản phẩm chỉ được đặt tối đa {MaxQuantity} phần.";

                    return RedirectToAction(nameof(Index));
                }

                existing.Quantity += quantity;

                // Đồng bộ thông tin hiển thị mới nhất
                existing.Name = product.Name;
                existing.ImageUrl = product.ImageUrl;
                existing.UnitPrice = product.UnitPrice;
                existing.Note = product.Note;
            }
            else
            {
                if (cart.Count >= MaxDistinctItems)
                {
                    TempData["CartError"] =
                        "Giỏ hàng đã đạt số loại sản phẩm tối đa.";

                    return RedirectToAction(nameof(Index));
                }

                cart.Add(product);
            }

            SaveCart(cart);

            TempData["CartMessage"] =
                "Đã thêm sản phẩm vào giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(
            string type,
            int id,
            int quantity)
        {
            if (quantity < 1 || quantity > MaxQuantity)
            {
                TempData["CartError"] =
                    $"Số lượng phải từ 1 đến {MaxQuantity}.";

                return RedirectToAction(nameof(Index));
            }

            var cart = GetCart();

            var item = cart.FirstOrDefault(x =>
                x.Type.Equals(
                    type ?? "",
                    StringComparison.OrdinalIgnoreCase) &&
                x.ProductId == id);

            if (item == null)
            {
                TempData["CartError"] =
                    "Không tìm thấy sản phẩm trong giỏ hàng.";

                return RedirectToAction(nameof(Index));
            }

            item.Quantity = quantity;

            SaveCart(cart);

            TempData["CartMessage"] =
                "Đã cập nhật giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(
            string type,
            int id)
        {
            var cart = GetCart();

            cart.RemoveAll(x =>
                x.Type.Equals(
                    type ?? "",
                    StringComparison.OrdinalIgnoreCase) &&
                x.ProductId == id);

            SaveCart(cart);

            TempData["CartMessage"] =
                "Đã xóa sản phẩm khỏi giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }
    }
}
