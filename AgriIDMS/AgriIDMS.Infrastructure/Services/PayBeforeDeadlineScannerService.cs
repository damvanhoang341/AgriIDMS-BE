using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriIDMS.Infrastructure.Services
{
    /// <summary>
    /// Đơn online PayBefore: quá hạn ExpiredAt allocation (24h sau sale confirm) mà chưa Paid → thông báo sale một lần.
    /// </summary>
    public class PayBeforeDeadlineScannerService : BackgroundService
    {
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(5);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PayBeforeDeadlineScannerService> _logger;

        public PayBeforeDeadlineScannerService(
            IServiceScopeFactory scopeFactory,
            ILogger<PayBeforeDeadlineScannerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "PayBeforeDeadlineScannerService started. Interval={IntervalMinutes} minutes",
                ScanInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanAndNotifyAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Pay-before deadline scan failed");
                }

                await Task.Delay(ScanInterval, stoppingToken);
            }
        }

        private async Task ScanAndNotifyAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;

            var overdueOrders = await db.Orders
                .Where(o =>
                    o.Source == OrderSource.Online
                    && o.Status == OrderStatus.Confirmed
                    && o.PaymentTiming == PaymentTiming.PayBefore
                    && o.PaymentDeadlineNotifiedAt == null
                    && !o.Payments.Any(p => p.PaymentStatus == PaymentStatus.Paid)
                    && o.Allocations.Any(a =>
                        a.Status == AllocationStatus.Reserved
                        && a.ExpiredAt.HasValue
                        && a.ExpiredAt.Value <= now))
                .ToListAsync(cancellationToken);

            if (overdueOrders.Count == 0)
                return;

            _logger.LogInformation("Found {Count} online PayBefore orders past payment reservation deadline", overdueOrders.Count);

            foreach (var order in overdueOrders)
            {
                try
                {
                    await notificationService.NotifyOnlineOrderPayBeforeDeadlineOverdueAsync(order.Id);
                    order.PaymentDeadlineNotifiedAt = now;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed payment-deadline notification for order {OrderId}", order.Id);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
