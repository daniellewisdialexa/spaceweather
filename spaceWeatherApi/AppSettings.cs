using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceWeatherApi
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public IdentitySettings IdentitySettings { get; set; }
        public DataValues DataValues { get; set; }

        public AppSettings()
        {
            ConnectionStrings = new ConnectionStrings();
            IdentitySettings = new IdentitySettings();
            DataValues = new DataValues();
        }
    }

    public class ConnectionStrings
    {
        public string DONKIBaseURL { get; set; } = string.Empty;
        public string NOAABaseURl { get; set; } = string.Empty;
    }


    public class IdentitySettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public class DataValues
    {
        public int CME_ASSOCIATION_WINDOW_HOURS { get; set; }
        public Dictionary<string, string> MagneticClassDescriptions { get; set; } = [];

        public Dictionary<string, SpeedRange> ExpectedSpeedRanges { get; set; } = [];
    }

    public class SpeedRange
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }
}
