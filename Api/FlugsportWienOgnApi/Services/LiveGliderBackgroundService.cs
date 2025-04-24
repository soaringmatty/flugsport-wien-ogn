using Aprs;

namespace FlugsportWienOgnApi.Services;

public class LiveGliderBackgroundService : BackgroundService
{
    private readonly LiveGliderService _service;
    public LiveGliderBackgroundService(LiveGliderService service) => _service = service;
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        //await _service.StartTracking(cancellationToken).ConfigureAwait(false);
    }
}
