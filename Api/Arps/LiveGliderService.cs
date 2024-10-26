using Arps.Models;
using System.Reactive.Linq;

namespace Arps;

public class LiveGliderService
{
    private AprsService _arpsService;
    private StreamConverter _streamConverter;

    public event Action<FlightData>? OnDataReceived;

    public LiveGliderService(double filterPositionLatitude, double filterPositionLongitude, int filterRadius)
    {
        _streamConverter = new StreamConverter();
        var config = new AprsConfig
        {
            FilterPositionLatitude = filterPositionLatitude,
            FilterPositionLongitude = filterPositionLongitude,
            FilterRadius = filterRadius
        };
        this._arpsService = new AprsService(config);
    }

    public async Task StartTracking()
    {
        this._arpsService.Stream
            .Subscribe(line =>
            {
                var result = _streamConverter.ConvertData(line);
                if (result != null)
                {
                    OnDataReceived?.Invoke(result);
                }
            });
        await _arpsService.Stream;
    }
}
