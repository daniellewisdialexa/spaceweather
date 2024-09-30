using System.Globalization;
using Newtonsoft.Json;
using spaceWeatherApi.DataModels;

namespace spaceWeatherApi
{
    public class DataHandler
    {
        private readonly IHost _host;

        public DataHandler(IHost host)
        {
            _host = host;
        }

        public async Task FetchDataAsync(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run <endpoint> [startDate] [endDate] [classType]");
                return;
            }

            string endpoint = args[0];
            DateTime? startDate = args.Length > 1 && DateTime.TryParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedStartDate) ? parsedStartDate : null;
            DateTime? endDate = args.Length > 2 && DateTime.TryParseExact(args[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate) ? parsedEndDate : null;
            string? classType = args.Length > 3 && !args[3].StartsWith("--") ? args[3] : null;

            var nasaApiClient = _host.Services.GetRequiredService<NasaApiClient>();

            if (endpoint == "FLR")
            {
                var flareEvents = await nasaApiClient.GetDataAsync<FlareEvent>(endpoint, startDate, endDate);

                if (!string.IsNullOrEmpty(classType))
                {
                    DateTime oneYearAgo = DateTime.UtcNow.AddYears(-1);

                    // Filter the results based on the search text and date
                    flareEvents = flareEvents
                        .Where(fe => fe.ClassType.Contains(classType, StringComparison.OrdinalIgnoreCase) && fe.BeginTime >= oneYearAgo)
                        .ToList();
                }

                // Output the JSON data to the console
                if (flareEvents != null)
                {
                    var json = JsonConvert.SerializeObject(flareEvents, Formatting.Indented);
                    Console.WriteLine(json);
                }
            }
            else if (endpoint == "CME")
            {
                var cmeEvents = await nasaApiClient.GetDataAsync<CMEEvent>(endpoint, startDate, endDate);

                // Output the JSON data to the console
                if (cmeEvents != null)
                {
                    var json = JsonConvert.SerializeObject(cmeEvents, Formatting.Indented);
                    Console.WriteLine(json);
                }
            }
            else
            {
                Console.WriteLine("Unknown endpoint.");
            }
        }
    }
}