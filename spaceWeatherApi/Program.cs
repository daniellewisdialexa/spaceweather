using spaceweather;
using spaceWeatherApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddHttpClient<NasaApiClient>();
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