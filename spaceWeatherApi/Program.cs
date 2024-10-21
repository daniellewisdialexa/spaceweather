using Microsoft.Extensions.Options;
using SpaceWeatherApi;
using SpaceWeatherApi.Utils;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.Configure<AppSettings>(builder.Configuration);

// Register IAppSettings
builder.Services.AddSingleton<IAppSettings>(sp =>
    sp.GetRequiredService<IOptions<AppSettings>>().Value);

// Register IApiClient and ApiClient
builder.Services.AddHttpClient<IApiClient, ApiClient>();
builder.Services.AddScoped<FlareAnalyzer>();
builder.Services.AddScoped<DataUtils>();
builder.Services.AddScoped<DateParseUtils>();
builder.Services.AddScoped<SpaceWeatherReportingUtils>();
builder.Services.AddScoped<SolarReportingUtils>();
builder.Services.AddControllers();

var app = builder.Build();


app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("Application started. Listening on:");
    foreach (var address in app.Urls)
    {
        Console.WriteLine($"  {address}");
    }
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();         