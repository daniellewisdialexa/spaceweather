using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi.DataModels;
using System.Text;
using SpaceWeatherApi.Utils;
using SpaceWeatherApi.Services.Interfaces;
namespace SpaceWeatherApi.Controllers
{

    [ApiController]
    [Route("api/report")]
    public class CorrelationController(IApiClient apiClient, IFlareAnalyzerService flareAnalyzerService, DataUtils dataUtils) : BaseController(apiClient)
    {

        /// <summary>
        /// Get solar events that might have occurred nearly at the same time
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>

        [HttpGet("sametime")]
        public async Task<IActionResult> GetCorrelationSameTimeEvents([FromQuery] string? startDate = null, string? endDate = null)
        {
            try
            {
                var FLRdata = await ApiClient.GetDataAsync("FLR", startDate, endDate) ?? [];
                var CMEdata = await ApiClient.GetDataAsync("CME", startDate, endDate) ?? [];
                if (dataUtils == null)
                {
                    return StatusCode(500, "DataUtils is not initialized.");
                }
                var correlatedEvents = dataUtils.FindDetailedCorralatedEvents(FLRdata, CMEdata);

                return Ok(correlatedEvents);
            }
            catch (Exception)
            {
                // Log the exception here if you have a logging mechanism
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Generate a scatter plot of solar flare intensity vs CME speed, defaults to html output
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("scottplot")]
        public async Task<IActionResult> GetScatterPlot([FromQuery] string? startDate = null, string? endDate = null)
        {
            var FLRdata = await ApiClient.GetDataAsync("FLR", startDate, endDate) ?? [];
            var CMEdata = await ApiClient.GetDataAsync("CME", startDate, endDate) ?? [];
            var correlatedEvents = dataUtils.FindCorrelatedEvents(FLRdata, CMEdata);

            //Create a scatter plot of flare intensity vs CME speed
            double[] flareIntensities = correlatedEvents
                .Where(e => !string.IsNullOrEmpty(e.FlareClassType))
                .Select(e => e.FlareClassType != null ? dataUtils.ConvertFlareClassToNumeric(e.FlareClassType) : 0.0)
                .ToArray();

            double[] cmeSpeeds = correlatedEvents
                .Where(e => e.CMESpeeds != null && e.CMESpeeds.Count != 0)
                .Select(e => e.CMESpeeds!.FirstOrDefault() ?? 0.0)
                .ToArray();

            var html = dataUtils.GenerateHTMLScatterPlot(flareIntensities, cmeSpeeds);
            return Content(html, "text/html");
        }


         /// <summary>
        /// Get interesting solar events
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("flagged")]
        public async Task<IActionResult> GetFlaggedEvents([FromQuery] string? startDate = null, string? endDate = null)
        {
            var FLRdata = await ApiClient.GetDataAsync("FLR", startDate, endDate) ?? [];
            var CMEdata = await ApiClient.GetDataAsync("CME", startDate, endDate) ?? [];

            var flareEvents = FLRdata.Cast<FlareEvent>().ToList();
            var CMEEvents = CMEdata.Cast<CMEEvent>().ToList();

            if (flareAnalyzerService == null)
            {
                return BadRequest("Flare analysis is not available");
            }
            var interestingEvents = await flareAnalyzerService.AnalyzeEventsAsync(flareEvents, CMEEvents);

            var report = new StringBuilder();
            report.AppendLine("# Interesting Solar Events Report");
            report.AppendLine();

            foreach (var evt in interestingEvents)
            {   
             
                report.AppendLine($"## Flare ID: {evt.Flare.FlrID}");
                report.AppendLine($"- Flare DataUtils: {evt.Flare.ClassType}");
                report.AppendLine($"- CME Speed: {evt.CMESpeed} km/s");
                report.AppendLine($"- Reason: {evt.Reason}");
                report.AppendLine($"- Link: {evt.Flare.Link}");
                report.AppendLine($"- Linked Events: {evt.Flare.LinkedEvents}");
                report.AppendLine();
            }

            return Content(report.ToString(), "text/markdown");
        }
    }
}

 
