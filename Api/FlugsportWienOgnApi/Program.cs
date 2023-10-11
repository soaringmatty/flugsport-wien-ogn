using FlugsportWienOgnApi.Hubs;
using FlugsportWienOgnApi.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddSingleton<FlightService>(); 
builder.Services.AddSingleton<LoxnFlightbookService>();

builder.Services.AddCors();

var app = builder.Build();

var loxnFlightbookService = app.Services.GetRequiredService<LoxnFlightbookService>();

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
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapHub<NotificationReaderHub>("/hubs/notificationReader");
//});
app.Run();

//await loxnFlightbookService.StartTracking();
