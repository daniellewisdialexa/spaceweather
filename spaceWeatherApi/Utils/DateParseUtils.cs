using System.Globalization;

namespace SpaceWeatherApi.Utils
{
    public class DateParseUtils
    {

        /// <summary>
        /// Parsing the start and end date strings into DateTime objects.   
        /// startDate param accepts strings: "today", "yr{number}", "yyyy-MM-dd"
        /// When using the yr shorthand the response will take a bit of time due to the amount of data
        /// </summary>
        /// <param name="startDate"></param> 
        /// <param name="endDate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public (DateTime parsedStartDate, DateTime parsedEndDate) ParseDateTime(string? startDate, string? endDate)
        {
            if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
            {
                return GetDefaultDateRange();
            }

            if (startDate != null && startDate.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                return GetTodayDateRange();
            }

            if (startDate != null && startDate.StartsWith("yr", StringComparison.OrdinalIgnoreCase))
            {
                return ParseYearShorthand(startDate);
            }

            // Simple check for yyyy-MM-dd format
            if (startDate != null && startDate.Length != 10)
            {
                throw new ArgumentException("Invalid start date format. Expected format: yyyy-MM-dd", nameof(startDate));
            }

            if (endDate != null && endDate.Length != 10)
            {
                throw new ArgumentException("Invalid end date format. Expected format: yyyy-MM-dd", nameof(endDate));
            }

            return ParseExactDates(startDate, endDate);
        }

        /// <summary>
        /// Provides a default date range of 30 days.
        /// </summary>
        /// <returns>Returns parsed start and end dates</returns>

        private static (DateTime parsedStartDate, DateTime parsedEndDate) GetDefaultDateRange()
        {
            DateTime parsedStartDate = DateTime.UtcNow.AddDays(-30);
            DateTime parsedEndDate = DateTime.UtcNow;
            return (parsedStartDate, parsedEndDate);
        }

        /// <summary>
        /// Get the date range for today.
        /// </summary>
        /// <returns>Returns parsed start and end DateTime</returns>
        private static (DateTime parsedStartDate, DateTime parsedEndDate) GetTodayDateRange()
        {
            DateTime parsedStartDate = DateTime.UtcNow;
            DateTime parsedEndDate = DateTime.UtcNow;
            return (parsedStartDate, parsedEndDate);
        }

        ///<summary>
        /// Parses the year shorthand string into a start and end date.
        /// </summary>
        /// <param name="startDate"></param>
        /// <returns>Returns parsed start and end DateTimes</returns>
        /// <exception cref="ArgumentException"></exception>
        private static (DateTime parsedStartDate, DateTime parsedEndDate) ParseYearShorthand(string startDate)
        {
            int years = int.Parse(startDate[2..]);
            DateTime parsedStartDate = DateTime.UtcNow.AddYears(-years);
            DateTime parsedEndDate = DateTime.UtcNow;
            return (parsedStartDate, parsedEndDate);
        }  
        
        /// <summary>
        /// Parses the exact date strings into DateTim4e objects.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>Parsed start and end DateTimes</returns>
        /// <exception cref="ArgumentException"></exception>
        private static (DateTime parsedStartDate, DateTime parsedEndDate) ParseExactDates(string? startDate, string? endDate)
        {
            DateTime parsedStartDate = DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sDate)
                ? sDate
                : throw new ArgumentException("Invalid start date format", nameof(startDate));

            DateTime parsedEndDate = string.IsNullOrEmpty(endDate)
                ? DateTime.UtcNow
                : DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime eDate)
                    ? eDate
                    : throw new ArgumentException("Invalid end date format", nameof(endDate));

            return (parsedStartDate, parsedEndDate);
        }

    }
}
