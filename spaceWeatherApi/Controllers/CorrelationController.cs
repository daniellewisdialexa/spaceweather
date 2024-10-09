using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi.DataModels;
using System.Text;
using SpaceWeatherApi.Utils;
namespace SpaceWeatherApi.Controllers
{

    [ApiController]
    [Route("api/report")]
    public class CorrelationController(NasaApiClient nasaApiClient, FlareAnalyzer flareAnalyzer) : BaseController(nasaApiClient)
    {
        private readonly FlareAnalyzer _flareAnalyzer = flareAnalyzer;
        /// <summary>
        /// Get solar events that might have occurred nearly at the same time
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("sametime")]
        public async Task<IActionResult> GetCorrelationSameTimeEvents([FromQuery] string? startDate = null, string? endDate = null)
        {
            var FLRdata = await _nasaApiClient.GetDataAsync("FLR", startDate, endDate) ?? [];
            var CMEdata = await _nasaApiClient.GetDataAsync("CME", startDate, endDate) ?? [];

            var flareEvents = FLRdata.Cast<FlareEvent>().Where(f => f.BeginTime.HasValue && f.PeakTime.HasValue).ToList();
            var CMEEvents = CMEdata.Cast<CMEEvent>().Where(c => c.StartTime.HasValue).ToList();
            
            TimeSpan timeVariation = TimeSpan.FromMinutes(5); // 5 minutes for a slightly wider window
            TimeSpan maxFlareDuration = TimeSpan.FromHours(2); // Maximum flare duration

            var correlatedEvents = flareEvents
                .SelectMany(flare => CMEEvents
                    .Where(cme => flare.BeginTime.HasValue && flare.PeakTime.HasValue && cme.StartTime.HasValue &&
                                  (cme.StartTime.Value >= flare.BeginTime.Value - timeVariation &&
                                   cme.StartTime.Value <= flare.PeakTime.Value + timeVariation) &&
                                  (flare.PeakTime.Value - flare.BeginTime.Value) <= maxFlareDuration)
                    .Select(cme => CreateDetailedCorrelatedEventReport(flare, cme)))
                .ToList();

            return Ok(correlatedEvents);
        }

        /// <summary>
        /// Create a detailed report of correlated solar events
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cme"></param>
        /// <returns></returns>
        private static object CreateDetailedCorrelatedEventReport(FlareEvent flare, CMEEvent cme)
        {
            var timeDifference = (cme.StartTime?.Subtract(flare.BeginTime ?? DateTime.MinValue).TotalMinutes) ?? 0;
            var flareRiseDuration = (flare.PeakTime?.Subtract(flare.BeginTime ?? DateTime.MinValue).TotalMinutes) ?? 0;

            return new
            {
                FlareID = flare.FlrID,
                FlareClassType = flare.ClassType,
                FlareBeginTime = flare.BeginTime,
                FlarePeakTime = flare.PeakTime,
                FlareRiseDuration = flareRiseDuration,
                FlareLink = flare.Link,
                CMEID = cme.ActivityID,
                CMEStartTime = cme.StartTime,
                TimeDifferenceMins = timeDifference,
                CMEAnalyses = cme.CMEAnalyses.Select(analysis => new
                {
                    analysis.IsMostAccurate,
                    analysis.Type,
                    analysis.Speed,
                    analysis.Latitude,
                    analysis.Longitude,
                    analysis.HalfAngle,
                    analysis.Link
                })
            };
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
            var FLRdata = await _nasaApiClient.GetDataAsync("FLR", startDate, endDate) ?? [];
            var CMEdata = await _nasaApiClient.GetDataAsync("CME", startDate, endDate) ?? [];

            var flareEvents = FLRdata.Cast<FlareEvent>().ToList();
            var CMEEvents = CMEdata.Cast<CMEEvent>().ToList();

            TimeSpan timeWindow = TimeSpan.FromMinutes(30);

            var correlatedEvents = flareEvents
            .SelectMany(flare => CMEEvents
                .Where(cme => cme.StartTime.HasValue && flare.BeginTime.HasValue &&
                              Math.Abs((cme.StartTime.Value - flare.BeginTime.Value).TotalMinutes) <= timeWindow.TotalMinutes)
                .Select(cme => new
                {
                    FlareClassType = flare.ClassType,
                    CMESpeeds = cme.CMEAnalyses.Select(analysis => analysis.Speed).ToList()
                }))
                     .ToList();

