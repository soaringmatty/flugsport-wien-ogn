using Aprs;
using FlugsportWienOgn.Database;
using FlugsportWienOgnApi;
using FlugsportWienOgnApi.Hubs;
using FlugsportWienOgnApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

// Add options
builder.Services.AddOptions<OgnConfig>().Bind(builder.Configuration.GetSection("OgnConfig"));
builder.Services.AddOptions<AprsConfig>().Bind(builder.Configuration.GetSection("AprsConfig"));

// Register application services
builder.Services.AddSingleton<AircraftProvider>();
builder.Services.AddSingleton<KnownAircraftService>();
builder.Services.AddSingleton<FlightService>();
builder.Services.AddSingleton<LiveTrackingService>();
builder.Services.AddSingleton(serviceProvider =>
{
    var aprsConfig = serviceProvider.GetRequiredService<IOptions<AprsConfig>>();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    return new LiveGliderService(aprsConfig.Value, loggerFactory);
});

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
builder.Services.AddHostedService<LiveTrackingBackgroundService>(); // BackgroundService that subscribes to APRS Server to receive live position updates
builder.Services.AddHostedService<DailyCleanupService>();

var app = builder.Build();

// Ensure DB existence
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    dbContext.InitializeDatabase();
}

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
