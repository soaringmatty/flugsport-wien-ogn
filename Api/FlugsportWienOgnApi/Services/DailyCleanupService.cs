using FlugsportWienOgn.Database;
using Microsoft.EntityFrameworkCore;

namespace FlugsportWienOgnApi.Services;

public sealed class DailyCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyCleanupService> _logger;

    public DailyCleanupService(IServiceProvider serviceProvider, ILogger<DailyCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await CleanupIfNeededAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1).AddMinutes(1);
            var delay = nextMidnight - now;

            _logger.LogInformation("CleanupService sleeps {Delay}. Next cleanup: {NextMidnight}", delay, nextMidnight);
            try { await Task.Delay(delay, cancellationToken); }
            catch (TaskCanceledException) { break; }

            await CleanupIfNeededAsync(cancellationToken);
        }
    }

    private async Task CleanupIfNeededAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var stamp = await db.Cleanup.SingleAsync(h => h.Id == 1, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (stamp.LastCleanup == today)
        {
            _logger.LogInformation("Database cleanup skipped since it already ran today");
            return;
        }

        _logger.LogInformation("Cleanup database...");
        var deleted = await db.FlightData.ExecuteDeleteAsync(cancellationToken);

        stamp.LastCleanup = today;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Database cleanup finished. {Rows} flightData rows deleted", deleted);
    }
}
