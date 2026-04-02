using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriIDMS.Infrastructure.Services
{
    /// <summary>
    /// Periodically scans near-expiry/expired lots and notifies warehouse/sales/manager.
    /// </summary>
    public class NearExpiryLotScannerService : BackgroundService
    {
        private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(6);
        private const int DefaultNearExpiryDays = 3;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NearExpiryLotScannerService> _logger;

        public NearExpiryLotScannerService(
            IServiceScopeFactory scopeFactory,
            ILogger<NearExpiryLotScannerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "NearExpiryLotScannerService started. Interval={IntervalHours} hours, DaysThreshold={Days}",
                ScanInterval.TotalHours,
                DefaultNearExpiryDays);

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
                    _logger.LogError(ex, "Near-expiry lot scan failed");
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
            var deadline = now.AddDays(DefaultNearExpiryDays);

            var lotIds = await db.Lots
                .AsNoTracking()
                .Where(l =>
                    (l.Status == LotStatus.Active || l.Status == LotStatus.Expired) &&
                    // Only notify when there is usable stock: Stored/Reserved boxes with weight > 0.
                    l.Boxes.Any(b =>
                        (b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved) &&
                        b.Weight > 0m) &&
                    l.ExpiryDate <= deadline)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            if (lotIds.Count == 0)
                return;

            _logger.LogInformation(
                "Found {Count} near-expiry/expired lots within +{Days} days threshold",
                lotIds.Count,
                DefaultNearExpiryDays);

            foreach (var lotId in lotIds)
            {
                await notificationService.NotifyNearExpiryLotAsync(lotId);
            }
        }
    }
}
