using SpaceWeatherApi.DataModels;

namespace SpaceWeatherApi.Services.Interfaces
{
    public interface IFlareAnalyzerService
    {
         Task<List<InterestingEvent>> AnalyzeEventsAsync(List<FlareEvent> flareEvents, List<CMEEvent> cmeEvents);
    }
}