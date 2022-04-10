namespace LightsOrchestrator
{
    public interface ILightTypes
    {
        string Controller { get; }

        string Entity { get; }
    }

    public class Stair : ILightTypes
    {
        public string Controller { get; } = "/api/Stairs";

        public string Entity { get; } = "stair";
    }

    public class Mushroom : ILightTypes
    {
        public string Controller { get; } = "/api/Stairs";

        public string Entity { get; } = "mush";
    }
}