using Aprs;
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

// Register application services
builder.Services.AddSingleton<AircraftProvider>();
builder.Services.AddSingleton<FlightService>();
builder.Services.AddSingleton(serviceProvider =>
{
    double AUSTRIA_LATITUDE = 47.748870;
    double AUSTRIA_LONGITUDE = 13.323171;
    int AUSTRIA_RADIUS = 300; // in km
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    return new LiveGliderService(AUSTRIA_LATITUDE, AUSTRIA_LONGITUDE, AUSTRIA_RADIUS, loggerFactory);
});
builder.Services.AddSingleton<LiveTrackingService>();

//builder.Services.AddDbContext<FlightDbContext>(options =>
//    options.UseInMemoryDatabase("FlightDatabase"));
builder.Services.AddDbContext<FlightDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

// Cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("NoRestriction", policy =>
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true));
});

// Register hosted services
//builder.Services.AddHostedService<AircraftProviderInitializer>();      // IHostedService that calls AircraftProvider.InitializeAsync()
//builder.Services.AddHostedService<LiveGliderBackgroundService>();     // BackgroundService that runs LiveGliderService.StartTracking()

var app = builder.Build();

// Ensure DB existence
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    dbContext.InitializeDatabase();
}

// Subscribe to APRS Server to receive live position updates
var liveGliderService = app.Services.GetRequiredService<LiveGliderService>();
Task.Factory.StartNew(async () =>
{
    var aircraftProvider = app.Services.GetRequiredService<AircraftProvider>();
    var liveTrackingService = app.Services.GetRequiredService<LiveTrackingService>();
    await aircraftProvider.InitializeAsync(CancellationToken.None);
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
app.UseCors("NoRestriction");
app.MapControllers();
app.MapHub<LiveGliderHub>("hubs/liveglider");

await app.RunAsync();
