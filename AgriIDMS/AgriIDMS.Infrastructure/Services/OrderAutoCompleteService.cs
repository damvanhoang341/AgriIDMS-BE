using AgriIDMS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriIDMS.Infrastructure.Services
{
    /// <summary>
    /// Periodically closes delivered orders after after-sales grace period.
    /// </summary>
    public class OrderAutoCompleteService : BackgroundService
    {
        private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderAutoCompleteService> _logger;

        public OrderAutoCompleteService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderAutoCompleteService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "OrderAutoCompleteService started. Interval={IntervalMinutes} minutes",
                ScanInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                    var completedCount = await orderService.AutoCompleteOrdersAsync();
                    if (completedCount > 0)
                        _logger.LogInformation("Auto-completed {Count} delivered orders", completedCount);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Order auto-complete job failed");
                }

                await Task.Delay(ScanInterval, stoppingToken);
            }
        }
    }
}
