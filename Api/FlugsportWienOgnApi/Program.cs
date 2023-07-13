using FlugsportWienOgnApi.Hubs;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
//services.AddSignalR().AddJsonProtocol(options =>
//{
//    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
//});
builder.Services.AddCors();

var app = builder.Build();

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
