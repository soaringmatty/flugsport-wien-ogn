using Aprs.Models;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;

namespace Aprs;

public class LiveGliderService
{
    private AprsService _aprsService;
    private StreamConverter _streamConverter;
    private readonly ILogger<LiveGliderService> _logger;

    public event Action<FlightData>? OnDataReceived;

    public LiveGliderService(double filterPositionLatitude, double filterPositionLongitude, int filterRadius, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LiveGliderService>();
        var streamConverterLogger = loggerFactory.CreateLogger<StreamConverter>();
        _streamConverter = new StreamConverter(streamConverterLogger);

        var config = new AprsConfig
        {
            FilterPositionLatitude = filterPositionLatitude,
            FilterPositionLongitude = filterPositionLongitude,
            FilterRadius = filterRadius
        };
        this._aprsService = new AprsService(config, loggerFactory.CreateLogger<AprsService>());
    }

    public async Task StartTracking()
    {
        this._aprsService.Stream
            .Subscribe(line =>
            {
                var result = _streamConverter.ConvertData(line);
                if (result != null)
                {
                    OnDataReceived?.Invoke(result);
                }
            });
        await _aprsService.Stream;
    }
}
