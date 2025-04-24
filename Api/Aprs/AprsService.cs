using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace Aprs;

public class AprsService : IAsyncDisposable
{
    public IObservable<string> Stream { get; }
    private AprsConfig _aprsConfig;
    private TcpClient _client;
    private IDisposable? _keepAliveSubscription;
    private ILogger<AprsService> _logger;

    public AprsService(AprsConfig aprsConfig, ILogger<AprsService> logger)
    {
        _aprsConfig = aprsConfig ?? throw new ArgumentNullException(nameof(aprsConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Stream = CreateStream();
    }

    private IObservable<string> CreateStream()
    {
        ValidateConfig();

        var latitude = _aprsConfig.FilterPositionLatitude.ToString(CultureInfo.InvariantCulture);
        var longitude = _aprsConfig.FilterPositionLongitude.ToString(CultureInfo.InvariantCulture);
        var radius = _aprsConfig.FilterRadius;
        var loginText = $"user {_aprsConfig.AprsUser} pass {_aprsConfig.AprsPassword} vers ogn_gateway 1.1 filter r/{latitude}/{longitude}/{radius}";

        return Observable.Create<string>(async (observer) =>
        {
            while (true)
            {
                try
                {
                    _client?.Dispose();
                    _client = new TcpClient();
                    await _client.ConnectAsync(_aprsConfig.AprsHost, _aprsConfig.AprsPort);
                    using var streamReader = new StreamReader(_client.GetStream());
                    using var streamWriter = new StreamWriter(_client.GetStream()) { AutoFlush = true };

                    await streamWriter.WriteLineAsync(loginText);
                    StartKeepAlive(streamWriter);
                    _logger.LogInformation($"Connected to APRS server.");

                    while (_client.Connected)
                    {
                        string? line = null;
                        try
                        {
                            line = await streamReader.ReadLineAsync();
                            if (line == null)
                            {
                                observer.OnCompleted();
                                break;
                            }

                            if (line.StartsWith("#") || line.Contains("TCPIP*")) continue;
                            observer.OnNext(line);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error occured during APRS stream: {ex.Message} -> the line when the error occured was \"{line}\"");
                            observer.OnError(ex);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occured during initial connection to APRS server: {ex.Message}");
                    observer.OnError(ex);
                }
                _logger.LogInformation($"Connection lost. Trying to reconnect to APRS server...");
                _client.Close();
            }
        })
        .Publish()
        .RefCount();
    }

    private void StartKeepAlive(StreamWriter streamWriter)
    {
        _keepAliveSubscription = Observable.Interval(TimeSpan.FromMinutes(10)).Subscribe(async _ =>
        {
            try
            {
                _logger.LogInformation($"Sending keep alive");
                await streamWriter.WriteLineAsync("# keep alive");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Keep-alive failed: {ex.Message}");
                _keepAliveSubscription?.Dispose();
                Reconnect();
            }
        });
    }

    private void ValidateConfig()
    {
        if (string.IsNullOrWhiteSpace(_aprsConfig.AprsHost))
            throw new ArgumentException("AprsHost is null or empty!");

        if (_aprsConfig.AprsPort == 0)
            throw new ArgumentException("APRS port not set!");

        if (_aprsConfig.FilterPositionLatitude == 0 ||
            _aprsConfig.FilterPositionLongitude == 0 ||
            _aprsConfig.FilterRadius == 0)
        {
            throw new ArgumentException("Filters not set properly!");
        }
    }

    private void Reconnect()
    {
        try
        {
            _logger.LogInformation($"Trying to reconnect to APRS server...");
            _client.Close();
            _client.Connect(_aprsConfig.AprsHost, _aprsConfig.AprsPort);
            _logger.LogInformation($"Reconnected to APRS server.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Reconnection attempt failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _keepAliveSubscription?.Dispose();
        _client.Close();
        await Task.CompletedTask;
    }
}