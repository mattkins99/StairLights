namespace LightsOrchestrator
{
    public interface IConfiguration
    {
        string BaseLightGetUri { get; set; }

        string BaseLightPostUri { get; set; }

        string SunsetGetUri { get; }

        string StairsControllerPath { get; set; }

        string StairParam { get; set; }

        string EncodedCreds { get; }

        TimeSpan BeforeSunsetStart { get; }

        TimeSpan AfterSunsetEnd { get; }

        int ControlledLight { get; }
        
        List<int> ControlledLights { get; }

        TimeSpan Delay { get; }

        string ApplicationInsightsId { get; }
    }

    public class Configuration : IConfiguration
    {
        public string BaseLightGetUri { get; set; } = "http://192.168.0.160";

        public string BaseLightPostUri { get; set; } = "https://192.168.0.160";

        public string longitude { get; set; } =  Environment.GetEnvironmentVariable("MyLng"); // set this in your local environment settings.

        public string latitude { get; set; } = Environment.GetEnvironmentVariable("MyLat"); // set this in your local environment settings.

        public TimeSpan BeforeSunsetStart { get; set; } = TimeSpan.FromMinutes(-30);

        public TimeSpan AfterSunsetEnd { get; set; }  = TimeSpan.FromHours(2);

        public int ControlledLight { get; set; } = 2;

        public List<int> ControlledLights { get; set; } = new List<int> {0, 1, 2, 3, 4, 5, 6, 7 };

        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

        public string ApplicationInsightsId { get; set; } = Environment.GetEnvironmentVariable("ApplicationInsightsId"); // set this in your local environment settings.

        public string SunsetGetUri 
        {
            get
            {   
                return $"https://api.sunrise-sunset.org/json?lat={latitude}&lng={longitude}&formatted=0&date=";
            }
        }

        public string StairsControllerPath { get; set; } = "/api/stairs";

        public string StairParam { get; set; } = "Stair";

        private string userId = Environment.GetEnvironmentVariable("StairsUser"); // set this in your local environment settings.  Must match creds in Credentials.h on the LightController

        private string pwd = Environment.GetEnvironmentVariable("StairsPass"); // set this in your local environment settings.  Must match creds in Credentials.h on the LightController

        public string EncodedCreds 
        {
            get
            {
                return System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{userId}:{pwd}"));
            }
        }
    }
}