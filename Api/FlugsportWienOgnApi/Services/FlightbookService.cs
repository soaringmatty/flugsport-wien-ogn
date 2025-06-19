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

    public async Task<IEnumerable<DepartureListItem>> GetFlightbookByAirfieldIcao(string icao, GliderListFilter filter)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var today = DateTime.UtcNow.Date;
        var query = dbContext.FlightbookEntry
            .Include(e => e.Aircraft)
            .Include(e => e.TowFlightEntry)
                .ThenInclude(t => t.Aircraft)
            .Where(e => e.AirfieldIcao.ToUpper() == icao.ToUpper()
                && (
                    (e.TakeOffTimestamp != null && e.TakeOffTimestamp.Value.Date == today) ||
                    (e.LandingTimestamp != null && e.LandingTimestamp.Value.Date == today)
                ));

        if (!filter.IncludeLaunchTypeWinch)
            query = query.Where(e => e.LaunchType != (int)LaunchType.Winch);

        if (!filter.IncludeLaunchTypeAerotow)
            query = query.Where(e => e.LaunchType != (int)LaunchType.Aerotow);

        if (!filter.IncludeLaunchTypeMotorized)
            query = query.Where(e => e.LaunchType != (int)LaunchType.Motorized);

        if (!filter.IncludeLaunchTypeUnknown)
            query = query.Where(e => e.LaunchType != (int)LaunchType.Unknown);

        if (!string.IsNullOrWhiteSpace(filter.FlarmId))
            query = query.Where(e => e.Aircraft.FlarmId == filter.FlarmId);

        if (!string.IsNullOrWhiteSpace(filter.TowPlaneFlarmId))
            query = query.Where(e => e.TowFlightEntry != null &&
                                     e.TowFlightEntry.Aircraft.FlarmId == filter.TowPlaneFlarmId);

        var resultList = await query
            .OrderByDescending(e => e.TakeOffTimestamp ?? e.LandingTimestamp)
            .ToListAsync();

        if (filter.KnownGlidersOnly)
        {
            resultList = resultList
                .Where(e => _knownAircraftService.AllKnownPlaneFlarmIds.Contains(e.Aircraft.FlarmId))
                .ToList();
        }

        var departureList = resultList.Select(e =>
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