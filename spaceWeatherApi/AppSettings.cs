using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spaceweather
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public IdentitySettings IdentitySettings { get; set; }
   

        public AppSettings()
        {
            ConnectionStrings = new ConnectionStrings();
            IdentitySettings = new IdentitySettings();
            
        }
    }

    public class ConnectionStrings
    {
        public string DONKIBaseURL { get; set; } = string.Empty;
    }

    public class IdentitySettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
