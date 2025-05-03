using Aprs.Models;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;

namespace Aprs;

public class LiveGliderService : IAsyncDisposable
{
    private AprsService _aprsService;
    private StreamConverter _streamConverter;
    private IDisposable? _aprsSubscription;
    private readonly ILogger<LiveGliderService> _logger;

    public event Action<FlightData>? FlightDataReceived;
    public event Action<string>? AprsMessageReceived;

    public LiveGliderService(AprsConfig config, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LiveGliderService>();
        var streamConverterLogger = loggerFactory.CreateLogger<StreamConverter>();
        _streamConverter = new StreamConverter(streamConverterLogger);
        this._aprsService = new AprsService(config, loggerFactory.CreateLogger<AprsService>());
    }

    public void Start(CancellationToken cancellationToken)
    {
        _aprsSubscription = _aprsService.Stream
            .Subscribe(line =>
            {
                AprsMessageReceived?.Invoke(line);
                var flightData = _streamConverter.ConvertData(line);
                if (flightData != null)
                {
                    FlightDataReceived?.Invoke(flightData);
                }
            },
            ex => _logger.LogError(ex, "Error in APRS observable"),
            () => _logger.LogInformation("APRS stream completed."));

        // kick off background loop without await
        _ = _aprsService.StartAsync(cancellationToken);
    }

    public void Stop() => _aprsSubscription?.Dispose();

    public async ValueTask DisposeAsync()
    {
        Stop();
        await _aprsService.DisposeAsync().ConfigureAwait(false);
    }
}
