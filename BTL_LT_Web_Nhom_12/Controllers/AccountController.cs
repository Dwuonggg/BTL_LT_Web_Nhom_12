
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Account;
using BTL_LT_Web_Nhom_12.ViewModels;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Security.Claims;
using System.Security.Cryptography;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AccountController> _logger;

        private readonly PasswordHasher<Admin> _adminHasher = new();
        private readonly PasswordHasher<Customer> _customerHasher = new();

        public AccountController(
            WebDbContext context,
            IWebHostEnvironment environment,
            ILogger<AccountController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModels model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string phone = model.Phone.Trim();

            bool exists = await _context.Customers
                .AnyAsync(c => c.Phone == phone);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Phone),
                    "Số điện thoại này đã được đăng ký."
                );

                return View(model);
            }

            var customer = new Customer
            {
                FullName = model.FullName.Trim(),
                Phone = phone,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            customer.Password = _customerHasher.HashPassword(
                customer,
                model.Password
            );

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Đăng ký thành công! Vui lòng đăng nhập.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModels model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string account = model.UsernameOrPhone.Trim();

            // -------- ĐĂNG NHẬP ADMIN --------

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == account);

            if (admin != null && admin.IsActive)
            {
                var result = PasswordVerificationResult.Failed;

                try
                {
                    result = _adminHasher.VerifyHashedPassword(
                        admin,
                        admin.Password,
                        model.Password
                    );
                }
                catch (FormatException)
                {
                    result = PasswordVerificationResult.Failed;
                }

                if (result != PasswordVerificationResult.Failed)
                {
                    if (result ==
                        PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        admin.Password = _adminHasher.HashPassword(
                            admin,
                            model.Password
                        );

                        await _context.SaveChangesAsync();
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            admin.AdminId.ToString()
                        ),
                        new Claim(
                            ClaimTypes.Name,
                            admin.Username
                        ),
                        new Claim(
                            ClaimTypes.Role,
                            "Admin"
                        )
                    };

                    await SignInUser(claims, model.RememberMe);

                    return RedirectToAction(
                        "Index",
                        "Dashboard",
                        new { area = "Admin" }
                    );
                }
            }

            // -------- ĐĂNG NHẬP CUSTOMER --------

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == account);

            if (customer != null && customer.IsActive)
            {
                var result = PasswordVerificationResult.Failed;

                try
                {
                    result = _customerHasher.VerifyHashedPassword(
                        customer,
                        customer.Password,
                        model.Password
                    );
                }
                catch (FormatException)
                {
                    result = PasswordVerificationResult.Failed;
                }

                if (result != PasswordVerificationResult.Failed)
                {
                    if (result ==
                        PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        customer.Password = _customerHasher.HashPassword(
                            customer,
                            model.Password
                        );

                        await _context.SaveChangesAsync();
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            customer.CustomerId.ToString()
                        ),
                        new Claim(
                            ClaimTypes.Name,
                            customer.FullName
                        ),
                        new Claim(
                            ClaimTypes.Role,
                            "Customer"
                        )
                    };

                    await SignInUser(claims, model.RememberMe);

                    return RedirectToAction(
                        "Index",
                        "Home",
                        new { area = "" }
                    );
                }
            }

            ModelState.AddModelError(
                "",
                "Tài khoản hoặc mật khẩu không chính xác."
            );

            ModelState.Remove(nameof(model.Password));
            model.Password = string.Empty;

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // OTP mô phỏng chỉ được sử dụng khi Development.
            if (!_environment.IsDevelopment())
            {
                ModelState.AddModelError(
                    "",
                    "Chức năng khôi phục mật khẩu chưa được cấu hình SMS."
                );

                return View(model);
            }

            string phone = model.Phone.Trim();

            // Xóa trạng thái xác minh từ lần trước.
            HttpContext.Session.Remove("ResetCustomerId");
            HttpContext.Session.Remove("ResetExpiryTicks");
            HttpContext.Session.SetString("ResetPhone", phone);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Phone == phone && c.IsActive);

            if (customer != null)
            {
                DateTime now = DateTime.UtcNow;

                var latest = await _context.PasswordResetOtps
                    .AsNoTracking()
                    .Where(x => x.CustomerId == customer.CustomerId)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefaultAsync();

                // Mỗi tài khoản chỉ được tạo OTP mới
                // sau ít nhất 60 giây.
                bool canCreate = latest == null ||
                    latest.CreatedAtUtc <= now.AddSeconds(-60);

                if (canCreate)
                {
                    // Vô hiệu hóa OTP cũ chưa sử dụng.
                    await _context.PasswordResetOtps
                        .Where(x =>
                            x.CustomerId == customer.CustomerId &&
                            !x.IsUsed)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(x => x.IsUsed, true));

                    // Tạo mã OTP ngẫu nhiên 6 chữ số.
                    string code = RandomNumberGenerator
                        .GetInt32(0, 1_000_000)
                        .ToString("D6");

                    var otp = new PasswordResetOtp
                    {
                        CustomerId = customer.CustomerId,
                        CreatedAtUtc = now,
                        ExpiresAtUtc = now.AddMinutes(5),
                        FailedAttempts = 0,
                        IsUsed = false
                    };

                    var otpHasher = new PasswordHasher<PasswordResetOtp>();

                    // Lưu hash OTP, không lưu OTP dạng thường.
                    otp.CodeHash = otpHasher.HashPassword(
                        otp,
                        code
                    );

                    _context.PasswordResetOtps.Add(otp);
                    await _context.SaveChangesAsync();

                    // Chỉ in OTP vào log khi chạy Development.
                    _logger.LogWarning(
                        "DEV ONLY - OTP dat lai mat khau: {OtpCode}",
                        code
                    );
                }
            }

            // Không tiết lộ số điện thoại có tài khoản hay không.
            TempData["OtpInfo"] =
                "Nếu số điện thoại đã được đăng ký, " +
                "mã xác minh sẽ được gửi. " +
                "Trong bản demo Development, xem OTP tại Output/Console.";

            return RedirectToAction(nameof(VerifyOtp));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            // Chỉ cho phép OTP demo trong Development
            if (!_environment.IsDevelopment())
            {
                TempData["OtpInfo"] =
                    "Chức năng gửi OTP chưa được cấu hình.";

                return RedirectToAction(nameof(VerifyOtp));
            }

            // Lấy số điện thoại đã nhập trước đó
            string? phone = HttpContext.Session.GetString("ResetPhone");

            if (string.IsNullOrWhiteSpace(phone))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Phone == phone &&
                    c.IsActive);

            if (customer == null)
            {
                TempData["OtpInfo"] =
                    "Không thể gửi mã xác minh. Vui lòng thử lại.";

                return RedirectToAction(nameof(VerifyOtp));
            }

            DateTime now = DateTime.UtcNow;

            // Tìm OTP được tạo gần nhất
            var latestOtp = await _context.PasswordResetOtps
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.CustomerId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            // Không cho gửi liên tục trong vòng 60 giây
            if (latestOtp != null)
            {
                DateTime nextAllowed =
                    latestOtp.CreatedAtUtc.AddSeconds(60);

                if (now < nextAllowed)
                {
                    int remainingSeconds = Math.Max(
                        1,
                        (int)Math.Ceiling(
                            (nextAllowed - now).TotalSeconds)
                    );

                    TempData["OtpInfo"] =
                        $"Vui lòng đợi {remainingSeconds} giây " +
                        "trước khi yêu cầu mã OTP mới.";

                    return RedirectToAction(nameof(VerifyOtp));
                }
            }

            // Vô hiệu hóa OTP cũ
            await _context.PasswordResetOtps
                .Where(x =>
                    x.CustomerId == customer.CustomerId &&
                    !x.IsUsed)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(x => x.IsUsed, true));

            // Sinh OTP mới gồm 6 chữ số
            string newCode = System.Security.Cryptography
                .RandomNumberGenerator
                .GetInt32(0, 1_000_000)
                .ToString("D6");

            var newOtp = new PasswordResetOtp
            {
                CustomerId = customer.CustomerId,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(5),
                FailedAttempts = 0,
                IsUsed = false
            };

            var hasher =
                new PasswordHasher<PasswordResetOtp>();

            newOtp.CodeHash = hasher.HashPassword(
                newOtp,
                newCode
            );

            _context.PasswordResetOtps.Add(newOtp);

            await _context.SaveChangesAsync();

            // Chỉ hiển thị OTP tại Output khi Development
            _logger.LogWarning(
                "DEV ONLY - OTP moi: {OtpCode}",
                newCode
            );

            TempData["OtpInfo"] =
                "Đã tạo mã OTP mới. " +
                "Vui lòng xem Output/Console trong Development.";

            return RedirectToAction(nameof(VerifyOtp));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyOtp()
        {
            string? phone =
                HttpContext.Session.GetString("ResetPhone");

            if (string.IsNullOrEmpty(phone))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new VerifyOtpViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(
            VerifyOtpViewModel model)
        {
            string? phone =
                HttpContext.Session.GetString("ResetPhone");

            if (string.IsNullOrEmpty(phone))
                return RedirectToAction(nameof(ForgotPassword));

            if (!ModelState.IsValid)
                return View(model);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Phone == phone && c.IsActive);

            PasswordResetOtp? otp = null;

            if (customer != null)
            {
                otp = await _context.PasswordResetOtps
                    .Where(x =>
                        x.CustomerId == customer.CustomerId &&
                        !x.IsUsed)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefaultAsync();
            }

            bool validOtp = false;

            if (otp != null &&
                otp.ExpiresAtUtc > DateTime.UtcNow &&
                otp.FailedAttempts < 5)
            {
                otp.FailedAttempts++;

                var otpHasher = new PasswordHasher<PasswordResetOtp>();

                var result = PasswordVerificationResult.Failed;

                try
                {
                    result = otpHasher.VerifyHashedPassword(
                        otp,
                        otp.CodeHash,
                        model.Code
                    );
                }
                catch (FormatException)
                {
                    result = PasswordVerificationResult.Failed;
                }

                if (result != PasswordVerificationResult.Failed)
                {
                    validOtp = true;
                    otp.IsUsed = true;
                }
                else if (otp.FailedAttempts >= 5)
                {
                    otp.IsUsed = true;
                }

                await _context.SaveChangesAsync();
            }

            if (!validOtp || customer == null)
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP không hợp lệ, hết hạn hoặc đã vượt số lần thử."
                );

                return View(model);
            }

            // Đã xác minh thành công.
            // Cho phép đặt lại mật khẩu trong 5 phút.
            HttpContext.Session.SetInt32(
                "ResetCustomerId",
                customer.CustomerId
            );

            HttpContext.Session.SetString(
                "ResetExpiryTicks",
                DateTime.UtcNow.AddMinutes(5).Ticks.ToString()
            );

            HttpContext.Session.Remove("ResetPhone");

            return RedirectToAction(nameof(ResetPassword));
        }

        private int? GetVerifiedResetCustomerId()
        {
            int? customerId =
                HttpContext.Session.GetInt32("ResetCustomerId");

            string? expiryText =
                HttpContext.Session.GetString("ResetExpiryTicks");

            if (!customerId.HasValue ||
                !long.TryParse(expiryText, out long ticks))
            {
                return null;
            }

            if (DateTime.UtcNow.Ticks >= ticks)
            {
                HttpContext.Session.Remove("ResetCustomerId");
                HttpContext.Session.Remove("ResetExpiryTicks");

                return null;
            }

            return customerId;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            if (!GetVerifiedResetCustomerId().HasValue)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            int? customerId = GetVerifiedResetCustomerId();

            if (!customerId.HasValue)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!ModelState.IsValid)
                return View(model);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId.Value &&
                    c.IsActive);

            if (customer == null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Hash mật khẩu mới.
            customer.Password = _customerHasher.HashPassword(
                customer,
                model.Password
            );

            await _context.SaveChangesAsync();

            // Xóa quyền reset để không sử dụng lại.
            HttpContext.Session.Remove("ResetCustomerId");
            HttpContext.Session.Remove("ResetExpiryTicks");
            HttpContext.Session.Remove("ResetPhone");

            TempData["SuccessMessage"] =
                "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";

            return RedirectToAction(nameof(Login));
        }

        private async Task SignInUser(
            List<Claim> claims,
            bool rememberMe)
        {
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe
                }
            );
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Profile()
        {
            var idClaim = User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

            if (!int.TryParse(idClaim, out int customerId))
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId &&
                    c.IsActive);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return StatusCode(
                403,
                "Bạn không có quyền truy cập."
            );
        }
    }
}
