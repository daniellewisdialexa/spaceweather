
using System.Reflection;

namespace SpaceWeatherApi.Utils.Extentions
{
    public static class ListExtensions
    {
        public static int CountOf<T>(this List<T> data, string propertyName)
        {
            if (data == null || string.IsNullOrEmpty(propertyName))
                return 0;

            var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return 0;

            return data.Count(item => property.GetValue(item) != null);
        }



        /// <summary>
        /// Filter data based on the filter string array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static List<T> FilterData<T>(this List<T> data, string[] filter)
        {
            if (filter == null || filter.Length == 0) return data;
            {
                foreach (var filterItem in filter)
                {
                    var filterParts = filterItem.Split('=');
                    if (filterParts.Length == 2)
                    {
                        var filterProperty = filterParts[0];
                        var filterValue = filterParts[1];
                        data = data
                            .Where(fe =>
                            {
                                if (fe == null) return false;

                                var propertyInfo = fe.GetType().GetProperty(filterProperty);
                                if (propertyInfo == null) return false;

                                var propertyValue = propertyInfo.GetValue(fe, null);
                                if (propertyValue == null) return false;
                                return propertyValue.ToString()?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ?? false;

                            }).ToList();
                    }
                }
            }
            return data;
        }
    }


   
}