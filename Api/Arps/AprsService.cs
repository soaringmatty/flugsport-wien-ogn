using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Arps;

internal class AprsService
{
    public IObservable<string> Stream { get; }
    private AprsConfig _aprsConfig;

    internal AprsService(AprsConfig aprsConfig)
    {
        this._aprsConfig = aprsConfig;
        Stream = CreateStream();
    }

    private IObservable<string> CreateStream()
    {
        if (string.IsNullOrWhiteSpace(_aprsConfig.AprsHost))
        {
            throw new Exception("AprsHost is null or empty!");
        }
        if (_aprsConfig.AprsPort == 0)
        {
            throw new Exception("APRS port not set!");
        }

        if (
            _aprsConfig.FilterPositionLatitude == 0 ||
            _aprsConfig.FilterPositionLongitude == 0 ||
            _aprsConfig.FilterRadius == 0
        )
        {
            throw new Exception("Filters not set properly!");
        }

        var latitude = _aprsConfig.FilterPositionLatitude.ToString(CultureInfo.InvariantCulture);
        var longitude = _aprsConfig.FilterPositionLongitude.ToString(CultureInfo.InvariantCulture);
        var radius = _aprsConfig.FilterRadius;
        var loginText = $"user {_aprsConfig.AprsUser} pass {_aprsConfig.AprsPassword} vers ogn_gateway 1.1 filter r/{latitude}/{longitude}/{radius}";

        return Observable.Create(async (IObserver<string> observer) =>
        {
            var client = new TcpClient();
            await client.ConnectAsync(_aprsConfig.AprsHost, _aprsConfig.AprsPort);

            var streamReader = new StreamReader(client.GetStream());
            var streamWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };

            // Login on the APRS server
            await streamWriter.WriteLineAsync(loginText);

            // Make sure to send regular messages to keep the connection alive
            async void SendKeepAlive(long _) {
                //Console.WriteLine($"Sending: # keep alive");
                await streamWriter.WriteLineAsync("# keep alive");
            }

            Observable.Interval(TimeSpan.FromMinutes(10))
                .Subscribe(SendKeepAlive);

            //Console.WriteLine($"Start reading stream");
            while (true)
            {
                var line = await streamReader.ReadLineAsync();
                //Console.WriteLine($"Read line: ${line}");
                if (line == null)
                {
                    // Stream ended. Close the loop.
                    observer.OnCompleted();
                    break;
                }

                if (line.StartsWith("#") || line.Contains("TCPIP*"))
                {
                    // Ignore server messages
                    continue;
                }

                observer.OnNext(line);
            }
        })
            .Publish() // Every subscriber should subscribe to the _same_ Observable (see "hot Observable")
            .AutoConnect(); // Hot Observables must be connected. We're doing that on the first subscription.
    }
}
