using Aprs;

namespace FlugsportWienOgnApi.Services;

public class AircraftProviderInitializer : IHostedService
{
    private readonly AircraftProvider _provider;
    public AircraftProviderInitializer(AircraftProvider provider) => _provider = provider;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _provider.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
