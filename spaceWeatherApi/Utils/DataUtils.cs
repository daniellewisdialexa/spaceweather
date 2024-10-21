
using SpaceWeatherApi.DataModels;
namespace SpaceWeatherApi.Utils
{

    public class DataUtils
    {
        

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
        ///  Find solar events that might have occurred nearly at the same time
        /// </summary>
        /// <param name="FLRdata"></param>
        /// <param name="CMEdata"></param>
        /// <returns> List of objects</returns>

        public List<object> FindDetailedCorralatedEvents(List<object> FLRdata, List<object> CMEdata)
        {

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

            return correlatedEvents;
        }


        public class CorrelatedEvent
        {
            public string? FlareClassType { get; set; }
            public List<float?>? CMESpeeds { get; set; }
        }

        /// <summary>
        ///  Find solar events that might have occurred nearly at the same time
        /// </summary>
        /// <param name="FLRdata"></param>
        /// <param name="CMEdata"></param>
        /// <returns>CorrelatedEvent list</returns>
        public List<CorrelatedEvent> FindCorrelatedEvents(List<object> FLRdata, List<object> CMEdata)
        {
            var flareEvents = FLRdata.Cast<FlareEvent>().ToList();
            var CMEEvents = CMEdata.Cast<CMEEvent>().ToList();

            TimeSpan timeWindow = TimeSpan.FromMinutes(30);

            var correlatedEvents = flareEvents
                .SelectMany(flare => CMEEvents
                    .Where(cme => cme.StartTime.HasValue && flare.BeginTime.HasValue &&
                                  Math.Abs((cme.StartTime.Value - flare.BeginTime.Value).TotalMinutes) <= timeWindow.TotalMinutes)
                    .Select(cme => new CorrelatedEvent
                    {
                        FlareClassType = flare.ClassType,
                        CMESpeeds = cme.CMEAnalyses.Select(analysis => analysis.Speed).ToList()
                    }))
                .ToList();

            return correlatedEvents;
        }



        /// <summary>
        /// Convert the flare class to a numeric value
        /// </summary>
        /// <param name="flareClass"></param>
        /// <returns>The converted class number</returns>
        public double ConvertFlareClassToNumeric(string flareClass)
        {
            if (string.IsNullOrEmpty(flareClass))
                return 0;

            flareClass = flareClass.Trim().ToUpper();

            char classLetter = flareClass[0];
            double baseValue;

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



        /// <summary>
        /// Generate scatter plot of solar flare intensity vs CME speed
        /// </summary>
        /// <param name="flareIntensities"></param>
        /// <param name="cmeSpeeds"></param>
        /// <returns>Return html string that contains scatter plot image</returns>
        public string GenerateHTMLScatterPlot(double[] flareIntensities, double[] cmeSpeeds)
        {

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

            return html;
        }

  
    }


}
