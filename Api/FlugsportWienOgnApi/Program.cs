using Arps;
using FlugsportWienOgn.Database;
using FlugsportWienOgnApi.Hubs;
using FlugsportWienOgnApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddSingleton<AircraftProvider>();
builder.Services.AddSingleton<FlightService>();
builder.Services.AddSingleton(serviceProvider =>
{
    double AUSTRIA_LATITUDE = 47.748870;
    double AUSTRIA_LONGITUDE = 13.323171;
    int AUSTRIA_RADIUS = 300; // in km
    return new LiveGliderService(AUSTRIA_LATITUDE, AUSTRIA_LONGITUDE, AUSTRIA_RADIUS);
});
//builder.Services.AddSingleton<LoxnFlightbookService>();
builder.Services.AddSingleton<LiveTrackingService>();


builder.Services.AddDbContext<FlightDbContext>(options =>
    options.UseInMemoryDatabase("FlightDatabase"));
builder.Services.AddCors();

var app = builder.Build();

// Subscribe to APRS Server to receive live position updates
var liveGliderService = app.Services.GetRequiredService<LiveGliderService>();
Task.Factory.StartNew(async () =>
{
    var aircraftProvider = app.Services.GetRequiredService<AircraftProvider>();
    var liveTrackingService = app.Services.GetRequiredService<LiveTrackingService>();
    await aircraftProvider.InitializeAsync();
    await liveGliderService.StartTracking();
});


// Configure the HTTP request pipeline.
app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization(); 
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // allow any origin
    .AllowCredentials()); // allow credentials
app.MapControllers();
app.MapHub<LiveGliderHub>("hubs/liveglider");

app.Run();
