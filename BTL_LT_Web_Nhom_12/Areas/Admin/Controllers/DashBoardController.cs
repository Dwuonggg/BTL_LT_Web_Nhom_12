
using BTL_LT_Web_Nhom_12.Areas.Admin.ViewModels;
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly WebDbContext _context;

        public DashboardController(WebDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Dashboard
        [HttpGet]
        public async Task<IActionResult> Index(string period = "today")
        {
            string[] validPeriods = { "today", "7days", "30days" };

            if (!validPeriods.Contains(period))
            {
                period = "today";
            }

            DateTime today = DateTime.Today;

            DateTime fromDate = period switch
            {
                "7days" => today.AddDays(-6),
                "30days" => today.AddDays(-29),
                _ => today
            };

            // Mốc kết thúc không bao gồm 0h ngày mai
            DateTime toDate = today.AddDays(1);

            var model = new DashboardViewModel
            {
                Period = period,
                PeriodLabel = period switch
                {
                    "7days" => "7 ngày qua",
                    "30days" => "30 ngày qua",
                    _ => "Hôm nay"
                }
            };

            // 1. Thống kê đơn hàng theo trạng thái
            var orderStatistics = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.OrderDate >= fromDate &&
                    o.OrderDate < toDate)
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            model.TotalOrders = orderStatistics.Sum(x => x.Count);

            model.PendingOrders = orderStatistics
                .Where(x => x.Status == "Pending")
                .Sum(x => x.Count);

            model.PreparingOrders = orderStatistics
                .Where(x => x.Status == "Preparing")
                .Sum(x => x.Count);

            model.DeliveringOrders = orderStatistics
                .Where(x => x.Status == "Delivering")
                .Sum(x => x.Count);

            model.DeliveredOrders = orderStatistics
                .Where(x => x.Status == "Delivered")
                .Sum(x => x.Count);

            model.CancelledOrders = orderStatistics
                .Where(x => x.Status == "Cancelled")
                .Sum(x => x.Count);

            // 2. Khách hàng đăng ký trong kỳ
            model.NewCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync(c =>
                    c.CreatedAt >= fromDate &&
                    c.CreatedAt < toDate);

            // 3. Giao dịch đã thanh toán trong kỳ
            var paidPayments = await _context.Payments
                .AsNoTracking()
                .Where(p =>
                    p.PaymentStatus == "Paid" &&
                    p.PaidAt.HasValue &&
                    p.PaidAt.Value >= fromDate &&
                    p.PaidAt.Value < toDate)
                .Select(p => new
                {
                    p.Amount,
                    PaidAt = p.PaidAt!.Value
                })
                .ToListAsync();

            // Doanh thu dựa trên thời điểm đã thanh toán
            model.Revenue = paidPayments.Sum(p => p.Amount);

            // 4. Biểu đồ
            if (period == "today")
            {
                // Nhóm doanh thu theo từng giờ
                var revenueByHour = paidPayments
                    .GroupBy(p => p.PaidAt.Hour)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.Amount)
                    );

                for (int hour = 0; hour < 24; hour++)
                {
                    model.ChartLabels.Add($"{hour:00}:00");

                    model.ChartValues.Add(
                        revenueByHour.TryGetValue(hour, out var value)
                            ? value
                            : 0
                    );
                }
            }
            else
            {
                // Nhóm doanh thu theo ngày
                var revenueByDate = paidPayments
                    .GroupBy(p => p.PaidAt.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.Amount)
                    );

                for (DateTime day = fromDate;
                     day < toDate;
                     day = day.AddDays(1))
                {
                    model.ChartLabels.Add(
                        day.ToString("dd/MM")
                    );

                    model.ChartValues.Add(
                        revenueByDate.TryGetValue(day.Date, out var value)
                            ? value
                            : 0
                    );
                }
            }

            return View(model);
        }
    }
}
