using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Order;
using BTL_LT_Web_Nhom_12.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;
using System.Text.Json;

using PaymentModel = BTL_LT_Web_Nhom_12.Models.Payment.Payment;
using OrderModel = BTL_LT_Web_Nhom_12.Models.Order.Order;
using VoucherModel = BTL_LT_Web_Nhom_12.Models.Voucher.Voucher;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CheckoutController : Controller
    {
        private async Task<bool> IsRestaurantOpenAsync()
        {
            DateTime now = DateTime.Now;

            int dayOfWeek = (int)now.DayOfWeek;
            TimeSpan currentTime = now.TimeOfDay;

            var openingHour = await _context.OpeningHours
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.DayOfWeek == dayOfWeek);

            if (openingHour == null || openingHour.IsClosed)
                return false;

            return currentTime >= openingHour.OpenTime
                && currentTime < openingHour.CloseTime;
        }

        private readonly WebDbContext _context;

        private const string CartSessionKey = "LuaMocCart";
        private const int MaxQuantity = 50;
        private const decimal StandardDeliveryFee = 20000m;
        private const decimal FreeShippingThreshold = 300000m;

        private decimal CalculateDeliveryFee(decimal subtotal)
        {
            return subtotal >= FreeShippingThreshold
                ? 0m
                : StandardDeliveryFee;
        }

        public CheckoutController(WebDbContext context)
        {
            _context = context;
        }

        private int? GetCustomerId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }
        private List<CartItemViewModel> GetCart()
        {
            string? json = HttpContext.Session.GetString(
                CartSessionKey);

            if (string.IsNullOrWhiteSpace(json))
                return new();

            try
            {
                return JsonSerializer.Deserialize<
                    List<CartItemViewModel>>(json) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }
        private async Task<List<CartItemViewModel>?>
            GetValidatedCartAsync()
        {
            var cart = GetCart();

            if (cart.Count == 0 || cart.Count > 50)
                return null;

            var result = new List<CartItemViewModel>();

            foreach (var item in cart)
            {
                if (item == null ||
                    item.ProductId <= 0 ||
                    item.Quantity < 1 ||
                    item.Quantity > MaxQuantity ||
                    (item.Note?.Length ?? 0) > 500)
                {
                    return null;
                }

                if (item.Type == "Food")
                {
                    var food = await _context.Foods
                        .AsNoTracking()
                        .Where(f =>
                            f.FoodId == item.ProductId &&
                            f.Status == "Available" &&
                            f.Category.IsActive)
                        .Select(f => new
                        {
                            f.FoodId,
                            f.Name,
                            f.ImageUrl,
                            f.Price
                        })
                        .FirstOrDefaultAsync();

                    if (food == null || food.Price < 0)
                        return null;

                    result.Add(new CartItemViewModel
                    {
                        Type = "Food",
                        ProductId = food.FoodId,
                        Name = food.Name,
                        ImageUrl = food.ImageUrl,
                        UnitPrice = food.Price,
                        Quantity = item.Quantity,
                        Note = item.Note
                    });
                }
                else if (item.Type == "Combo")
                {
                    var combo = await _context.Combos
                        .AsNoTracking()
                        .Where(c =>
                            c.ComboId == item.ProductId &&
                            c.IsActive)
                        .Select(c => new
                        {
                            c.ComboId,
                            c.Name,
                            c.ImageUrl,
                            c.Price
                        })
                        .FirstOrDefaultAsync();

                    if (combo == null || combo.Price < 0)
                        return null;

                    result.Add(new CartItemViewModel
                    {
                        Type = "Combo",
                        ProductId = combo.ComboId,
                        Name = combo.Name,
                        ImageUrl = combo.ImageUrl,
                        UnitPrice = combo.Price,
                        Quantity = item.Quantity,
                        Note = item.Note
                    });
                }
                else
                {
                    return null;
                }
            }

            // Không cho một món bị lặp bất thường
            if (result.GroupBy(x => new
            {
                x.Type,
                x.ProductId
            }).Any(g => g.Count() > 1))
            {
                return null;
            }

            return result;
        }

        private async Task<(
            VoucherModel? Voucher,
            decimal Discount,
            string? Error)> EvaluateVoucherAsync(
                string? code,
                int customerId,
                decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (null, 0m, null);

            string normalized = code.Trim().ToUpperInvariant();

            if (normalized.Length > 50)
            {
                return (
                    null,
                    0m,
                    "Mã ưu đãi không được quá 50 ký tự.");
            }

            DateTime now = DateTime.Now;

            var voucher = await _context.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(v =>
                    v.Code.ToUpper() == normalized);

            if (voucher == null ||
                !voucher.IsActive ||
                voucher.StartDate > now ||
                voucher.EndDate < now)
            {
                return (
                    null,
                    0m,
                    "Mã ưu đãi không tồn tại hoặc đã hết hạn.");
            }

            if (subtotal < voucher.MinOrderAmount)
            {
                return (
                    null,
                    0m,
                    $"Đơn hàng cần tối thiểu " +
                    $"{voucher.MinOrderAmount:N0}đ để dùng mã này.");
            }

            // Mỗi Customer chỉ dùng mỗi Voucher một lần
            bool alreadyUsed = await _context.VoucherUsages
                .AsNoTracking()
                .AnyAsync(u =>
                    u.CustomerId == customerId &&
                    u.VoucherId == voucher.VoucherId);

            if (alreadyUsed)
            {
                return (
                    null,
                    0m,
                    "Bạn đã sử dụng mã ưu đãi này rồi.");
            }

            // Tính số tiền giảm theo phần trăm
            decimal discount = Math.Round(
                subtotal * voucher.DiscountPercent / 100m,
                0,
                MidpointRounding.AwayFromZero);

            // Có giới hạn giảm tối đa thì áp dụng
            if (voucher.MaxDiscountAmount > 0)
            {
                discount = Math.Min(
                    discount,
                    voucher.MaxDiscountAmount);
            }

            discount = Math.Min(discount, subtotal);

            // Bản QR hiện tại chưa hỗ trợ đơn 0đ
            if (subtotal - discount + CalculateDeliveryFee(subtotal) <= 0)
            {
                return (
                    null,
                    0m,
                    "Đơn 0đ chưa hỗ trợ QR demo. " +
                    "Vui lòng chọn mã khác.");
            }

            return (voucher, discount, null);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            var items = await GetValidatedCartAsync();

            if (items == null)
            {
                TempData["CartError"] =
                    "Giỏ hàng trống hoặc có sản phẩm không còn bán.";

                return RedirectToAction("Index", "Cart");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId.Value &&
                    c.IsActive);

            if (customer == null)
                return Forbid();

            decimal subtotal = items.Sum(x => x.UnitPrice * x.Quantity);
            decimal deliveryFee = CalculateDeliveryFee(subtotal);

            var model = new CheckoutViewModel
            {
                ReceiverName = customer.FullName,
                ReceiverPhone = customer.Phone,
                Items = items,
                DeliveryFee = deliveryFee
            };
            ViewBag.IsRestaurantOpen = await IsRestaurantOpenAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewVoucher(
            string? voucherCode)
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Unauthorized();

            var items = await GetValidatedCartAsync();

            if (items == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Giỏ hàng không hợp lệ."
                });
            }

            decimal subtotal = items.Sum(
                x => x.UnitPrice * x.Quantity);

            var result = await EvaluateVoucherAsync(
                voucherCode,
                customerId.Value,
                subtotal);

            if (result.Error != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Error
                });
            }

            return Json(new
            {
                success = true,
                discountAmount = result.Discount,
                totalAmount =
                    subtotal - result.Discount + CalculateDeliveryFee(subtotal),

                message = result.Voucher == null
                    ? "Chưa áp dụng voucher."
                    : $"Áp dụng mã {result.Voucher.Code} thành công!"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            CheckoutViewModel model)
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            if (!await IsRestaurantOpenAsync())
            {
                TempData["CartError"] =
                    "LỬA MỘC hiện đang đóng cửa. " +
                    "Vui lòng quay lại trong giờ hoạt động.";

                return RedirectToAction("Index", "Cart");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId.Value &&
                    c.IsActive);

            if (customer == null)
                return Forbid();

            var items = await GetValidatedCartAsync();

            if (items == null)
            {
                TempData["CartError"] =
                    "Giỏ hàng không hợp lệ. Vui lòng kiểm tra lại.";

                return RedirectToAction("Index", "Cart");
            }

            model.Items = items;

            // Luôn tính lại bằng giá từ Database
            decimal subtotal = items.Sum(
                x => x.UnitPrice * x.Quantity);

            decimal deliveryFee = CalculateDeliveryFee(subtotal);
            model.DeliveryFee = deliveryFee;
            // Đảm bảo ModelState không giữ giá trị phí ship do client gửi
            ModelState.Remove(nameof(model.DeliveryFee));

            // Kiểm tra lại Voucher tại lúc tạo đơn
            var voucherResult = await EvaluateVoucherAsync(
                model.VoucherCode,
                customer.CustomerId,
                subtotal);

            if (voucherResult.Error != null)
            {
                ModelState.AddModelError(
                    nameof(model.VoucherCode),
                    voucherResult.Error);
            }

            model.DiscountAmount =
                voucherResult.Error == null
                    ? voucherResult.Discount
                    : 0m;

            if (!ModelState.IsValid)
            {
                ViewBag.IsRestaurantOpen =
                    await IsRestaurantOpenAsync();

                return View("Index", model);
            }

            decimal total =
                subtotal - model.DiscountAmount + deliveryFee;

            DateTime now = DateTime.Now;

            var order = new OrderModel
            {
                CustomerId = customer.CustomerId,

                ReceiverName = model.ReceiverName.Trim(),
                ReceiverPhone = model.ReceiverPhone.Trim(),
                DeliveryAddress = model.DeliveryAddress.Trim(),

                OrderDate = now,
                Status = "Pending",

                Subtotal = subtotal,
                DiscountAmount = model.DiscountAmount,
                DeliveryFee = deliveryFee,
                TotalAmount = total,

                // Lưu VoucherId để dùng khi thanh toán
                VoucherId = voucherResult.Voucher?.VoucherId,

                EstimatedCompletionTime =
                    now.AddMinutes(60),

                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim()
            };

            // Lưu các món của đơn hàng
            foreach (var item in items)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    FoodId = item.Type == "Food"
                        ? item.ProductId
                        : null,

                    ComboId = item.Type == "Combo"
                        ? item.ProductId
                        : null,

                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Note = item.Note
                });
            }

            // Thanh toán dùng số tiền SAU giảm giá
            order.Payment = new PaymentModel
            {
                Amount = total,
                PaymentMethod = "BankTransfer",
                PaymentStatus = "Pending"
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // Xóa giỏ sau khi tạo đơn thành công
            HttpContext.Session.Remove(CartSessionKey);

            // Chuyển thẳng sang QR mô phỏng
            return RedirectToAction(
                "Index",
                "DemoPayment",
                new { id = order.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentPending(int id)
        {
            int? customerId = GetCustomerId();

            if (!customerId.HasValue)
                return Challenge();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.CustomerId == customerId.Value);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}