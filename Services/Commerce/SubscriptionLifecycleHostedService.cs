using Microsoft.Extensions.DependencyInjection;

namespace LTU_U15.Services.Commerce;

public sealed class SubscriptionLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionLifecycleHostedService> _logger;

    public SubscriptionLifecycleHostedService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionLifecycleHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var purchaseService = scope.ServiceProvider.GetRequiredService<IContentPurchaseService>();
            var result = await purchaseService.RunSubscriptionLifecycleAsync(cancellationToken);

            if (result.ProcessedMembers > 0 || result.ExpiredMarked > 0 || result.WarningEmailsSent > 0)
            {
                _logger.LogInformation(
                    "Subscription lifecycle run completed. Members: {Members}; ExpiredMarked: {ExpiredMarked}; WarningEmailsSent: {WarningEmailsSent}",
                    result.ProcessedMembers,
                    result.ExpiredMarked,
                    result.WarningEmailsSent);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription lifecycle run failed.");
        }
    }
}
