using FlugsportWienOgn.Database;
using Microsoft.EntityFrameworkCore;
using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Services;

public class FlightbookService
{
    private readonly ILogger<FlightbookService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly KnownAircraftService _knownAircraftService;

    public FlightbookService(IServiceProvider serviceProvider, ILogger<FlightbookService> logger, KnownAircraftService knownAircraftService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _knownAircraftService = knownAircraftService;
    }

    public async Task<IEnumerable<DepartureListItem>> GetFlightbookByAirfieldIcao(string icao, bool? knownGlidersOnly)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var today = DateTime.UtcNow.Date;
        var flightbookEntries = await dbContext.FlightbookEntry
            .Include(e => e.Aircraft)
            .Where(e => e.AirfieldIcao.ToUpper() == icao.ToUpper()
                && (
                    (e.TakeOffTimestamp != null && e.TakeOffTimestamp.Value.Date == today) ||
                    (e.LandingTimestamp != null && e.LandingTimestamp.Value.Date == today)
                ))
            .OrderByDescending(e => e.TakeOffTimestamp ?? e.LandingTimestamp)
            .ToListAsync();

        var filteredEntries = knownGlidersOnly == true
            ? flightbookEntries.Where(e => _knownAircraftService.AllKnownPlaneFlarmIds.Contains(e.Aircraft.FlarmId))
            : flightbookEntries;

        var departureList = filteredEntries.Select(e =>
        {
            var known = _knownAircraftService.AllKnownPlanes.FirstOrDefault(p => p.FlarmId == e.Aircraft.FlarmId);
            return new DepartureListItem
            {
                FlarmId = e.Aircraft.FlarmId,
                Registration = e.Aircraft.Registration,
                RegistrationShort = e.Aircraft.CallSign,
                Model = e.Aircraft.Model,
                DepartureTimestamp = e.TakeOffTimestamp,
                LandingTimestamp = e.LandingTimestamp,
                LaunchType = (LaunchType)e.LaunchType,
                LaunchHeight = e.LaunchHeight
            };
        });
        return departureList;
    }
}