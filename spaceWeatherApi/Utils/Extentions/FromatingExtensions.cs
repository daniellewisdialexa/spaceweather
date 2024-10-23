namespace SpaceWeatherApi.Utils.Extentions
{
    public static class FromatingExtensions
    {
        /// <summary>
        /// Format the given angle as a coordinate string
        /// </summary>
        /// <param name="angle">The angle to format</param>
        /// <returns>Formatted angle coordinate</returns>
        public static string FormatAsAngle(this double angle)
        {
            angle = Math.Abs(angle); // Ensure angle is positive
            int degrees = (int)angle;
            double minutesDecimal = (angle - degrees) * 60;
            int minutes = (int)minutesDecimal;
            int seconds = (int)((minutesDecimal - minutes) * 60);

            return $"{degrees}° {minutes}' {seconds}\"";
        }


        /// <summary>
        /// Formats a coordinate (latitude or longitude) as a string with degrees, minutes, seconds, and direction.
        /// </summary>
        /// <param name="coordinate">The coordinate value to format</param>
        /// <param name="isLatitude">True if the coordinate is latitude, false if it's longitude</param>
        /// <returns>A formatted string representation of the coordinate</returns>
        public static string FormatAsLatLong(this double coordinate, bool isLatitude)
        {
            char direction = isLatitude ? (coordinate >= 0 ? 'N' : 'S') : (coordinate >= 0 ? 'E' : 'W');
            coordinate = Math.Abs(coordinate);
            int degrees = (int)coordinate;
            double minutesDecimal = (coordinate - degrees) * 60;
            int minutes = (int)minutesDecimal;
            int seconds = (int)((minutesDecimal - minutes) * 60);

            return $"{degrees}° {minutes}' {seconds}\" {direction}";
        }
    }
}