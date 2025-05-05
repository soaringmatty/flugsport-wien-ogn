using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Aprs;

/// <summary>
/// Low-level APRS TCP client that publishes lines via IObservable string.
/// </summary>
public class AprsService : IAsyncDisposable
{
    private TcpClient? _client;
    private Task? _processingTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Subject<string> _subject = new();
    private readonly AprsConfig _config;
    private readonly ILogger<AprsService> _logger;

    public IObservable<string> Stream => _subject.AsObservable();

    public AprsService(AprsConfig aprsConfig, ILogger<AprsService> logger)
    {
        _config = aprsConfig ?? throw new ArgumentNullException(nameof(aprsConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Kicks off the APRS TCP stream read loop in the background and returns immediately.
    /// </summary>
    public Task StartAsync(CancellationToken hostCancellationToken)
    {
        // prevent double-start
        if (_processingTask != null)
            return _processingTask;

        // link host cancellation
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken);

        // run the loop
        _processingTask = Task.Run(
            () => RunAprsStreamReaderLoopAsync(_cancellationTokenSource.Token),
            CancellationToken.None
        );

        // ensure exceptions are logged
        _processingTask.ContinueWith(task =>
        {
            _logger.LogError(task.Exception, "APRS processing crashed");
        }, TaskContinuationOptions.OnlyOnFaulted);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Contains the infinite connect/read/reconnect loop.
    /// </summary>
    private async Task RunAprsStreamReaderLoopAsync(CancellationToken cancellationToken)
    {
        ValidateConfig();

        var latitude = _config.FilterPositionLatitude.ToString(CultureInfo.InvariantCulture);
        var longitude = _config.FilterPositionLongitude.ToString(CultureInfo.InvariantCulture);
        var radius = _config.FilterRadius;
        var login = $"user {_config.AprsUser} pass {_config.AprsPassword} vers ogn_gateway 1.1 filter r/{latitude}/{longitude}/{radius}";

        int delay = 1000;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _client?.Dispose();
                _client = new TcpClient();
                await _client.ConnectAsync(_config.AprsHost!, _config.AprsPort, cancellationToken).ConfigureAwait(false);

                using var stream = _client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(login).ConfigureAwait(false);
                _logger.LogInformation("Connected to APRS server");

                // start keep-alive
                _ = StartKeepAliveLoop(writer, cancellationToken);

                // read loop
                while (!cancellationToken.IsCancellationRequested && _client.Connected)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                    if (line.StartsWith("#") || line.Contains("TCPIP*")) continue;
                    _logger.LogTrace(line);
                    _subject.OnNext(line);
                }
                _logger.LogWarning("Lost connection to APRS server");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in APRS stream. Retrying in {delay} ms");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = Math.Min(delay * 2, 60000);
            }
            finally
            {
                _client?.Close();
            }
        }
        _subject.OnCompleted();
    }

    private async Task StartKeepAliveLoop(StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteLineAsync("# keep alive").ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keep-alive failed");
        }
    }

    private void ValidateConfig()
    {
        if (string.IsNullOrWhiteSpace(_config.AprsHost))
            throw new ArgumentException("AprsHost is null or empty!");

        if (_config.AprsPort == 0)
            throw new ArgumentException("APRS port not set!");

        if (_config.FilterPositionLatitude == 0 ||
            _config.FilterPositionLongitude == 0 ||
            _config.FilterRadius == 0)
        {
            throw new ArgumentException("Filters not set properly!");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        if (_processingTask != null)
        {
            try { await _processingTask.ConfigureAwait(false); }
            catch { /* already logged */ }
        }
        //_client?.Close();
        _subject.Dispose();
    }
}