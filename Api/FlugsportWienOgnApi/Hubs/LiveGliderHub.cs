using Microsoft.AspNetCore.SignalR;

namespace FlugsportWienOgnApi.Hubs;

public class LiveGliderHub : Hub
{
    public async Task SendUpdate(string message)
        => await Clients.All.SendAsync("receiveUpdate", message);
}
