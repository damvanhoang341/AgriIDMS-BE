using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriIDMS.Infrastructure.Services
{
    /// <summary>
    /// Periodically scans backorder orders that passed allocation expiry
    /// and sends notifications to sales staff for customer decision.
    /// </summary>
    public class BackorderExpiryScannerService : BackgroundService
    {
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(5);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackorderExpiryScannerService> _logger;

        public BackorderExpiryScannerService(
            IServiceScopeFactory scopeFactory,
            ILogger<BackorderExpiryScannerService> logger)
        {
            //
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackorderExpiryScannerService started. Interval={IntervalMinutes} minutes", ScanInterval.TotalMinutes);

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
                    _logger.LogError(ex, "Backorder expiry scan failed");
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
                    o.Status == OrderStatus.BackorderWaiting
                    && o.BackorderExpiryNotifiedAt == null
                    && o.Details.Any(d => d.ShortageQuantity > 0)
                    && o.Allocations.Any(a =>
                        a.Status == AllocationStatus.Reserved
                        && a.ExpiredAt.HasValue
                        && a.ExpiredAt.Value <= now))
                .ToListAsync(cancellationToken);

            if (overdueOrders.Count == 0)
                return;

            _logger.LogInformation("Found {Count} overdue backorder orders", overdueOrders.Count);

            foreach (var order in overdueOrders)
            {
                try
                {
                    await notificationService.NotifyBackorderExpiredForSalesAsync(order.Id);
                    order.BackorderExpiryNotifiedAt = now;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send backorder-expired notification for order {OrderId}", order.Id);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
