using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpaceWeatherApi.Services.Interfaces;
using SpaceWeatherApi.Utils;
namespace SpaceWeatherApi.Tests;

[TestFixture]
public abstract class BaseTestFixture
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IApiClient ApiClient { get; private set; }
    protected IAppSettings AppSettings { get; private set; }
    protected DataUtils DataUtils { get; private set; }
    protected DateParseUtils DateParseUtils { get; private set; }
    protected IFlareAnalyzerService FlareAnalyzerService { get; private set; }

    [OneTimeSetUp]
    public void BaseSetUp()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration);
        services.AddSingleton<IAppSettings>(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);
        services.AddHttpClient();
        services.AddSingleton<DataUtils>();
        services.AddSingleton<DateParseUtils>();
        services.AddSingleton<IFlareAnalyzerService>();
        services.AddTransient<IApiClient, ApiClient>();
        ServiceProvider = services.BuildServiceProvider();
        AppSettings = ServiceProvider.GetRequiredService<IAppSettings>();
        ApiClient = ServiceProvider.GetRequiredService<IApiClient>();
        DataUtils = ServiceProvider.GetRequiredService<DataUtils>();
        DateParseUtils = ServiceProvider.GetRequiredService<DateParseUtils>();
        FlareAnalyzerService = ServiceProvider.GetRequiredService<IFlareAnalyzerService>();
    }

    [OneTimeTearDown]
    public void BaseTearDown()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}