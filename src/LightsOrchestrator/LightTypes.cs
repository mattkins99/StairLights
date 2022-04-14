namespace LightsOrchestrator
{
    public interface ILightTypes
    {
        string Controller { get; }

        string Entity { get; }
    }

    public class Stair : ILightTypes
    {
        public string Controller { get; } = "/api/stairs";

        public string Entity { get; } = "Stair";
    }

    public class Mushroom : ILightTypes
    {
        public string Controller { get; } = "/api/mushrooms";

        public string Entity { get; } = "mush";
    }
}