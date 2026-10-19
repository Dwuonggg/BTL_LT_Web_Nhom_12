
using BTL_LT_Web_Nhom_12.Database;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BTL_LT_Web_Nhom_12.Services
{
    public class OrderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderBackgroundService> _logger;

        public OrderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            // Kiểm tra ngay khi chạy website,
            // sau đó kiểm tra mỗi 30 giây.
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(30));

            try
            {
                do
                {
                    try
                    {
                        await ProcessOrdersAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Lỗi xử lý đơn hàng tự động");
                    }
                }
                while (await timer.WaitForNextTickAsync(
                    stoppingToken));
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Website đang tắt bình thường.
            }
        }

        private async Task ProcessOrdersAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<WebDbContext>();

            // OrderDate đang lưu theo giờ địa phương.
            DateTime paymentDeadline =
                DateTime.Now.AddMinutes(-10);

            var expiredOrderIds = await db.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Status == "Pending" &&
                    o.OrderDate <= paymentDeadline &&
                    o.Payment != null &&
                    o.Payment.PaymentStatus == "Pending")
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);

            foreach (int orderId in expiredOrderIds)
            {
                // Hủy Order và Payment trong cùng transaction.
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                int paymentUpdated = await db.Payments
                    .Where(p =>
                        p.OrderId == orderId &&
                        p.PaymentStatus == "Pending" &&
                        p.Order.Status == "Pending" &&
                        p.Order.OrderDate <= paymentDeadline)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            p => p.PaymentStatus,
                            "Cancelled"),
                        cancellationToken);

                if (paymentUpdated != 1)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);
                    continue;
                }

                int orderUpdated = await db.Orders
                    .Where(o =>
                        o.OrderId == orderId &&
                        o.Status == "Pending" &&
                        o.OrderDate <= paymentDeadline &&
                        o.Payment != null &&
                        o.Payment.PaymentStatus == "Cancelled")
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            o => o.Status,
                            "Cancelled"),
                        cancellationToken);

                if (orderUpdated != 1)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);
                    continue;
                }

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Đã hủy đơn #{OrderId} do quá hạn thanh toán",
                    orderId);
            }

            DateTime deliveryDeadline =
                DateTime.UtcNow.AddHours(-24);

            int completed = await db.Orders
                .Where(o =>
                    o.Status == "Delivering" &&
                    o.DeliveryStartedAt.HasValue &&
                    o.DeliveryStartedAt.Value <= deliveryDeadline &&
                    o.Payment != null &&
                    o.Payment.PaymentStatus == "Paid")
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        o => o.Status,
                        "Delivered"),
                    cancellationToken);

            if (completed > 0)
            {
                _logger.LogInformation(
                    "Đã tự hoàn thành {Count} đơn hàng",
                    completed);
            }
        }
    }
}