            //Data for plot
            double[] flareIntensities = correlatedEvents.Select(e => ConvertFlareClassToNumeric(e.FlareClassType)).ToArray();
            double[] cmeSpeeds = correlatedEvents.Select(e => e.CMESpeeds.FirstOrDefault() ?? 0.0).ToArray();

            //Making the plot
            var plt = new ScottPlot.Plot();
            plt.Add.Scatter(flareIntensities, cmeSpeeds);

            // Transform flare intensities to logarithmic scale
            double[] logFlareIntensities = flareIntensities.Select(x => Math.Log10(x)).ToArray();
            var scatter = plt.Add.Scatter(logFlareIntensities, cmeSpeeds);
            scatter.Color = ScottPlot.Color.FromHex("#FF0000");
            scatter.LineWidth = 0; 
            scatter.MarkerSize = 10; 
            scatter.MarkerShape = ScottPlot.MarkerShape.FilledCircle;

            //Lables
            plt.Title("Solar Flare Intensity vs CME Speed");
            plt.XLabel("Flare Intensity (Log Scale)");
            plt.YLabel("CME Speed (km/s)");

            // Set labels for the X-axis, flare classes
            double[] tickPositions = { -8, -7, -6, -5, -4 };
            string[] tickLabels = { "A", "B", "C", "M", "X" };

            // Set axis limits to ensure all data is visible
            plt.Axes.SetLimits(
            left: -9,
            right: -3,
            bottom: cmeSpeeds.Min(),
            top: cmeSpeeds.Max() * 1.1
            );

            // Create a custom tick generator with labels
            var customTickGenerator = new ScottPlot.TickGenerators.NumericManual();
            for (int i = 0; i < tickPositions.Length; i++)
            {
                customTickGenerator.AddMajor(tickPositions[i], tickLabels[i]);
            }

            // Set the custom tick generator
            plt.Axes.Bottom.TickGenerator = customTickGenerator;

            // Adjust X-axis appearance
            plt.Axes.Bottom.MajorTickStyle.Color = ScottPlot.Color.FromHex("#0000FF");
            plt.Axes.Bottom.MinorTickStyle.Color = ScottPlot.Color.FromHex("#2ddd15");
            plt.Axes.Bottom.MajorTickStyle.Length = 10;
            plt.Axes.Bottom.MinorTickStyle.Length = 5;


            //Convert the image to a base64 string
            byte[] imageBytes = plt.GetImage(800, 600).GetImageBytes();
            string base64 = Convert.ToBase64String(imageBytes);

            // Create an HTML page with the embedded image
            string html = $@"
            <html>
            <body>
                <h1>Solar Flare Intensity vs CME Speed</h1>
                <img src='data:image/png;base64,{base64}'/>
            </body>
            </html>";

            /// <summary>
            /// Converts a flare class to a numeric value
            /// </summary>
            static double ConvertFlareClassToNumeric(string flareClass)
            {
                if (string.IsNullOrEmpty(flareClass))
                    return 0;

                flareClass = flareClass.Trim().ToUpper();

                char classLetter = flareClass[0];
                double baseValue = 0;

                switch (classLetter)
                {
                    //Uses scientific notation to convert
                    //the flare class to a numeric value
                    //e = exponent, 1e=-8 means 10^-8
                    case 'A': baseValue = 1e-8; break;
                    case 'B': baseValue = 1e-7; break;
                    case 'C': baseValue = 1e-6; break;
                    case 'M': baseValue = 1e-5; break;
                    case 'X': baseValue = 1e-4; break;
                    default: return 0;
                }

                if (flareClass.Length > 1 && double.TryParse(flareClass.AsSpan(1), out double multiplier))
                {
                    return baseValue * multiplier;
                }

                return baseValue;
            }
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
            var FLRdata = await _nasaApiClient.GetDataAsync("FLR", startDate, endDate) ?? [];
            var CMEdata = await _nasaApiClient.GetDataAsync("CME", startDate, endDate) ?? [];

            var flareEvents = FLRdata.Cast<FlareEvent>().ToList();
            var CMEEvents = CMEdata.Cast<CMEEvent>().ToList();

            var interestingEvents = await _flareAnalyzer.AnalyzeEventsAsync(flareEvents, CMEEvents);

            var report = new StringBuilder();
            report.AppendLine("# Interesting Solar Events Report");
            report.AppendLine();

            foreach (var evt in interestingEvents)
            {   
             
                report.AppendLine($"## Flare ID: {evt.Flare.FlrID}");
                report.AppendLine($"- Flare Class: {evt.Flare.ClassType}");
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

 
