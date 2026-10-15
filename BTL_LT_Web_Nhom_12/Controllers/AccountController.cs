
using BTL_LT_Web_Nhom_12.Database;
using BTL_LT_Web_Nhom_12.Models.Account;
using BTL_LT_Web_Nhom_12.ViewModels;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebDbContext _context;

        public AccountController(WebDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Register(RegisterViewModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            string phone = model.Phone.Trim();
            bool phoneExists = await _context.Customers
                .AnyAsync(c => c.Phone == phone);

            if (phoneExists)
            {
                ModelState.AddModelError(
                    nameof(model.Phone),
                    "Số điện thoại đã được đăng ký"
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

            var hasher = new PasswordHasher<Customer>();

            customer.Password = hasher.HashPassword(
                customer,
                model.Password
            );

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            TempData["Success"] =
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
        public async Task<IActionResult> Login(LoginViewModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string account = model.UsernameOrPhone.Trim();

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == account);

            if (admin != null &&
                admin.IsActive &&
                admin.Password == model.Password)
            {
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

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == account);

            if (customer != null &&
                customer.IsActive &&
                customer.Password == model.Password)
            {
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

            ModelState.AddModelError(
                "",
                "Tài khoản hoặc mật khẩu không chính xác"
            );

            return View(model);
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
